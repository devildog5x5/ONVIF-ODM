using System.Windows.Input;
using OnvifDeviceManager.Models;
using OnvifDeviceManager.Services;

namespace OnvifDeviceManager.ViewModels;

public class DeviceInfoViewModel : ViewModelBase
{
    private readonly OnvifDeviceService _deviceService;
    private OnvifDevice? _device;
    private string _hostname = string.Empty;
    private SystemDateTimeInfo? _dateTimeInfo;
    private bool _isLoading;
    private string _statusText = string.Empty;

    public DeviceInfoViewModel(OnvifDeviceService deviceService)
    {
        _deviceService = deviceService;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        RebootCommand = new AsyncRelayCommand(RebootAsync);
        SetHostnameCommand = new AsyncRelayCommand(SetHostnameAsync);
        FactoryResetCommand = new AsyncRelayCommand(FactoryResetAsync);
    }

    public OnvifDevice? Device
    {
        get => _device;
        set => SetProperty(ref _device, value);
    }

    public string Hostname
    {
        get => _hostname;
        set => SetProperty(ref _hostname, value);
    }

    public SystemDateTimeInfo? DateTimeInfo
    {
        get => _dateTimeInfo;
        set => SetProperty(ref _dateTimeInfo, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand RebootCommand { get; }
    public ICommand SetHostnameCommand { get; }
    public ICommand FactoryResetCommand { get; }

    public void SetDevice(OnvifDevice device)
    {
        Device = device;
        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (Device == null) return;

        IsLoading = true;
        StatusText = "Loading device information...";

        try
        {
            Hostname = await _deviceService.GetHostnameAsync(
                Device.ServiceAddress, Device.Username, Device.Password);

            DateTimeInfo = await _deviceService.GetSystemDateTimeAsync(
                Device.ServiceAddress, Device.Username, Device.Password);

            try
            {
                Device.NetworkConfig = await _deviceService.GetNetworkInterfacesAsync(
                    Device.ServiceAddress, Device.Username, Device.Password);
            }
            catch { }

            StatusText = "Device information loaded";
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

    private async Task RebootAsync()
    {
        if (Device == null) return;

        try
        {
            StatusText = "Rebooting device...";
            await _deviceService.SystemRebootAsync(
                Device.ServiceAddress, Device.Username, Device.Password);
            StatusText = "Reboot command sent. Device will restart shortly.";
        }
        catch (Exception ex)
        {
            StatusText = $"Reboot failed: {ex.Message}";
        }
    }

    private async Task SetHostnameAsync()
    {
        if (Device == null || string.IsNullOrWhiteSpace(Hostname)) return;

        try
        {
            await _deviceService.SetHostnameAsync(
                Device.ServiceAddress, Hostname, Device.Username, Device.Password);
            StatusText = $"Hostname set to {Hostname}";
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to set hostname: {ex.Message}";
        }
    }

    private async Task FactoryResetAsync()
    {
        if (Device == null) return;

        try
        {
            StatusText = "Performing factory reset...";
            await _deviceService.FactoryResetAsync(
                Device.ServiceAddress, "Hard", Device.Username, Device.Password);
            StatusText = "Factory reset initiated. Device will restart with default settings.";
        }
        catch (Exception ex)
        {
            StatusText = $"Factory reset failed: {ex.Message}";
        }
    }
}
