using System.Collections.ObjectModel;

namespace OnvifDeviceManager.Models;

public class NetworkConfiguration
{
    public string IpAddress { get; set; } = string.Empty;
    public string SubnetMask { get; set; } = string.Empty;
    public string Gateway { get; set; } = string.Empty;
    public string PrimaryDns { get; set; } = string.Empty;
    public string SecondaryDns { get; set; } = string.Empty;
    public bool IsDhcp { get; set; }
    public string MacAddress { get; set; } = string.Empty;
    public int HttpPort { get; set; } = 80;
    public int RtspPort { get; set; } = 554;
    public bool IsHttpsEnabled { get; set; }
    public int HttpsPort { get; set; } = 443;
}

public class DeviceUser
{
    public string Username { get; set; } = string.Empty;
    public string UserLevel { get; set; } = string.Empty;
}

public class DeviceEvent
{
    public DateTime Timestamp { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
}

public class SystemDateTimeInfo
{
    public DateTime DeviceTime { get; set; }
    public string TimeZone { get; set; } = string.Empty;
    public bool IsNtp { get; set; }
    public string NtpServer { get; set; } = string.Empty;
}
