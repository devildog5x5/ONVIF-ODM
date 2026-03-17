using System.Collections.ObjectModel;
using System.Windows.Input;
using OnvifDeviceManager.Models;
using OnvifDeviceManager.Services;

namespace OnvifDeviceManager.ViewModels;

public class CredentialManagerViewModel : ViewModelBase
{
    private readonly CredentialStore _credentialStore;
    private SavedCredential? _selectedCredential;
    private DeviceGroupData? _selectedGroup;
    private string _statusText = string.Empty;

    private string _newDeviceAddress = string.Empty;
    private string _newDeviceUsername = string.Empty;
    private string _newDevicePassword = string.Empty;

    private string _newGroupName = string.Empty;
    private string _newGroupUsername = string.Empty;
    private string _newGroupPassword = string.Empty;

    private string _addToGroupAddress = string.Empty;

    public CredentialManagerViewModel(CredentialStore credentialStore)
    {
        _credentialStore = credentialStore;

        SaveDeviceCredentialCommand = new RelayCommand(SaveDeviceCredential);
        DeleteDeviceCredentialCommand = new RelayCommand(DeleteDeviceCredential);
        CreateGroupCommand = new RelayCommand(CreateGroup);
        DeleteGroupCommand = new RelayCommand(DeleteGroup);
        UpdateGroupCommand = new RelayCommand(UpdateGroup);
        AddDeviceToGroupCommand = new RelayCommand(AddDeviceToGroup);
        RemoveDeviceFromGroupCommand = new RelayCommand(RemoveDeviceFromGroup);
        RefreshCommand = new RelayCommand(Refresh);

        Refresh();
    }

    public ObservableCollection<SavedCredential> Credentials { get; } = new();
    public ObservableCollection<DeviceGroupData> Groups { get; } = new();
    public ObservableCollection<string> SelectedGroupDevices { get; } = new();

    public SavedCredential? SelectedCredential
    {
        get => _selectedCredential;
        set
        {
            if (SetProperty(ref _selectedCredential, value) && value != null)
            {
                NewDeviceAddress = value.DeviceAddress;
                NewDeviceUsername = value.Username;
                NewDevicePassword = value.Password;
            }
        }
    }

    public DeviceGroupData? SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            if (SetProperty(ref _selectedGroup, value))
            {
                RefreshGroupDevices();
                if (value != null)
                {
                    NewGroupName = value.Name;
                    NewGroupUsername = value.Username;
                    NewGroupPassword = value.Password;
                }
            }
        }
    }

    public string NewDeviceAddress
    {
        get => _newDeviceAddress;
        set => SetProperty(ref _newDeviceAddress, value);
    }

    public string NewDeviceUsername
    {
        get => _newDeviceUsername;
        set => SetProperty(ref _newDeviceUsername, value);
    }

    public string NewDevicePassword
    {
        get => _newDevicePassword;
        set => SetProperty(ref _newDevicePassword, value);
    }

    public string NewGroupName
    {
        get => _newGroupName;
        set => SetProperty(ref _newGroupName, value);
    }

    public string NewGroupUsername
    {
        get => _newGroupUsername;
        set => SetProperty(ref _newGroupUsername, value);
    }

    public string NewGroupPassword
    {
        get => _newGroupPassword;
        set => SetProperty(ref _newGroupPassword, value);
    }

    public string AddToGroupAddress
    {
        get => _addToGroupAddress;
        set => SetProperty(ref _addToGroupAddress, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public ICommand SaveDeviceCredentialCommand { get; }
    public ICommand DeleteDeviceCredentialCommand { get; }
    public ICommand CreateGroupCommand { get; }
    public ICommand DeleteGroupCommand { get; }
    public ICommand UpdateGroupCommand { get; }
    public ICommand AddDeviceToGroupCommand { get; }
    public ICommand RemoveDeviceFromGroupCommand { get; }
    public ICommand RefreshCommand { get; }

    private void Refresh()
    {
        var selectedGroupId = _selectedGroup?.Id;

        Credentials.Clear();
        foreach (var c in _credentialStore.GetAllCredentials())
            Credentials.Add(c);

        Groups.Clear();
        foreach (var g in _credentialStore.GetAllGroups())
            Groups.Add(g);

        if (selectedGroupId != null)
            SelectedGroup = Groups.FirstOrDefault(g => g.Id == selectedGroupId);

        StatusText = $"{Credentials.Count} saved credential(s), {Groups.Count} group(s)";
    }

    private void RefreshGroupDevices()
    {
        SelectedGroupDevices.Clear();
        if (_selectedGroup != null)
        {
            var fresh = _credentialStore.GetAllGroups().FirstOrDefault(g => g.Id == _selectedGroup.Id);
            if (fresh != null)
                foreach (var a in fresh.DeviceAddresses)
                    SelectedGroupDevices.Add(a);
        }
    }

    private void SaveDeviceCredential()
    {
        if (string.IsNullOrWhiteSpace(NewDeviceAddress) || string.IsNullOrWhiteSpace(NewDeviceUsername))
        {
            StatusText = "Enter a device address and username first";
            return;
        }

        _credentialStore.SaveCredential(NewDeviceAddress.Trim(), NewDeviceUsername.Trim(), NewDevicePassword);
        StatusText = $"Credentials saved for {NewDeviceAddress}";
        NewDeviceAddress = string.Empty;
        NewDeviceUsername = string.Empty;
        NewDevicePassword = string.Empty;
        Refresh();
    }

    private void DeleteDeviceCredential()
    {
        if (SelectedCredential == null)
        {
            StatusText = "Select a credential to delete first";
            return;
        }
        var addr = SelectedCredential.DeviceAddress;
        _credentialStore.RemoveCredential(addr);
        StatusText = $"Credentials removed for {addr}";
        SelectedCredential = null;
        Refresh();
    }

    private void CreateGroup()
    {
        if (string.IsNullOrWhiteSpace(NewGroupName) || string.IsNullOrWhiteSpace(NewGroupUsername))
        {
            StatusText = "Enter a group name and username first";
            return;
        }

        _credentialStore.CreateGroup(NewGroupName.Trim(), NewGroupUsername.Trim(), NewGroupPassword);
        StatusText = $"Group '{NewGroupName}' created — now add devices using the field below";
        NewGroupName = string.Empty;
        NewGroupUsername = string.Empty;
        NewGroupPassword = string.Empty;
        Refresh();

        if (Groups.Count > 0)
            SelectedGroup = Groups.Last();
    }

    private void DeleteGroup()
    {
        if (SelectedGroup == null)
        {
            StatusText = "Select a group to delete first";
            return;
        }
        var name = SelectedGroup.Name;
        _credentialStore.DeleteGroup(SelectedGroup.Id);
        StatusText = $"Group '{name}' deleted";
        SelectedGroup = null;
        Refresh();
    }

    private void UpdateGroup()
    {
        if (SelectedGroup == null)
        {
            StatusText = "Select a group to update first";
            return;
        }
        if (string.IsNullOrWhiteSpace(NewGroupName) || string.IsNullOrWhiteSpace(NewGroupUsername))
        {
            StatusText = "Group name and username cannot be empty";
            return;
        }
        _credentialStore.UpdateGroup(SelectedGroup.Id, NewGroupName.Trim(), NewGroupUsername.Trim(), NewGroupPassword);
        StatusText = $"Group '{NewGroupName}' updated";
        Refresh();
    }

    private void AddDeviceToGroup()
    {
        if (SelectedGroup == null)
        {
            StatusText = "Select a group first, then enter a device address";
            return;
        }
        if (string.IsNullOrWhiteSpace(AddToGroupAddress))
        {
            StatusText = "Enter a device IP address or hostname (e.g. 192.168.1.100)";
            return;
        }

        var address = AddToGroupAddress.Trim();
        _credentialStore.AddDeviceToGroup(SelectedGroup.Id, address);
        StatusText = $"Added {address} to group '{SelectedGroup.Name}'";
        AddToGroupAddress = string.Empty;
        RefreshGroupDevices();
    }

    private void RemoveDeviceFromGroup(object? parameter)
    {
        if (SelectedGroup == null || parameter is not string address) return;
        _credentialStore.RemoveDeviceFromGroup(SelectedGroup.Id, address);
        StatusText = $"Removed {address} from group '{SelectedGroup.Name}'";
        RefreshGroupDevices();
    }
}
