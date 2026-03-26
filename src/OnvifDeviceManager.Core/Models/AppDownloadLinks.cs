namespace OnvifDeviceManager.Models;

/// <summary>Canonical URLs for in-app "Downloads" hub (README, Releases, CI).</summary>
public static class AppDownloadLinks
{
    public const string ReadmeLatestDirectDownloads =
        "https://github.com/devildog5x5/ONVIF-ODM/blob/main/README.md#latest-direct-download-links";

    public const string ReleasesLatest = "https://github.com/devildog5x5/ONVIF-ODM/releases/latest";

    public const string CiWorkflowMain =
        "https://github.com/devildog5x5/ONVIF-ODM/actions/workflows/dotnet.yml?query=branch%3Amain";
}
