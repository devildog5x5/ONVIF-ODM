using System.Collections.ObjectModel;
using System.Windows;
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

    private ViewModelBase? _currentView;
    private OnvifDevice? _selectedDevice;
    private string _statusMessage = "Ready";
    private bool _isDiscovering;
    private string _selectedNavItem = "Discovery";
    private bool _isDeviceConnected;

    public MainViewModel()
    {
        DiscoveryViewModel = new DiscoveryViewModel(_discoveryService, _deviceService, _mediaService);
        DeviceInfoViewModel = new DeviceInfoViewModel(_deviceService);
        LiveViewViewModel = new LiveViewViewModel(_mediaService);
        PtzViewModel = new PtzViewModel(_ptzService);
        ProfilesViewModel = new ProfilesViewModel(_mediaService);
        NetworkViewModel = new NetworkViewModel(_deviceService);
        UsersViewModel = new UsersViewModel(_deviceService);
        EventsViewModel = new EventsViewModel();
        SettingsViewModel = new SettingsViewModel();

        CurrentView = DiscoveryViewModel;

        NavigateCommand = new RelayCommand(Navigate);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);

        DiscoveryViewModel.DeviceSelected += OnDeviceSelected;
        DiscoveryViewModel.StatusChanged += OnStatusChanged;
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

    public ObservableCollection<OnvifDevice> Devices => DiscoveryViewModel.Devices;

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
            _ => DiscoveryViewModel
        };
    }

    private void OnDeviceSelected(OnvifDevice device)
    {
        SelectedDevice = device;
        StatusMessage = $"Connected to {device.DisplayName}";
        Navigate("DeviceInfo");
    }

    private void OnStatusChanged(string message)
    {
        StatusMessage = message;
    }

    private void UpdateChildViewModels()
    {
        if (_selectedDevice == null) return;

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
        {
            await DiscoveryViewModel.DiscoverDevicesAsync();
        }
    }
}
