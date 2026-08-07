using System.Text.Json;
using OnvifDeviceManager.Models;
using OnvifDeviceManager.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<OnvifDiscoveryService>();
builder.Services.AddSingleton<CredentialStore>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();
app.UseCors();

var connectedDevices = new Dictionary<string, OnvifDevice>();

app.MapGet("/", () => Results.Json(new
{
    service = "ONVIF Device Manager Server",
    version = "1.0.0",
    status = "running",
    endpoints = new[]
    {
        "GET  /api/devices/discover",
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
}));

app.MapGet("/api/status", () => Results.Json(new
{
    status = "running",
    connectedDevices = connectedDevices.Count,
    uptime = DateTime.UtcNow.ToString("o")
}));

app.MapGet("/api/devices/discover", async (int? timeout, OnvifDiscoveryService discovery) =>
{
    var timeoutSec = timeout ?? 5;
    var devices = await discovery.DiscoverDevicesAsync(timeoutSec);

    foreach (var device in devices)
    {
        if (!connectedDevices.ContainsKey(device.Address))
            connectedDevices[device.Address] = device;
    }

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
});

app.MapPost("/api/devices/add", (AddDeviceRequest request) =>
{
    var device = new OnvifDevice
    {
        Address = request.Address,
        ServiceAddress = request.Address.Contains("://")
            ? request.Address
            : $"http://{request.Address}/onvif/device_service",
        Name = request.Name ?? request.Address,
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

record AddDeviceRequest(string Address, string? Name, string? Username, string? Password);
record PtzMoveRequest(string ProfileToken, float Pan, float Tilt, float Zoom);
record PtzStopRequest(string ProfileToken);
