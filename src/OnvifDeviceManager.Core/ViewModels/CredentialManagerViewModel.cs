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
        DeleteDeviceCredentialCommand = new RelayCommand(DeleteDeviceCredential, () => SelectedCredential != null);
        CreateGroupCommand = new RelayCommand(CreateGroup);
        DeleteGroupCommand = new RelayCommand(DeleteGroup, () => SelectedGroup != null);
        UpdateGroupCommand = new RelayCommand(UpdateGroup, () => SelectedGroup != null);
        AddDeviceToGroupCommand = new RelayCommand(AddDeviceToGroup, () => SelectedGroup != null && !string.IsNullOrWhiteSpace(AddToGroupAddress));
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
                SelectedGroupDevices.Clear();
                if (value != null)
                {
                    NewGroupName = value.Name;
                    NewGroupUsername = value.Username;
                    NewGroupPassword = value.Password;
                    foreach (var addr in value.DeviceAddresses)
                        SelectedGroupDevices.Add(addr);
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
        Credentials.Clear();
        foreach (var c in _credentialStore.GetAllCredentials())
            Credentials.Add(c);

        Groups.Clear();
        foreach (var g in _credentialStore.GetAllGroups())
            Groups.Add(g);

        StatusText = $"{Credentials.Count} saved credential(s), {Groups.Count} group(s)";
    }

    private void SaveDeviceCredential()
    {
        if (string.IsNullOrWhiteSpace(NewDeviceAddress) || string.IsNullOrWhiteSpace(NewDeviceUsername))
        {
            StatusText = "Device address and username are required";
            return;
        }

        _credentialStore.SaveCredential(NewDeviceAddress, NewDeviceUsername, NewDevicePassword);
        StatusText = $"Credentials saved for {NewDeviceAddress}";
        NewDeviceAddress = string.Empty;
        NewDeviceUsername = string.Empty;
        NewDevicePassword = string.Empty;
        Refresh();
    }

    private void DeleteDeviceCredential()
    {
        if (SelectedCredential == null) return;
        _credentialStore.RemoveCredential(SelectedCredential.DeviceAddress);
        StatusText = $"Credentials removed for {SelectedCredential.DeviceAddress}";
        SelectedCredential = null;
        Refresh();
    }

    private void CreateGroup()
    {
        if (string.IsNullOrWhiteSpace(NewGroupName) || string.IsNullOrWhiteSpace(NewGroupUsername))
        {
            StatusText = "Group name and username are required";
            return;
        }

        _credentialStore.CreateGroup(NewGroupName, NewGroupUsername, NewGroupPassword);
        StatusText = $"Group '{NewGroupName}' created";
        NewGroupName = string.Empty;
        NewGroupUsername = string.Empty;
        NewGroupPassword = string.Empty;
        Refresh();
    }

    private void DeleteGroup()
    {
        if (SelectedGroup == null) return;
        var name = SelectedGroup.Name;
        _credentialStore.DeleteGroup(SelectedGroup.Id);
        StatusText = $"Group '{name}' deleted";
        SelectedGroup = null;
        Refresh();
    }

    private void UpdateGroup()
    {
        if (SelectedGroup == null) return;
        _credentialStore.UpdateGroup(SelectedGroup.Id, NewGroupName, NewGroupUsername, NewGroupPassword);
        StatusText = $"Group '{NewGroupName}' updated";
        Refresh();
    }

    private void AddDeviceToGroup()
    {
        if (SelectedGroup == null || string.IsNullOrWhiteSpace(AddToGroupAddress)) return;
        _credentialStore.AddDeviceToGroup(SelectedGroup.Id, AddToGroupAddress.Trim());
        StatusText = $"Added {AddToGroupAddress} to group '{SelectedGroup.Name}'";
        AddToGroupAddress = string.Empty;

        SelectedGroupDevices.Clear();
        var updated = _credentialStore.GetAllGroups().FirstOrDefault(g => g.Id == SelectedGroup.Id);
        if (updated != null)
            foreach (var a in updated.DeviceAddresses)
                SelectedGroupDevices.Add(a);
        Refresh();
    }

    private void RemoveDeviceFromGroup(object? parameter)
    {
        if (SelectedGroup == null || parameter is not string address) return;
        _credentialStore.RemoveDeviceFromGroup(SelectedGroup.Id, address);
        StatusText = $"Removed {address} from group '{SelectedGroup.Name}'";

        SelectedGroupDevices.Clear();
        var updated = _credentialStore.GetAllGroups().FirstOrDefault(g => g.Id == SelectedGroup.Id);
        if (updated != null)
            foreach (var a in updated.DeviceAddresses)
                SelectedGroupDevices.Add(a);
        Refresh();
    }
}
