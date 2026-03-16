using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OnvifDeviceManager.Models;

public class OnvifDevice : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _address = string.Empty;
    private string _manufacturer = string.Empty;
    private string _model = string.Empty;
    private string _firmwareVersion = string.Empty;
    private string _serialNumber = string.Empty;
    private string _hardwareId = string.Empty;
    private string _macAddress = string.Empty;
    private string _hostname = string.Empty;
    private bool _isOnline;
    private bool _isAuthenticated;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private DateTime _lastSeen;
    private DeviceStatus _status = DeviceStatus.Unknown;

    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ServiceAddress { get; set; } = string.Empty;
    public string XAddrs { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = Array.Empty<string>();

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string Address
    {
        get => _address;
        set { _address = value; OnPropertyChanged(); }
    }

    public string Manufacturer
    {
        get => _manufacturer;
        set { _manufacturer = value; OnPropertyChanged(); }
    }

    public string Model
    {
        get => _model;
        set { _model = value; OnPropertyChanged(); }
    }

    public string FirmwareVersion
    {
        get => _firmwareVersion;
        set { _firmwareVersion = value; OnPropertyChanged(); }
    }

    public string SerialNumber
    {
        get => _serialNumber;
        set { _serialNumber = value; OnPropertyChanged(); }
    }

    public string HardwareId
    {
        get => _hardwareId;
        set { _hardwareId = value; OnPropertyChanged(); }
    }

    public string MacAddress
    {
        get => _macAddress;
        set { _macAddress = value; OnPropertyChanged(); }
    }

    public string Hostname
    {
        get => _hostname;
        set { _hostname = value; OnPropertyChanged(); }
    }

    public bool IsOnline
    {
        get => _isOnline;
        set { _isOnline = value; OnPropertyChanged(); }
    }

    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        set { _isAuthenticated = value; OnPropertyChanged(); }
    }

    public string Username
    {
        get => _username;
        set { _username = value; OnPropertyChanged(); }
    }

    public string Password
    {
        get => _password;
        set { _password = value; OnPropertyChanged(); }
    }

    public DateTime LastSeen
    {
        get => _lastSeen;
        set { _lastSeen = value; OnPropertyChanged(); }
    }

    public DeviceStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public ObservableCollection<MediaProfile> Profiles { get; set; } = new();
    public ObservableCollection<PtzPreset> PtzPresets { get; set; } = new();
    public DeviceCapabilities Capabilities { get; set; } = new();
    public NetworkConfiguration NetworkConfig { get; set; } = new();

    public string DisplayName => !string.IsNullOrEmpty(Name) ? Name : Address;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum DeviceStatus
{
    Unknown,
    Online,
    Offline,
    Authenticating,
    Error
}
