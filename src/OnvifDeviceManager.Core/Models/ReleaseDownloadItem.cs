namespace OnvifDeviceManager.Models;

/// <summary>Single file attached to a GitHub Release (name + browser_download_url).</summary>
public sealed class ReleaseDownloadItem
{
    public string Name { get; init; } = "";
    public string Url { get; init; } = "";
}
