using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using OnvifDeviceManager.Models;
using OnvifDeviceManager.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<OnvifDiscoveryService>();
builder.Services.AddSingleton<CredentialStore>();

var apiKey = Environment.GetEnvironmentVariable("ODM_API_KEY")
    ?? builder.Configuration["ApiKey"]
    ?? GenerateDefaultApiKey();

var allowedOrigins = Environment.GetEnvironmentVariable("ODM_ALLOWED_ORIGINS")
    ?? builder.Configuration["AllowedOrigins"]
    ?? "http://localhost:*";

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins == "*")
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        else
            policy.WithOrigins(allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries))
                  .AllowAnyMethod()
                  .AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseCors();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";

    if (path == "/" || path == "/api/status")
    {
        await next();
        return;
    }

    if (!path.StartsWith("/api/"))
    {
        await next();
        return;
    }

    var providedKey = context.Request.Headers["X-API-Key"].FirstOrDefault()
        ?? context.Request.Query["api_key"].FirstOrDefault();

    if (string.IsNullOrEmpty(providedKey) || !CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(providedKey),
            System.Text.Encoding.UTF8.GetBytes(apiKey)))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(new { error = "Unauthorized. Provide a valid API key via X-API-Key header." });
        return;
    }

    await next();
});

var connectedDevices = new ConcurrentDictionary<string, OnvifDevice>();
var lastDiscoveryTime = DateTime.MinValue;
var discoveryLock = new SemaphoreSlim(1, 1);

const int MaxDiscoveryTimeout = 30;
const int MinDiscoveryCooldown = 5;

app.MapGet("/", () =>
{
    var maskedKey = apiKey.Length > 8
        ? apiKey[..4] + new string('*', apiKey.Length - 8) + apiKey[^4..]
        : "****";

    return Results.Json(new
    {
        service = "ONVIF Device Manager Server",
        version = "1.0.0",
        status = "running",
        security = new
        {
            authentication = "API Key required (X-API-Key header)",
            apiKeyHint = maskedKey,
            binding = app.Urls.FirstOrDefault() ?? "default"
        },
        endpoints = new[]
        {
            "GET  /api/devices/discover?timeout=5",
            "POST /api/devices/add",
            "GET  /api/devices",
            "GET  /api/devices/{address}/info",
            "GET  /api/devices/{address}/capabilities",
            "GET  /api/devices/{address}/profiles",
            "GET  /api/devices/{address}/stream-uri/{profileToken}",
            "GET  /api/devices/{address}/snapshot-uri/{profileToken}",
            "GET  /api/devices/{address}/network",
            "GET  /api/devices/{address}/users",
            "POST /api/devices/{address}/ptz/move",
            "POST /api/devices/{address}/ptz/stop",
            "GET  /api/devices/{address}/ptz/status/{profileToken}",
            "GET  /api/devices/{address}/ptz/presets/{profileToken}",
            "GET  /api/status"
        }
    });
});

app.MapGet("/api/status", () => Results.Json(new
{
    status = "running",
    connectedDevices = connectedDevices.Count,
    uptime = DateTime.UtcNow.ToString("o")
}));

app.MapGet("/api/devices/discover", async (int? timeout, OnvifDiscoveryService discovery) =>
{
    var timeoutSec = Math.Clamp(timeout ?? 5, 1, MaxDiscoveryTimeout);

    var elapsed = DateTime.UtcNow - lastDiscoveryTime;
    if (elapsed.TotalSeconds < MinDiscoveryCooldown)
    {
        return Results.Json(new { error = $"Rate limited. Wait {MinDiscoveryCooldown - (int)elapsed.TotalSeconds}s before next discovery." },
            statusCode: 429);
    }

    if (!await discoveryLock.WaitAsync(0))
        return Results.Json(new { error = "Discovery already in progress." }, statusCode: 409);

    try
    {
        lastDiscoveryTime = DateTime.UtcNow;
        var devices = await discovery.DiscoverDevicesAsync(timeoutSec);

        foreach (var device in devices)
            connectedDevices.TryAdd(device.Address, device);

        return Results.Json(devices.Select(d => new
        {
            d.Address,
            d.Name,
            d.Manufacturer,
            d.Model,
            d.ServiceAddress,
            d.IsOnline,
            Status = d.Status.ToString()
        }));
    }
    finally
    {
        discoveryLock.Release();
    }
});

app.MapPost("/api/devices/add", (AddDeviceRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Address))
        return Results.BadRequest(new { error = "Address is required." });

    if (!IsValidDeviceAddress(request.Address))
        return Results.BadRequest(new { error = "Invalid device address. Must be a valid IP address or hostname." });

    var serviceAddress = request.Address.Contains("://")
        ? request.Address
        : $"http://{request.Address}/onvif/device_service";

    if (!IsValidOnvifUrl(serviceAddress))
        return Results.BadRequest(new { error = "Invalid service URL. Only http/https with standard ONVIF paths are allowed." });

    var device = new OnvifDevice
    {
        Address = request.Address,
        ServiceAddress = serviceAddress,
        Name = SanitizeString(request.Name ?? request.Address, 100),
        Username = request.Username ?? string.Empty,
        Password = request.Password ?? string.Empty,
        IsOnline = true,
        Status = DeviceStatus.Online
    };

    connectedDevices[device.Address] = device;

    return Results.Json(new
    {
        device.Address,
        device.Name,
        device.ServiceAddress,
        message = "Device added successfully"
    });
});

app.MapGet("/api/devices", () =>
{
    return Results.Json(connectedDevices.Values.Select(d => new
    {
        d.Address,
        d.Name,
        d.Manufacturer,
        d.Model,
        d.ServiceAddress,
        d.IsOnline,
        d.IsAuthenticated,
        Status = d.Status.ToString()
    }));
});

app.MapGet("/api/devices/{address}/info", async (string address) =>
{
    if (!connectedDevices.TryGetValue(address, out var device))
        return Results.NotFound(new { error = $"Device {address} not found. Add it first." });

    using var service = new OnvifDeviceService();
    try
    {
        var info = await service.GetDeviceInformationAsync(device.ServiceAddress, device.Username, device.Password);
        device.Manufacturer = info.Manufacturer;
        device.Model = info.Model;
        device.FirmwareVersion = info.FirmwareVersion;
        device.SerialNumber = info.SerialNumber;
        device.HardwareId = info.HardwareId;
        device.IsAuthenticated = true;

        return Results.Json(new
        {
            device.Address,
            device.Manufacturer,
            device.Model,
            device.FirmwareVersion,
            device.SerialNumber,
            device.HardwareId
        });
    }
    catch (SoapFaultException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/devices/{address}/capabilities", async (string address) =>
{
    if (!connectedDevices.TryGetValue(address, out var device))
        return Results.NotFound(new { error = $"Device {address} not found" });

    using var service = new OnvifDeviceService();
    try
    {
        var caps = await service.GetCapabilitiesAsync(device.ServiceAddress, device.Username, device.Password);
        device.Capabilities = caps;

        return Results.Json(new
        {
            caps.HasMedia,
            caps.HasPtz,
            caps.HasImaging,
            caps.HasEvents,
            caps.HasAnalytics,
            caps.MediaServiceAddress,
            caps.PtzServiceAddress,
            caps.EventsServiceAddress
        });
    }
    catch (SoapFaultException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/devices/{address}/profiles", async (string address) =>
{
    if (!connectedDevices.TryGetValue(address, out var device))
        return Results.NotFound(new { error = $"Device {address} not found" });

    var mediaUrl = device.Capabilities.MediaServiceAddress ?? device.ServiceAddress.Replace("/device_service", "/media_service");
    using var service = new OnvifMediaService();
    try
    {
        var profiles = await service.GetProfilesAsync(mediaUrl, device.Username, device.Password);
        return Results.Json(profiles.Select(p => new
        {
            p.Name,
            p.Token,
            p.IsPtzEnabled,
            VideoEncoder = p.VideoEncoder != null ? new { p.VideoEncoder.Encoding, p.VideoEncoder.Resolution, p.VideoEncoder.FrameRate, p.VideoEncoder.BitRate } : null,
            AudioEncoder = p.AudioEncoder != null ? new { p.AudioEncoder.Encoding, p.AudioEncoder.BitRate, p.AudioEncoder.SampleRate } : null
        }));
    }
    catch (SoapFaultException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/devices/{address}/stream-uri/{profileToken}", async (string address, string profileToken) =>
{
    if (!connectedDevices.TryGetValue(address, out var device))
        return Results.NotFound(new { error = $"Device {address} not found" });

    var mediaUrl = device.Capabilities.MediaServiceAddress ?? device.ServiceAddress.Replace("/device_service", "/media_service");
    using var service = new OnvifMediaService();
    try
    {
        var uri = await service.GetStreamUriAsync(mediaUrl, profileToken, device.Username, device.Password);
        return Results.Json(new { streamUri = uri });
    }
    catch (SoapFaultException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/devices/{address}/snapshot-uri/{profileToken}", async (string address, string profileToken) =>
{
    if (!connectedDevices.TryGetValue(address, out var device))
        return Results.NotFound(new { error = $"Device {address} not found" });

    var mediaUrl = device.Capabilities.MediaServiceAddress ?? device.ServiceAddress.Replace("/device_service", "/media_service");
    using var service = new OnvifMediaService();
    try
    {
        var uri = await service.GetSnapshotUriAsync(mediaUrl, profileToken, device.Username, device.Password);
        return Results.Json(new { snapshotUri = uri });
    }
    catch (SoapFaultException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/devices/{address}/network", async (string address) =>
{
    if (!connectedDevices.TryGetValue(address, out var device))
        return Results.NotFound(new { error = $"Device {address} not found" });

    using var service = new OnvifDeviceService();
    try
    {
        var config = await service.GetNetworkInterfacesAsync(device.ServiceAddress, device.Username, device.Password);
        return Results.Json(config);
    }
    catch (SoapFaultException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/devices/{address}/users", async (string address) =>
{
    if (!connectedDevices.TryGetValue(address, out var device))
        return Results.NotFound(new { error = $"Device {address} not found" });

    using var service = new OnvifDeviceService();
    try
    {
        var users = await service.GetUsersAsync(device.ServiceAddress, device.Username, device.Password);
        return Results.Json(users);
    }
    catch (SoapFaultException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/devices/{address}/ptz/move", async (string address, PtzMoveRequest request) =>
{
    if (!connectedDevices.TryGetValue(address, out var device))
        return Results.NotFound(new { error = $"Device {address} not found" });

    if (string.IsNullOrWhiteSpace(request.ProfileToken))
        return Results.BadRequest(new { error = "ProfileToken is required." });

    request = request with
    {
        Pan = Math.Clamp(request.Pan, -1f, 1f),
        Tilt = Math.Clamp(request.Tilt, -1f, 1f),
        Zoom = Math.Clamp(request.Zoom, -1f, 1f)
    };

    var ptzUrl = device.Capabilities.PtzServiceAddress ?? device.ServiceAddress.Replace("/device_service", "/ptz_service");
    using var service = new OnvifPtzService();
    try
    {
        await service.ContinuousMoveAsync(ptzUrl, request.ProfileToken, request.Pan, request.Tilt, request.Zoom, device.Username, device.Password);
        return Results.Json(new { message = "PTZ move command sent" });
    }
    catch (SoapFaultException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/devices/{address}/ptz/stop", async (string address, PtzStopRequest request) =>
{
    if (!connectedDevices.TryGetValue(address, out var device))
        return Results.NotFound(new { error = $"Device {address} not found" });

    if (string.IsNullOrWhiteSpace(request.ProfileToken))
        return Results.BadRequest(new { error = "ProfileToken is required." });

    var ptzUrl = device.Capabilities.PtzServiceAddress ?? device.ServiceAddress.Replace("/device_service", "/ptz_service");
    using var service = new OnvifPtzService();
    try
    {
        await service.StopAsync(ptzUrl, request.ProfileToken, true, true, device.Username, device.Password);
        return Results.Json(new { message = "PTZ stopped" });
    }
    catch (SoapFaultException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/devices/{address}/ptz/status/{profileToken}", async (string address, string profileToken) =>
{
    if (!connectedDevices.TryGetValue(address, out var device))
        return Results.NotFound(new { error = $"Device {address} not found" });

    var ptzUrl = device.Capabilities.PtzServiceAddress ?? device.ServiceAddress.Replace("/device_service", "/ptz_service");
    using var service = new OnvifPtzService();
    try
    {
        var status = await service.GetStatusAsync(ptzUrl, profileToken, device.Username, device.Password);
        return Results.Json(status);
    }
    catch (SoapFaultException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/devices/{address}/ptz/presets/{profileToken}", async (string address, string profileToken) =>
{
    if (!connectedDevices.TryGetValue(address, out var device))
        return Results.NotFound(new { error = $"Device {address} not found" });

    var ptzUrl = device.Capabilities.PtzServiceAddress ?? device.ServiceAddress.Replace("/device_service", "/ptz_service");
    using var service = new OnvifPtzService();
    try
    {
        var presets = await service.GetPresetsAsync(ptzUrl, profileToken, device.Username, device.Password);
        return Results.Json(presets);
    }
    catch (SoapFaultException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();

static string GenerateDefaultApiKey()
{
    var bytes = new byte[32];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(bytes);
    var key = Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")[..32];
    Console.WriteLine($"[SECURITY] Generated API key: {key}");
    Console.WriteLine($"[SECURITY] Set ODM_API_KEY environment variable or ApiKey in appsettings to use a fixed key.");
    return key;
}

static bool IsValidDeviceAddress(string address)
{
    if (address.Contains("://"))
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri))
            return false;
        return uri.Scheme == "http" || uri.Scheme == "https";
    }

    if (IPAddress.TryParse(address, out var ip))
    {
        if (IPAddress.IsLoopback(ip)) return false;
        if (ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any)) return false;
        return true;
    }

    return Regex.IsMatch(address, @"^[a-zA-Z0-9][a-zA-Z0-9\-\.]*[a-zA-Z0-9]$") && address.Length <= 253;
}

static bool IsValidOnvifUrl(string url)
{
    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        return false;

    if (uri.Scheme != "http" && uri.Scheme != "https")
        return false;

    if (uri.Host == "localhost" || uri.Host == "127.0.0.1" || uri.Host == "::1")
        return false;

    if (uri.Host.StartsWith("169.254."))
        return false;

    return true;
}

static string SanitizeString(string input, int maxLength)
{
    if (string.IsNullOrEmpty(input)) return string.Empty;
    var sanitized = input.Trim();
    if (sanitized.Length > maxLength)
        sanitized = sanitized[..maxLength];
    return Regex.Replace(sanitized, @"[<>""'&]", "");
}

record AddDeviceRequest(string Address, string? Name, string? Username, string? Password);
record PtzMoveRequest(string ProfileToken, float Pan, float Tilt, float Zoom);
record PtzStopRequest(string ProfileToken);
