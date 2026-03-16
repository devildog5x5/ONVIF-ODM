using System.Text.Json;
using OnvifDeviceManager.Models;

namespace OnvifDeviceManager.Services;

public class CredentialStore
{
    private readonly string _filePath;
    private CredentialStoreData _data = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public CredentialStore()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OnvifDeviceManager");
        Directory.CreateDirectory(appData);
        _filePath = Path.Combine(appData, "credentials.json");
        Load();
    }

    public CredentialStore(string filePath)
    {
        _filePath = filePath;
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        Load();
    }

    public List<SavedCredential> GetAllCredentials() => _data.Credentials;
    public List<DeviceGroupData> GetAllGroups() => _data.Groups;

    public SavedCredential? GetCredential(string deviceAddress)
    {
        return _data.Credentials.FirstOrDefault(c =>
            c.DeviceAddress.Equals(deviceAddress, StringComparison.OrdinalIgnoreCase));
    }

    public (string Username, string Password)? ResolveCredentials(string deviceAddress)
    {
        var direct = GetCredential(deviceAddress);
        if (direct != null)
            return (direct.Username, direct.Password);

        var group = _data.Groups.FirstOrDefault(g =>
            g.DeviceAddresses.Any(a => a.Equals(deviceAddress, StringComparison.OrdinalIgnoreCase)));

        if (group != null)
            return (group.Username, group.Password);

        return null;
    }

    public void SaveCredential(string deviceAddress, string username, string password, string? deviceName = null)
    {
        var existing = _data.Credentials.FirstOrDefault(c =>
            c.DeviceAddress.Equals(deviceAddress, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            existing.Username = username;
            existing.Password = password;
            existing.LastUsed = DateTime.Now;
            if (deviceName != null) existing.DeviceName = deviceName;
        }
        else
        {
            _data.Credentials.Add(new SavedCredential
            {
                DeviceAddress = deviceAddress,
                DeviceName = deviceName,
                Username = username,
                Password = password,
                LastUsed = DateTime.Now
            });
        }

        Save();
    }

    public void RemoveCredential(string deviceAddress)
    {
        _data.Credentials.RemoveAll(c =>
            c.DeviceAddress.Equals(deviceAddress, StringComparison.OrdinalIgnoreCase));
        Save();
    }

    public DeviceGroupData CreateGroup(string name, string username, string password)
    {
        var group = new DeviceGroupData
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Username = username,
            Password = password
        };
        _data.Groups.Add(group);
        Save();
        return group;
    }

    public void UpdateGroup(string groupId, string? name = null, string? username = null, string? password = null)
    {
        var group = _data.Groups.FirstOrDefault(g => g.Id == groupId);
        if (group == null) return;

        if (name != null) group.Name = name;
        if (username != null) group.Username = username;
        if (password != null) group.Password = password;
        Save();
    }

    public void DeleteGroup(string groupId)
    {
        _data.Groups.RemoveAll(g => g.Id == groupId);
        foreach (var cred in _data.Credentials.Where(c => c.GroupId == groupId))
            cred.GroupId = null;
        Save();
    }

    public void AddDeviceToGroup(string groupId, string deviceAddress)
    {
        var group = _data.Groups.FirstOrDefault(g => g.Id == groupId);
        if (group == null) return;

        if (!group.DeviceAddresses.Contains(deviceAddress, StringComparer.OrdinalIgnoreCase))
        {
            group.DeviceAddresses.Add(deviceAddress);
            Save();
        }
    }

    public void RemoveDeviceFromGroup(string groupId, string deviceAddress)
    {
        var group = _data.Groups.FirstOrDefault(g => g.Id == groupId);
        if (group == null) return;

        group.DeviceAddresses.RemoveAll(a => a.Equals(deviceAddress, StringComparison.OrdinalIgnoreCase));
        Save();
    }

    public DeviceGroupData? GetGroupForDevice(string deviceAddress)
    {
        return _data.Groups.FirstOrDefault(g =>
            g.DeviceAddresses.Any(a => a.Equals(deviceAddress, StringComparison.OrdinalIgnoreCase)));
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _data = JsonSerializer.Deserialize<CredentialStoreData>(json, JsonOptions) ?? new();
            }
        }
        catch
        {
            _data = new();
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_data, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch { }
    }
}
