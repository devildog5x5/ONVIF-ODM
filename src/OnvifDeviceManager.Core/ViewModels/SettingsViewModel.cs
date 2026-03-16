using System.Windows.Input;

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

    public SettingsViewModel()
    {
        SaveCommand = new RelayCommand(Save);
        ResetCommand = new RelayCommand(Reset);
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
}
