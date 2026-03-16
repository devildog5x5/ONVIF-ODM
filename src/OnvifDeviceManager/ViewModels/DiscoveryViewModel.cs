using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Threading;
using OnvifDeviceManager.Models;
using OnvifDeviceManager.Services;

namespace OnvifDeviceManager.ViewModels;

public class DiscoveryViewModel : ViewModelBase
{
    private readonly OnvifDiscoveryService _discoveryService;
    private readonly OnvifDeviceService _deviceService;
    private readonly OnvifMediaService _mediaService;
    private CancellationTokenSource? _discoveryCts;

    private bool _isDiscovering;
    private OnvifDevice? _selectedDevice;
    private string _manualAddress = string.Empty;
    private string _username = "admin";
    private string _password = string.Empty;
    private int _discoveryTimeout = 5;
    private string _statusText = "Click Discover to scan the network for ONVIF devices";
    private bool _isConnecting;

    public DiscoveryViewModel(OnvifDiscoveryService discoveryService, OnvifDeviceService deviceService, OnvifMediaService mediaService)
    {
        _discoveryService = discoveryService;
        _deviceService = deviceService;
        _mediaService = mediaService;

        DiscoverCommand = new AsyncRelayCommand(DiscoverDevicesAsync, () => !IsDiscovering);
        StopDiscoveryCommand = new RelayCommand(StopDiscovery, () => IsDiscovering);
        ConnectCommand = new AsyncRelayCommand(ConnectToDeviceAsync, () => SelectedDevice != null && !IsConnecting);
        AddManualCommand = new AsyncRelayCommand(AddManualDeviceAsync, () => !string.IsNullOrWhiteSpace(ManualAddress));
        RemoveDeviceCommand = new RelayCommand(RemoveDevice, () => SelectedDevice != null);
    }

    public event Action<OnvifDevice>? DeviceSelected;
    public event Action<string>? StatusChanged;

    public ObservableCollection<OnvifDevice> Devices { get; } = new();

    public bool IsDiscovering
    {
        get => _isDiscovering;
        set => SetProperty(ref _isDiscovering, value);
    }

    public bool IsConnecting
    {
        get => _isConnecting;
        set => SetProperty(ref _isConnecting, value);
    }

    public OnvifDevice? SelectedDevice
    {
        get => _selectedDevice;
        set => SetProperty(ref _selectedDevice, value);
    }

    public string ManualAddress
    {
        get => _manualAddress;
        set => SetProperty(ref _manualAddress, value);
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public int DiscoveryTimeout
    {
        get => _discoveryTimeout;
        set => SetProperty(ref _discoveryTimeout, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public ICommand DiscoverCommand { get; }
    public ICommand StopDiscoveryCommand { get; }
    public ICommand ConnectCommand { get; }
    public ICommand AddManualCommand { get; }
    public ICommand RemoveDeviceCommand { get; }

    public async Task DiscoverDevicesAsync()
    {
        IsDiscovering = true;
        StatusText = "Scanning network for ONVIF devices...";
        StatusChanged?.Invoke("Discovering devices...");

        _discoveryCts?.Cancel();
        _discoveryCts = new CancellationTokenSource();

        try
        {
            Devices.Clear();
            var devices = await _discoveryService.DiscoverDevicesAsync(DiscoveryTimeout, _discoveryCts.Token);

            foreach (var device in devices)
            {
                await Dispatcher.UIThread.InvokeAsync(() => Devices.Add(device));
            }

            StatusText = $"Discovery complete. Found {devices.Count} device(s)";
            StatusChanged?.Invoke($"Found {devices.Count} ONVIF device(s)");
        }
        catch (OperationCanceledException)
        {
            StatusText = "Discovery cancelled";
            StatusChanged?.Invoke("Discovery cancelled");
        }
        catch (Exception ex)
        {
            StatusText = $"Discovery error: {ex.Message}";
            StatusChanged?.Invoke($"Error: {ex.Message}");
        }
        finally
        {
            IsDiscovering = false;
        }
    }

    private void StopDiscovery()
    {
        _discoveryCts?.Cancel();
    }

    private async Task ConnectToDeviceAsync()
    {
        if (SelectedDevice == null) return;

        IsConnecting = true;
        StatusText = $"Connecting to {SelectedDevice.DisplayName}...";
        StatusChanged?.Invoke($"Connecting to {SelectedDevice.DisplayName}...");

        try
        {
            SelectedDevice.Username = Username;
            SelectedDevice.Password = Password;

            var deviceInfo = await _deviceService.GetDeviceInformationAsync(
                SelectedDevice.ServiceAddress, Username, Password);

            SelectedDevice.Manufacturer = deviceInfo.Manufacturer;
            SelectedDevice.Model = deviceInfo.Model;
            SelectedDevice.FirmwareVersion = deviceInfo.FirmwareVersion;
            SelectedDevice.SerialNumber = deviceInfo.SerialNumber;
            SelectedDevice.HardwareId = deviceInfo.HardwareId;

            var capabilities = await _deviceService.GetCapabilitiesAsync(
                SelectedDevice.ServiceAddress, Username, Password);
            SelectedDevice.Capabilities = capabilities;

            if (capabilities.HasMedia && !string.IsNullOrEmpty(capabilities.MediaServiceAddress))
            {
                var profiles = await _mediaService.GetProfilesAsync(
                    capabilities.MediaServiceAddress, Username, Password);

                SelectedDevice.Profiles.Clear();
                foreach (var profile in profiles)
                {
                    try
                    {
                        profile.StreamUri = await _mediaService.GetStreamUriAsync(
                            capabilities.MediaServiceAddress, profile.Token, Username, Password);
                    }
                    catch { }

                    try
                    {
                        profile.SnapshotUri = await _mediaService.GetSnapshotUriAsync(
                            capabilities.MediaServiceAddress, profile.Token, Username, Password);
                    }
                    catch { }

                    SelectedDevice.Profiles.Add(profile);
                }
            }

            SelectedDevice.IsAuthenticated = true;
            SelectedDevice.Status = DeviceStatus.Online;
            StatusText = $"Connected to {SelectedDevice.DisplayName}";
            StatusChanged?.Invoke($"Connected to {SelectedDevice.DisplayName}");

            DeviceSelected?.Invoke(SelectedDevice);
        }
        catch (SoapFaultException ex)
        {
            StatusText = $"Authentication failed: {ex.Message}";
            StatusChanged?.Invoke($"Authentication failed: {ex.Message}");
            SelectedDevice.Status = DeviceStatus.Error;
        }
        catch (Exception ex)
        {
            StatusText = $"Connection failed: {ex.Message}";
            StatusChanged?.Invoke($"Connection failed: {ex.Message}");
            SelectedDevice.Status = DeviceStatus.Error;
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private async Task AddManualDeviceAsync()
    {
        if (string.IsNullOrWhiteSpace(ManualAddress)) return;

        var address = ManualAddress.Trim();
        if (!address.StartsWith("http"))
        {
            address = $"http://{address}/onvif/device_service";
        }
        else if (!address.Contains("/onvif/"))
        {
            address = address.TrimEnd('/') + "/onvif/device_service";
        }

        var device = new OnvifDevice
        {
            Address = new Uri(address).Host,
            ServiceAddress = address,
            Name = new Uri(address).Host,
            Status = DeviceStatus.Unknown,
            LastSeen = DateTime.Now
        };

        try
        {
            StatusText = $"Probing {address}...";
            StatusChanged?.Invoke($"Probing {address}...");

            var dateTime = await _deviceService.GetSystemDateTimeAsync(address);
            device.IsOnline = true;
            device.Status = DeviceStatus.Online;
            StatusText = $"Device found at {address}";
        }
        catch
        {
            device.Status = DeviceStatus.Unknown;
            StatusText = $"Added {address} (unverified)";
        }

        Devices.Add(device);
        SelectedDevice = device;
        ManualAddress = string.Empty;
    }

    private void RemoveDevice()
    {
        if (SelectedDevice == null) return;
        Devices.Remove(SelectedDevice);
        SelectedDevice = null;
    }
}
