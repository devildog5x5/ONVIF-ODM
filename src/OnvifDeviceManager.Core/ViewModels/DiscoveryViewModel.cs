using System.Collections.ObjectModel;
using System.Windows.Input;
using OnvifDeviceManager.Models;
using OnvifDeviceManager.Services;

namespace OnvifDeviceManager.ViewModels;

public class DiscoveryViewModel : ViewModelBase
{
    private readonly OnvifDiscoveryService _discoveryService;
    private readonly OnvifDeviceService _deviceService;
    private readonly OnvifMediaService _mediaService;
    private readonly CredentialStore _credentialStore;
    private readonly IUiDispatcher _dispatcher;
    private CancellationTokenSource? _discoveryCts;

    private bool _isDiscovering;
    private OnvifDevice? _selectedDevice;
    private string _manualAddress = string.Empty;
    private string _username = "admin";
    private string _password = string.Empty;
    private int _discoveryTimeout = 5;
    private string _statusText = "Click Discover to scan the network for ONVIF devices";
    private bool _isConnecting;
    private bool _saveCredentials = true;
    private string _credentialSource = string.Empty;

    public DiscoveryViewModel(OnvifDiscoveryService discoveryService, OnvifDeviceService deviceService,
        OnvifMediaService mediaService, CredentialStore credentialStore, IUiDispatcher dispatcher)
    {
        _discoveryService = discoveryService;
        _deviceService = deviceService;
        _mediaService = mediaService;
        _credentialStore = credentialStore;
        _dispatcher = dispatcher;

        DiscoverCommand = new AsyncRelayCommand(DiscoverDevicesAsync);
        StopDiscoveryCommand = new RelayCommand(StopDiscovery);
        ConnectCommand = new AsyncRelayCommand(ConnectToDeviceAsync);
        ConnectAllCommand = new AsyncRelayCommand(ConnectAllDevicesAsync);
        AddManualCommand = new AsyncRelayCommand(AddManualDeviceAsync);
        RemoveDeviceCommand = new RelayCommand(RemoveDevice);
        DisconnectCommand = new RelayCommand(DisconnectDevice);
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
        set
        {
            if (SetProperty(ref _selectedDevice, value) && value != null)
                LoadSavedCredentials(value);
        }
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

    public bool SaveCredentials
    {
        get => _saveCredentials;
        set => SetProperty(ref _saveCredentials, value);
    }

    public string CredentialSource
    {
        get => _credentialSource;
        set => SetProperty(ref _credentialSource, value);
    }

    public ICommand DiscoverCommand { get; }
    public ICommand StopDiscoveryCommand { get; }
    public ICommand ConnectCommand { get; }
    public ICommand ConnectAllCommand { get; }
    public ICommand AddManualCommand { get; }
    public ICommand RemoveDeviceCommand { get; }
    public ICommand DisconnectCommand { get; }

    private void LoadSavedCredentials(OnvifDevice device)
    {
        if (device.IsAuthenticated)
        {
            Username = device.Username;
            Password = device.Password;
            CredentialSource = "Active connection";
            return;
        }

        var resolved = _credentialStore.ResolveCredentials(device.Address);
        if (resolved.HasValue)
        {
            Username = resolved.Value.Username;
            Password = resolved.Value.Password;

            var group = _credentialStore.GetGroupForDevice(device.Address);
            CredentialSource = group != null
                ? $"Group: {group.Name}"
                : "Saved (device-specific)";
        }
        else
        {
            Username = "admin";
            Password = string.Empty;
            CredentialSource = "Default";
        }
    }

    public async Task DiscoverDevicesAsync()
    {
        if (IsDiscovering) return;

        IsDiscovering = true;
        StatusText = "Scanning network for ONVIF devices...";
        StatusChanged?.Invoke("Discovering devices...");

        _discoveryCts?.Cancel();
        _discoveryCts = new CancellationTokenSource();

        try
        {
            var existingConnected = Devices.Where(d => d.IsAuthenticated).ToList();
            Devices.Clear();
            foreach (var d in existingConnected) Devices.Add(d);

            var devices = await _discoveryService.DiscoverDevicesAsync(DiscoveryTimeout, _discoveryCts.Token);

            foreach (var device in devices)
            {
                if (Devices.Any(d => d.Address == device.Address)) continue;

                var saved = _credentialStore.ResolveCredentials(device.Address);
                if (saved.HasValue)
                {
                    device.Username = saved.Value.Username;
                    device.Password = saved.Value.Password;
                }

                await _dispatcher.InvokeAsync(() => Devices.Add(device));
            }

            StatusText = $"Discovery complete. Found {Devices.Count} device(s)";
            StatusChanged?.Invoke($"Found {Devices.Count} ONVIF device(s)");
        }
        catch (OperationCanceledException)
        {
            StatusText = "Discovery cancelled";
        }
        catch (Exception ex)
        {
            StatusText = $"Discovery error: {ex.Message}";
            CrashLogger.Log("DiscoverDevicesAsync", ex);
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
        if (SelectedDevice == null)
        {
            StatusText = "Select a device first";
            return;
        }
        if (string.IsNullOrWhiteSpace(Username))
        {
            StatusText = "Enter a username";
            return;
        }

        IsConnecting = true;
        try
        {
            await ConnectSingleDeviceAsync(SelectedDevice, Username, Password);
            DeviceSelected?.Invoke(SelectedDevice);
        }
        catch (Exception ex)
        {
            StatusText = $"Connection failed: {ex.Message}";
            CrashLogger.Log("ConnectToDeviceAsync", ex);
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private async Task ConnectAllDevicesAsync()
    {
        if (Devices.Count == 0)
        {
            StatusText = "No devices to connect. Run discovery first.";
            return;
        }

        IsConnecting = true;
        var total = Devices.Count;
        var connected = 0;
        var failed = 0;

        StatusText = $"Connecting to {total} device(s)...";
        StatusChanged?.Invoke($"Batch connecting {total} device(s)...");

        foreach (var device in Devices.ToList())
        {
            if (device.IsAuthenticated)
            {
                connected++;
                continue;
            }

            var creds = _credentialStore.ResolveCredentials(device.Address);
            var user = creds?.Username ?? Username;
            var pass = creds?.Password ?? Password;

            try
            {
                await ConnectSingleDeviceAsync(device, user, pass);
                connected++;
            }
            catch
            {
                failed++;
            }

            StatusText = $"Connecting... {connected + failed}/{total} ({failed} failed)";
        }

        IsConnecting = false;
        StatusText = $"Batch connect: {connected} connected, {failed} failed out of {total}";
        StatusChanged?.Invoke(StatusText);
    }

    private async Task ConnectSingleDeviceAsync(OnvifDevice device, string username, string password)
    {
        device.Username = username;
        device.Password = password;
        device.Status = DeviceStatus.Authenticating;

        var errors = new List<string>();

        // Phase 1: Get device info (non-fatal if it fails)
        try
        {
            var deviceInfo = await _deviceService.GetDeviceInformationAsync(
                device.ServiceAddress, username, password);
            device.Manufacturer = deviceInfo.Manufacturer;
            device.Model = deviceInfo.Model;
            device.FirmwareVersion = deviceInfo.FirmwareVersion;
            device.SerialNumber = deviceInfo.SerialNumber;
            device.HardwareId = deviceInfo.HardwareId;
        }
        catch (Exception ex)
        {
            errors.Add($"Device info: {ex.Message}");
            CrashLogger.Log("ConnectSingleDevice - GetDeviceInformation", ex);
        }

        // Phase 2: Get capabilities (important but non-fatal)
        try
        {
            var capabilities = await _deviceService.GetCapabilitiesAsync(
                device.ServiceAddress, username, password);
            device.Capabilities = capabilities;
        }
        catch (Exception ex)
        {
            errors.Add($"Capabilities: {ex.Message}");
            CrashLogger.Log("ConnectSingleDevice - GetCapabilities", ex);
        }

        // Phase 3: Get media profiles (non-fatal)
        try
        {
            var mediaUrl = device.Capabilities.MediaServiceAddress;
            if (device.Capabilities.HasMedia && !string.IsNullOrEmpty(mediaUrl))
            {
                var profiles = await _mediaService.GetProfilesAsync(mediaUrl, username, password);
                device.Profiles.Clear();
                foreach (var profile in profiles)
                {
                    try { profile.StreamUri = await _mediaService.GetStreamUriAsync(mediaUrl, profile.Token, username, password); } catch { }
                    try { profile.SnapshotUri = await _mediaService.GetSnapshotUriAsync(mediaUrl, profile.Token, username, password); } catch { }
                    device.Profiles.Add(profile);
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Profiles: {ex.Message}");
            CrashLogger.Log("ConnectSingleDevice - GetProfiles", ex);
        }

        // Mark as connected regardless of partial failures
        device.IsAuthenticated = true;
        device.Status = DeviceStatus.Online;
        device.LastSeen = DateTime.Now;

        if (SaveCredentials)
            _credentialStore.SaveCredential(device.Address, username, password, device.DisplayName);

        if (errors.Count > 0)
        {
            StatusText = $"Connected to {device.DisplayName} (with {errors.Count} warning(s): {errors[0]})";
            CrashLogger.Log($"Partial connection to {device.Address}: {string.Join("; ", errors)}");
        }
        else
        {
            StatusText = $"Connected to {device.DisplayName}";
        }

        StatusChanged?.Invoke(StatusText);
    }

    private async Task AddManualDeviceAsync()
    {
        if (string.IsNullOrWhiteSpace(ManualAddress))
        {
            StatusText = "Enter an IP address or URL";
            return;
        }

        var address = ManualAddress.Trim();
        if (!address.StartsWith("http"))
            address = $"http://{address}/onvif/device_service";
        else if (!address.Contains("/onvif/"))
            address = address.TrimEnd('/') + "/onvif/device_service";

        OnvifDevice device;
        try
        {
            device = new OnvifDevice
            {
                Address = new Uri(address).Host,
                ServiceAddress = address,
                Name = new Uri(address).Host,
                Status = DeviceStatus.Unknown,
                LastSeen = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            StatusText = $"Invalid address: {ex.Message}";
            return;
        }

        var saved = _credentialStore.ResolveCredentials(device.Address);
        if (saved.HasValue)
        {
            device.Username = saved.Value.Username;
            device.Password = saved.Value.Password;
        }

        try
        {
            StatusText = $"Probing {address}...";
            await _deviceService.GetSystemDateTimeAsync(address);
            device.IsOnline = true;
            device.Status = DeviceStatus.Online;
            StatusText = $"Device found at {address}";
        }
        catch
        {
            device.Status = DeviceStatus.Unknown;
            StatusText = $"Added {address} (could not verify — try connecting with credentials)";
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

    private void DisconnectDevice()
    {
        if (SelectedDevice == null || !SelectedDevice.IsAuthenticated) return;
        SelectedDevice.IsAuthenticated = false;
        SelectedDevice.Status = DeviceStatus.Online;
        SelectedDevice.Profiles.Clear();
        StatusText = $"Disconnected from {SelectedDevice.DisplayName}";
        StatusChanged?.Invoke(StatusText);
    }
}
