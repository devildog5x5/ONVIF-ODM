using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OnvifDeviceManager.Models;

public class DeviceGroup : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _username = string.Empty;
    private string _password = string.Empty;

    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
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

    public ObservableCollection<string> DeviceIds { get; set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class SavedCredential
{
    public string DeviceAddress { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? GroupId { get; set; }
    public DateTime LastUsed { get; set; }
}

public class CredentialStoreData
{
    public List<SavedCredential> Credentials { get; set; } = new();
    public List<DeviceGroupData> Groups { get; set; } = new();
}

public class DeviceGroupData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public List<string> DeviceAddresses { get; set; } = new();
}
