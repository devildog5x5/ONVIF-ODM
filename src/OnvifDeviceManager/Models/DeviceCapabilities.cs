namespace OnvifDeviceManager.Models;

public class DeviceCapabilities
{
    public bool HasPtz { get; set; }
    public bool HasMedia { get; set; }
    public bool HasMedia2 { get; set; }
    public bool HasImaging { get; set; }
    public bool HasEvents { get; set; }
    public bool HasAnalytics { get; set; }
    public bool HasRecording { get; set; }
    public bool HasReplay { get; set; }
    public bool HasSearch { get; set; }
    public bool HasIO { get; set; }

    public string? MediaServiceAddress { get; set; }
    public string? PtzServiceAddress { get; set; }
    public string? ImagingServiceAddress { get; set; }
    public string? EventsServiceAddress { get; set; }
    public string? AnalyticsServiceAddress { get; set; }
    public string? DeviceIOServiceAddress { get; set; }
}
