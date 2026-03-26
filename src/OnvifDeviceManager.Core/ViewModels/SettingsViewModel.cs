using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using OnvifDeviceManager.Models;
using OnvifDeviceManager.Services;

namespace OnvifDeviceManager.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private int _discoveryTimeout = 5;
    private int _connectionTimeout = 10;
    private int _snapshotRefreshRate = 1000;
    private bool _autoDiscoverOnStartup;
    private bool _rememberCredentials;
    private string _defaultUsername = "admin";
    private string _defaultPassword = string.Empty;
    private string _statusText = string.Empty;
    private string _latestReleaseTag = "—";
    private string _downloadsSectionStatus = "Click Refresh to load files from the latest GitHub Release.";

    public SettingsViewModel()
    {
        SaveCommand = new RelayCommand(Save);
        ResetCommand = new RelayCommand(Reset);
        OpenUrlCommand = new RelayCommand(OpenUrl);
        RefreshLatestDownloadsCommand = new AsyncRelayCommand(RefreshLatestDownloadsAsync);
        LatestReleaseAssets = new ObservableCollection<ReleaseDownloadItem>();
    }

    public int DiscoveryTimeout
    {
        get => _discoveryTimeout;
        set => SetProperty(ref _discoveryTimeout, Math.Clamp(value, 1, 30));
    }

    public int ConnectionTimeout
    {
        get => _connectionTimeout;
        set => SetProperty(ref _connectionTimeout, Math.Clamp(value, 1, 60));
    }

    public int SnapshotRefreshRate
    {
        get => _snapshotRefreshRate;
        set => SetProperty(ref _snapshotRefreshRate, Math.Clamp(value, 100, 10000));
    }

    public bool AutoDiscoverOnStartup
    {
        get => _autoDiscoverOnStartup;
        set => SetProperty(ref _autoDiscoverOnStartup, value);
    }

    public bool RememberCredentials
    {
        get => _rememberCredentials;
        set => SetProperty(ref _rememberCredentials, value);
    }

    public string DefaultUsername
    {
        get => _defaultUsername;
        set => SetProperty(ref _defaultUsername, value);
    }

    public string DefaultPassword
    {
        get => _defaultPassword;
        set => SetProperty(ref _defaultPassword, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand ResetCommand { get; }

    /// <summary>Opens a URL in the default browser (CommandParameter = string url).</summary>
    public ICommand OpenUrlCommand { get; }

    public ICommand RefreshLatestDownloadsCommand { get; }

    public ObservableCollection<ReleaseDownloadItem> LatestReleaseAssets { get; }

    public string LatestReleaseTag
    {
        get => _latestReleaseTag;
        set => SetProperty(ref _latestReleaseTag, value);
    }

    public string DownloadsSectionStatus
    {
        get => _downloadsSectionStatus;
        set => SetProperty(ref _downloadsSectionStatus, value);
    }

    private void Save()
    {
        StatusText = "Settings saved";
    }

    private void Reset()
    {
        DiscoveryTimeout = 5;
        ConnectionTimeout = 10;
        SnapshotRefreshRate = 1000;
        AutoDiscoverOnStartup = false;
        RememberCredentials = false;
        DefaultUsername = "admin";
        DefaultPassword = string.Empty;
        StatusText = "Settings reset to defaults";
    }

    private static void OpenUrl(object? parameter)
    {
        if (parameter is not string url || string.IsNullOrWhiteSpace(url))
            return;
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch
        {
            /* ignore */
        }
    }

    private async Task RefreshLatestDownloadsAsync()
    {
        DownloadsSectionStatus = "Loading…";
        LatestReleaseAssets.Clear();
        LatestReleaseTag = "—";

        var (tag, assets, err) = await GitHubLatestReleaseApi.TryGetLatestAsync().ConfigureAwait(true);
        if (err != null)
        {
            LatestReleaseTag = "—";
            DownloadsSectionStatus = $"Could not load release assets: {err}";
            return;
        }

        LatestReleaseTag = string.IsNullOrWhiteSpace(tag) ? "—" : tag!;
        foreach (var a in assets)
            LatestReleaseAssets.Add(a);

        DownloadsSectionStatus = assets.Count == 0
            ? "Latest release has no downloadable assets yet."
            : $"Loaded {assets.Count} file(s). Use Open to download in your browser.";
    }
}
