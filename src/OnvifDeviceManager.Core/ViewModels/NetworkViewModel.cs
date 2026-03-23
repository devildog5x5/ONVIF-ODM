using System.Windows.Input;
using OnvifDeviceManager.Models;
using OnvifDeviceManager.Services;

namespace OnvifDeviceManager.ViewModels;

public class NetworkViewModel : ViewModelBase
{
    private readonly OnvifDeviceService _deviceService;
    private OnvifDevice? _device;
    private NetworkConfiguration _networkConfig = new();
    private string _statusText = string.Empty;
    private bool _isLoading;

    public NetworkViewModel(OnvifDeviceService deviceService)
    {
        _deviceService = deviceService;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    public OnvifDevice? Device
    {
        get => _device;
        set => SetProperty(ref _device, value);
    }

    public NetworkConfiguration NetworkConfig
    {
        get => _networkConfig;
        set => SetProperty(ref _networkConfig, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ICommand RefreshCommand { get; }

    public void SetDevice(OnvifDevice device)
    {
        Device = device;
        NetworkConfig = device.NetworkConfig;
        _ = RefreshAsync();
    }

    public void ClearDevice()
    {
        Device = null;
        NetworkConfig = new NetworkConfiguration();
        StatusText = string.Empty;
    }

    private async Task RefreshAsync()
    {
        if (Device == null) return;

        IsLoading = true;
        try
        {
            var config = await _deviceService.GetNetworkInterfacesAsync(
                Device.ServiceAddress, Device.Username, Device.Password);
            NetworkConfig = config;
            Device.NetworkConfig = config;
            StatusText = "Network configuration loaded";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
