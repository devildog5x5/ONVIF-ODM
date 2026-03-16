using System.Xml.Linq;
using OnvifDeviceManager.Models;

namespace OnvifDeviceManager.Services;

public class OnvifDeviceService : IDisposable
{
    private readonly SoapClient _soapClient = new();

    private static readonly XNamespace TdsNs = "http://www.onvif.org/ver10/device/wsdl";
    private static readonly XNamespace TtNs = "http://www.onvif.org/ver10/schema";

    public async Task<DeviceCapabilities> GetCapabilitiesAsync(string serviceUrl, string? username = null, string? password = null)
    {
        var body = new XElement(TdsNs + "GetCapabilities",
            new XElement(TdsNs + "Category", "All"));

        var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);
        return ParseCapabilities(response);
    }

    public async Task<OnvifDevice> GetDeviceInformationAsync(string serviceUrl, string? username = null, string? password = null)
    {
        var body = new XElement(TdsNs + "GetDeviceInformation");
        var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);

        var info = response.Descendants(TdsNs + "GetDeviceInformationResponse").FirstOrDefault();
        if (info == null)
            throw new InvalidOperationException("Failed to get device information");

        return new OnvifDevice
        {
            Manufacturer = info.Element(TdsNs + "Manufacturer")?.Value ?? string.Empty,
            Model = info.Element(TdsNs + "Model")?.Value ?? string.Empty,
            FirmwareVersion = info.Element(TdsNs + "FirmwareVersion")?.Value ?? string.Empty,
            SerialNumber = info.Element(TdsNs + "SerialNumber")?.Value ?? string.Empty,
            HardwareId = info.Element(TdsNs + "HardwareId")?.Value ?? string.Empty
        };
    }

    public async Task<string> GetHostnameAsync(string serviceUrl, string? username = null, string? password = null)
    {
        var body = new XElement(TdsNs + "GetHostname");
        var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);

        var hostname = response.Descendants(TdsNs + "GetHostnameResponse")
            .FirstOrDefault()
            ?.Descendants(TtNs + "Name")
            .FirstOrDefault()?.Value;

        return hostname ?? string.Empty;
    }

    public async Task<NetworkConfiguration> GetNetworkInterfacesAsync(string serviceUrl, string? username = null, string? password = null)
    {
        var body = new XElement(TdsNs + "GetNetworkInterfaces");
        var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);

        var config = new NetworkConfiguration();
        var networkInterface = response.Descendants(TdsNs + "NetworkInterfaces").FirstOrDefault();

        if (networkInterface != null)
        {
            config.MacAddress = networkInterface.Descendants(TtNs + "HwAddress").FirstOrDefault()?.Value ?? string.Empty;

            var ipv4Config = networkInterface.Descendants(TtNs + "IPv4").FirstOrDefault();
            if (ipv4Config != null)
            {
                config.IsDhcp = bool.TryParse(ipv4Config.Element(TtNs + "DHCP")?.Value, out var dhcp) && dhcp;
                var manual = ipv4Config.Descendants(TtNs + "Manual").FirstOrDefault();
                if (manual != null)
                {
                    config.IpAddress = manual.Element(TtNs + "Address")?.Value ?? string.Empty;
                    var prefixLength = int.TryParse(manual.Element(TtNs + "PrefixLength")?.Value, out var pl) ? pl : 24;
                    config.SubnetMask = PrefixLengthToSubnetMask(prefixLength);
                }
            }
        }

        return config;
    }

    public async Task<SystemDateTimeInfo> GetSystemDateTimeAsync(string serviceUrl, string? username = null, string? password = null)
    {
        var body = new XElement(TdsNs + "GetSystemDateAndTime");
        var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);

        var dateTime = response.Descendants(TdsNs + "GetSystemDateAndTimeResponse").FirstOrDefault();
        var info = new SystemDateTimeInfo();

        if (dateTime != null)
        {
            var sysDateTime = dateTime.Descendants(TtNs + "SystemDateAndTime").FirstOrDefault();
            if (sysDateTime != null)
            {
                var dateTimeType = sysDateTime.Element(TtNs + "DateTimeType")?.Value;
                info.IsNtp = dateTimeType == "NTP";
                info.TimeZone = sysDateTime.Descendants(TtNs + "TZ").FirstOrDefault()?.Value ?? string.Empty;

                var utcDate = sysDateTime.Element(TtNs + "UTCDateTime");
                if (utcDate != null)
                {
                    var date = utcDate.Element(TtNs + "Date");
                    var time = utcDate.Element(TtNs + "Time");
                    if (date != null && time != null)
                    {
                        int.TryParse(date.Element(TtNs + "Year")?.Value, out var year);
                        int.TryParse(date.Element(TtNs + "Month")?.Value, out var month);
                        int.TryParse(date.Element(TtNs + "Day")?.Value, out var day);
                        int.TryParse(time.Element(TtNs + "Hour")?.Value, out var hour);
                        int.TryParse(time.Element(TtNs + "Minute")?.Value, out var minute);
                        int.TryParse(time.Element(TtNs + "Second")?.Value, out var second);
                        info.DeviceTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
                    }
                }
            }
        }

        return info;
    }

    public async Task<List<DeviceUser>> GetUsersAsync(string serviceUrl, string? username = null, string? password = null)
    {
        var body = new XElement(TdsNs + "GetUsers");
        var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);

        var users = new List<DeviceUser>();
        foreach (var user in response.Descendants(TdsNs + "User"))
        {
            users.Add(new DeviceUser
            {
                Username = user.Element(TtNs + "Username")?.Value ?? string.Empty,
                UserLevel = user.Element(TtNs + "UserLevel")?.Value ?? string.Empty
            });
        }

        return users;
    }

    public async Task SetHostnameAsync(string serviceUrl, string hostname, string? username = null, string? password = null)
    {
        var body = new XElement(TdsNs + "SetHostname",
            new XElement(TdsNs + "Name", hostname));

        await _soapClient.SendRequestAsync(serviceUrl, body, username, password);
    }

    public async Task SystemRebootAsync(string serviceUrl, string? username = null, string? password = null)
    {
        var body = new XElement(TdsNs + "SystemReboot");
        await _soapClient.SendRequestAsync(serviceUrl, body, username, password);
    }

    public async Task FactoryResetAsync(string serviceUrl, string factoryDefault, string? username = null, string? password = null)
    {
        var body = new XElement(TdsNs + "SetSystemFactoryDefault",
            new XElement(TdsNs + "FactoryDefault", factoryDefault));

        await _soapClient.SendRequestAsync(serviceUrl, body, username, password);
    }

    public async Task CreateUserAsync(string serviceUrl, string newUsername, string newPassword, string userLevel, string? username = null, string? password = null)
    {
        var body = new XElement(TdsNs + "CreateUsers",
            new XElement(TdsNs + "User",
                new XElement(TtNs + "Username", newUsername),
                new XElement(TtNs + "Password", newPassword),
                new XElement(TtNs + "UserLevel", userLevel)));

        await _soapClient.SendRequestAsync(serviceUrl, body, username, password);
    }

    public async Task DeleteUserAsync(string serviceUrl, string userToDelete, string? username = null, string? password = null)
    {
        var body = new XElement(TdsNs + "DeleteUsers",
            new XElement(TdsNs + "Username", userToDelete));

        await _soapClient.SendRequestAsync(serviceUrl, body, username, password);
    }

    private DeviceCapabilities ParseCapabilities(XElement response)
    {
        var caps = new DeviceCapabilities();
        var capabilities = response.Descendants(TtNs + "Capabilities").FirstOrDefault();
        if (capabilities == null) return caps;

        var media = capabilities.Element(TtNs + "Media");
        if (media != null)
        {
            caps.HasMedia = true;
            caps.MediaServiceAddress = media.Attribute("XAddr")?.Value
                ?? media.Element(TtNs + "XAddr")?.Value;
        }

        var ptz = capabilities.Element(TtNs + "PTZ");
        if (ptz != null)
        {
            caps.HasPtz = true;
            caps.PtzServiceAddress = ptz.Attribute("XAddr")?.Value
                ?? ptz.Element(TtNs + "XAddr")?.Value;
        }

        var imaging = capabilities.Element(TtNs + "Imaging");
        if (imaging != null)
        {
            caps.HasImaging = true;
            caps.ImagingServiceAddress = imaging.Attribute("XAddr")?.Value
                ?? imaging.Element(TtNs + "XAddr")?.Value;
        }

        var events = capabilities.Element(TtNs + "Events");
        if (events != null)
        {
            caps.HasEvents = true;
            caps.EventsServiceAddress = events.Attribute("XAddr")?.Value
                ?? events.Element(TtNs + "XAddr")?.Value;
        }

        var analytics = capabilities.Element(TtNs + "Analytics");
        if (analytics != null)
        {
            caps.HasAnalytics = true;
            caps.AnalyticsServiceAddress = analytics.Attribute("XAddr")?.Value
                ?? analytics.Element(TtNs + "XAddr")?.Value;
        }

        return caps;
    }

    private static string PrefixLengthToSubnetMask(int prefixLength)
    {
        var mask = prefixLength == 0 ? 0 : uint.MaxValue << (32 - prefixLength);
        var bytes = BitConverter.GetBytes(mask);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return $"{bytes[0]}.{bytes[1]}.{bytes[2]}.{bytes[3]}";
    }

    public void Dispose()
    {
        _soapClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
