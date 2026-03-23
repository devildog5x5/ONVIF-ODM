using System.Collections.ObjectModel;
using System.Windows.Input;
using OnvifDeviceManager.Models;
using OnvifDeviceManager.Services;

namespace OnvifDeviceManager.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly OnvifDiscoveryService _discoveryService = new();
    private readonly OnvifDeviceService _deviceService = new();
    private readonly OnvifMediaService _mediaService = new();
    private readonly OnvifPtzService _ptzService = new();
    private readonly CredentialStore _credentialStore = new();

    private ViewModelBase? _currentView;
    private OnvifDevice? _selectedDevice;
    private string _statusMessage = "Ready";
    private bool _isDiscovering;
    private string _selectedNavItem = "Discovery";
    private bool _isDeviceConnected;

    public MainViewModel() : this(new DefaultUiDispatcher(), new DefaultClipboardService()) { }

    public MainViewModel(IUiDispatcher dispatcher, IClipboardService clipboard)
    {
        DiscoveryViewModel = new DiscoveryViewModel(_discoveryService, _deviceService, _mediaService, _credentialStore, dispatcher);
        DeviceInfoViewModel = new DeviceInfoViewModel(_deviceService);
        LiveViewViewModel = new LiveViewViewModel(_mediaService, _ptzService, dispatcher, clipboard);
        PtzViewModel = new PtzViewModel(_ptzService);
        ProfilesViewModel = new ProfilesViewModel(_mediaService);
        NetworkViewModel = new NetworkViewModel(_deviceService);
        UsersViewModel = new UsersViewModel(_deviceService);
        EventsViewModel = new EventsViewModel();
        SettingsViewModel = new SettingsViewModel();
        CredentialManagerViewModel = new CredentialManagerViewModel(_credentialStore);

        CurrentView = DiscoveryViewModel;

        NavigateCommand = new RelayCommand(Navigate);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        SwitchDeviceCommand = new RelayCommand(SwitchDevice);

        DiscoveryViewModel.DeviceSelected += OnDeviceSelected;
        DiscoveryViewModel.StatusChanged += OnStatusChanged;

        _ = AutoDiscoverAsync();
    }

    private async Task AutoDiscoverAsync()
    {
        await Task.Delay(500);
        StatusMessage = "Auto-discovering devices...";
        await DiscoveryViewModel.DiscoverDevicesAsync();
    }

    public DiscoveryViewModel DiscoveryViewModel { get; }
    public DeviceInfoViewModel DeviceInfoViewModel { get; }
    public LiveViewViewModel LiveViewViewModel { get; }
    public PtzViewModel PtzViewModel { get; }
    public ProfilesViewModel ProfilesViewModel { get; }
    public NetworkViewModel NetworkViewModel { get; }
    public UsersViewModel UsersViewModel { get; }
    public EventsViewModel EventsViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }
    public CredentialManagerViewModel CredentialManagerViewModel { get; }

    public ObservableCollection<OnvifDevice> Devices => DiscoveryViewModel.Devices;

    public ObservableCollection<OnvifDevice> ConnectedDevices { get; } = new();

    public ViewModelBase? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public OnvifDevice? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (SetProperty(ref _selectedDevice, value))
            {
                IsDeviceConnected = value != null;
                UpdateChildViewModels();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsDiscovering
    {
        get => _isDiscovering;
        set => SetProperty(ref _isDiscovering, value);
    }

    public string SelectedNavItem
    {
        get => _selectedNavItem;
        set => SetProperty(ref _selectedNavItem, value);
    }

    public bool IsDeviceConnected
    {
        get => _isDeviceConnected;
        set => SetProperty(ref _isDeviceConnected, value);
    }

    public ICommand NavigateCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SwitchDeviceCommand { get; }

    private void Navigate(object? parameter)
    {
        var viewName = parameter as string;
        SelectedNavItem = viewName ?? "Discovery";

        CurrentView = viewName switch
        {
            "Discovery" => DiscoveryViewModel,
            "DeviceInfo" => DeviceInfoViewModel,
            "LiveView" => LiveViewViewModel,
            "PTZ" => PtzViewModel,
            "Profiles" => ProfilesViewModel,
            "Network" => NetworkViewModel,
            "Users" => UsersViewModel,
            "Events" => EventsViewModel,
            "Settings" => SettingsViewModel,
            "Credentials" => CredentialManagerViewModel,
            _ => DiscoveryViewModel
        };

        if (viewName == "Discovery")
            _ = DiscoveryViewModel.DiscoverDevicesAsync();
    }

    private void OnDeviceSelected(OnvifDevice device)
    {
        if (!ConnectedDevices.Any(d => d.Address == device.Address))
            ConnectedDevices.Add(device);

        SelectedDevice = device;
        StatusMessage = $"Connected to {device.DisplayName} ({device.Username}@{device.Address})";
        Navigate("DeviceInfo");
    }

    private void SwitchDevice(object? parameter)
    {
        if (parameter is OnvifDevice device && device.IsAuthenticated)
        {
            SelectedDevice = device;
            StatusMessage = $"Switched to {device.DisplayName} ({device.Username}@{device.Address})";
        }
    }

    private void OnStatusChanged(string message)
    {
        StatusMessage = message;
    }

    private void UpdateChildViewModels()
    {
        if (_selectedDevice == null)
        {
            // Clear all child view models when no device selected — ensures left-panel sections
            // display and operate only for the currently selected camera.
            DeviceInfoViewModel.ClearDevice();
            LiveViewViewModel.ClearDevice();
            PtzViewModel.ClearDevice();
            ProfilesViewModel.ClearDevice();
            NetworkViewModel.ClearDevice();
            UsersViewModel.ClearDevice();
            EventsViewModel.ClearDevice();
            return;
        }

        DeviceInfoViewModel.SetDevice(_selectedDevice);
        LiveViewViewModel.SetDevice(_selectedDevice);
        PtzViewModel.SetDevice(_selectedDevice);
        ProfilesViewModel.SetDevice(_selectedDevice);
        NetworkViewModel.SetDevice(_selectedDevice);
        UsersViewModel.SetDevice(_selectedDevice);
        EventsViewModel.SetDevice(_selectedDevice);
    }

    private async Task RefreshAsync()
    {
        if (CurrentView == DiscoveryViewModel)
            await DiscoveryViewModel.DiscoverDevicesAsync();
    }
}

internal class DefaultUiDispatcher : IUiDispatcher
{
    public Task InvokeAsync(Action action) { action(); return Task.CompletedTask; }
}

internal class DefaultClipboardService : IClipboardService
{
    public Task SetTextAsync(string text) => Task.CompletedTask;
}
