using System.Xml.Linq;
using OnvifDeviceManager.Models;

namespace OnvifDeviceManager.Services;

public class OnvifDeviceService : IDisposable
{
    private readonly SoapClient _soapClient = new();

    private static readonly XNamespace TdsNs = "http://www.onvif.org/ver10/device/wsdl";
    private static readonly XNamespace TtNs = "http://www.onvif.org/ver10/schema";

    private static XElement? FindElement(XElement parent, string localName)
    {
        return parent.Descendants().FirstOrDefault(e => e.Name.LocalName == localName);
    }

    private static IEnumerable<XElement> FindElements(XElement parent, string localName)
    {
        return parent.Descendants().Where(e => e.Name.LocalName == localName);
    }

    private static string GetValue(XElement? parent, string localName, string fallback = "")
    {
        if (parent == null) return fallback;
        return parent.Descendants().FirstOrDefault(e => e.Name.LocalName == localName)?.Value ?? fallback;
    }

    public async Task<DeviceCapabilities> GetCapabilitiesAsync(string serviceUrl, string? username = null, string? password = null)
    {
        try
        {
            var body = new XElement(TdsNs + "GetCapabilities",
                new XElement(TdsNs + "Category", "All"));

            var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);
            return ParseCapabilities(response);
        }
        catch (SoapFaultException) { throw; }
        catch (Exception ex)
        {
            CrashLogger.Log("GetCapabilitiesAsync", ex);
            throw new SoapFaultException($"Failed to get capabilities: {ex.Message}");
        }
    }

    public async Task<OnvifDevice> GetDeviceInformationAsync(string serviceUrl, string? username = null, string? password = null)
    {
        try
        {
            var body = new XElement(TdsNs + "GetDeviceInformation");
            var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);

            var info = FindElement(response, "GetDeviceInformationResponse");
            if (info == null)
            {
                CrashLogger.Log($"GetDeviceInformationResponse not found in response. Elements: {string.Join(", ", response.Descendants().Select(e => e.Name.LocalName).Distinct())}");
                return new OnvifDevice();
            }

            return new OnvifDevice
            {
                Manufacturer = GetValue(info, "Manufacturer"),
                Model = GetValue(info, "Model"),
                FirmwareVersion = GetValue(info, "FirmwareVersion"),
                SerialNumber = GetValue(info, "SerialNumber"),
                HardwareId = GetValue(info, "HardwareId")
            };
        }
        catch (SoapFaultException) { throw; }
        catch (Exception ex)
        {
            CrashLogger.Log("GetDeviceInformationAsync", ex);
            throw new SoapFaultException($"Failed to get device information: {ex.Message}");
        }
    }

    public async Task<string> GetHostnameAsync(string serviceUrl, string? username = null, string? password = null)
    {
        try
        {
            var body = new XElement(TdsNs + "GetHostname");
            var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);

            var hostnameResponse = FindElement(response, "GetHostnameResponse");
            return hostnameResponse != null ? GetValue(hostnameResponse, "Name") : string.Empty;
        }
        catch (SoapFaultException) { throw; }
        catch (Exception ex)
        {
            CrashLogger.Log("GetHostnameAsync", ex);
            return string.Empty;
        }
    }

    public async Task<NetworkConfiguration> GetNetworkInterfacesAsync(string serviceUrl, string? username = null, string? password = null)
    {
        try
        {
            var body = new XElement(TdsNs + "GetNetworkInterfaces");
            var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);

            var config = new NetworkConfiguration();
            var networkInterface = FindElement(response, "NetworkInterfaces");

            if (networkInterface != null)
            {
                config.MacAddress = GetValue(networkInterface, "HwAddress");

                var ipv4Config = FindElement(networkInterface, "IPv4");
                if (ipv4Config != null)
                {
                    config.IsDhcp = bool.TryParse(
                        ipv4Config.Descendants().FirstOrDefault(e => e.Name.LocalName == "DHCP")?.Value,
                        out var dhcp) && dhcp;

                    var fromDhcp = FindElement(ipv4Config, "FromDHCP");
                    var manual = FindElement(ipv4Config, "Manual");
                    var addrSource = fromDhcp ?? manual;

                    if (addrSource != null)
                    {
                        config.IpAddress = GetValue(addrSource, "Address");
                        var prefixLength = int.TryParse(
                            addrSource.Descendants().FirstOrDefault(e => e.Name.LocalName == "PrefixLength")?.Value,
                            out var pl) ? pl : 24;
                        config.SubnetMask = PrefixLengthToSubnetMask(prefixLength);
                    }
                }
            }

            return config;
        }
        catch (SoapFaultException) { throw; }
        catch (Exception ex)
        {
            CrashLogger.Log("GetNetworkInterfacesAsync", ex);
            return new NetworkConfiguration();
        }
    }

    public async Task<SystemDateTimeInfo> GetSystemDateTimeAsync(string serviceUrl, string? username = null, string? password = null)
    {
        try
        {
            var body = new XElement(TdsNs + "GetSystemDateAndTime");
            var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);

            var info = new SystemDateTimeInfo();
            var sysDateTime = FindElement(response, "SystemDateAndTime");
            if (sysDateTime == null) return info;

            info.IsNtp = GetValue(sysDateTime, "DateTimeType") == "NTP";
            info.TimeZone = GetValue(sysDateTime, "TZ");

            var utcDate = FindElement(sysDateTime, "UTCDateTime");
            if (utcDate != null)
            {
                int.TryParse(GetValue(utcDate, "Year", "0"), out var year);
                int.TryParse(GetValue(utcDate, "Month", "0"), out var month);
                int.TryParse(GetValue(utcDate, "Day", "0"), out var day);
                int.TryParse(GetValue(utcDate, "Hour", "0"), out var hour);
                int.TryParse(GetValue(utcDate, "Minute", "0"), out var minute);
                int.TryParse(GetValue(utcDate, "Second", "0"), out var second);

                if (year > 0 && month > 0 && day > 0)
                    info.DeviceTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
            }

            return info;
        }
        catch (SoapFaultException) { throw; }
        catch (Exception ex)
        {
            CrashLogger.Log("GetSystemDateTimeAsync", ex);
            return new SystemDateTimeInfo();
        }
    }

    public async Task<List<DeviceUser>> GetUsersAsync(string serviceUrl, string? username = null, string? password = null)
    {
        try
        {
            var body = new XElement(TdsNs + "GetUsers");
            var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);

            var users = new List<DeviceUser>();
            foreach (var user in FindElements(response, "User"))
            {
                users.Add(new DeviceUser
                {
                    Username = GetValue(user, "Username"),
                    UserLevel = GetValue(user, "UserLevel")
                });
            }
            return users;
        }
        catch (SoapFaultException) { throw; }
        catch (Exception ex)
        {
            CrashLogger.Log("GetUsersAsync", ex);
            return new List<DeviceUser>();
        }
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

        try
        {
            var capabilities = FindElement(response, "Capabilities");
            if (capabilities == null) return caps;

            var media = FindElement(capabilities, "Media");
            if (media != null)
            {
                caps.HasMedia = true;
                caps.MediaServiceAddress = media.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "XAddr")?.Value
                    ?? media.Attribute("XAddr")?.Value;
            }

            var ptz = FindElement(capabilities, "PTZ");
            if (ptz != null)
            {
                caps.HasPtz = true;
                caps.PtzServiceAddress = ptz.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "XAddr")?.Value
                    ?? ptz.Attribute("XAddr")?.Value;
            }

            var imaging = FindElement(capabilities, "Imaging");
            if (imaging != null)
            {
                caps.HasImaging = true;
                caps.ImagingServiceAddress = imaging.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "XAddr")?.Value;
            }

            var events = FindElement(capabilities, "Events");
            if (events != null)
            {
                caps.HasEvents = true;
                caps.EventsServiceAddress = events.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "XAddr")?.Value;
            }

            var analytics = FindElement(capabilities, "Analytics");
            if (analytics != null)
            {
                caps.HasAnalytics = true;
                caps.AnalyticsServiceAddress = analytics.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "XAddr")?.Value;
            }
        }
        catch (Exception ex)
        {
            CrashLogger.Log("ParseCapabilities", ex);
        }

        return caps;
    }

    private static string PrefixLengthToSubnetMask(int prefixLength)
    {
        prefixLength = Math.Clamp(prefixLength, 0, 32);
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
