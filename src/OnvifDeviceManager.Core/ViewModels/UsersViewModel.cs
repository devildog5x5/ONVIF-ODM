using System.Collections.ObjectModel;
using System.Windows.Input;
using OnvifDeviceManager.Models;
using OnvifDeviceManager.Services;

namespace OnvifDeviceManager.ViewModels;

public class UsersViewModel : ViewModelBase
{
    private readonly OnvifDeviceService _deviceService;
    private OnvifDevice? _device;
    private string _statusText = string.Empty;
    private bool _isLoading;
    private DeviceUser? _selectedUser;
    private string _newUsername = string.Empty;
    private string _newPassword = string.Empty;
    private string _selectedUserLevel = "User";

    public UsersViewModel(OnvifDeviceService deviceService)
    {
        _deviceService = deviceService;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        CreateUserCommand = new AsyncRelayCommand(CreateUserAsync);
        DeleteUserCommand = new AsyncRelayCommand(DeleteUserAsync);
    }

    public OnvifDevice? Device
    {
        get => _device;
        set => SetProperty(ref _device, value);
    }

    public ObservableCollection<DeviceUser> Users { get; } = new();

    public DeviceUser? SelectedUser
    {
        get => _selectedUser;
        set => SetProperty(ref _selectedUser, value);
    }

    public string NewUsername
    {
        get => _newUsername;
        set => SetProperty(ref _newUsername, value);
    }

    public string NewPassword
    {
        get => _newPassword;
        set => SetProperty(ref _newPassword, value);
    }

    public string SelectedUserLevel
    {
        get => _selectedUserLevel;
        set => SetProperty(ref _selectedUserLevel, value);
    }

    public ObservableCollection<string> UserLevels { get; } = new()
    {
        "Administrator", "Operator", "User", "Anonymous"
    };

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
    public ICommand CreateUserCommand { get; }
    public ICommand DeleteUserCommand { get; }

    public void SetDevice(OnvifDevice device)
    {
        Device = device;
        _ = RefreshAsync();
    }

    public void ClearDevice()
    {
        Device = null;
        Users.Clear();
        SelectedUser = null;
        StatusText = string.Empty;
    }

    private async Task RefreshAsync()
    {
        if (Device == null) return;

        IsLoading = true;
        try
        {
            var users = await _deviceService.GetUsersAsync(
                Device.ServiceAddress, Device.Username, Device.Password);

            Users.Clear();
            foreach (var user in users)
            {
                Users.Add(user);
            }

            StatusText = $"Loaded {users.Count} user(s)";
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

    private async Task CreateUserAsync()
    {
        if (Device == null || string.IsNullOrWhiteSpace(NewUsername) || string.IsNullOrWhiteSpace(NewPassword))
        {
            StatusText = "Username and password are required";
            return;
        }

        IsLoading = true;
        try
        {
            await _deviceService.CreateUserAsync(
                Device.ServiceAddress, NewUsername, NewPassword, SelectedUserLevel,
                Device.Username, Device.Password);

            StatusText = $"User '{NewUsername}' created successfully";
            NewUsername = string.Empty;
            NewPassword = string.Empty;
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Error creating user: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DeleteUserAsync()
    {
        if (Device == null || SelectedUser == null)
        {
            StatusText = "Select a user to delete";
            return;
        }

        IsLoading = true;
        try
        {
            await _deviceService.DeleteUserAsync(
                Device.ServiceAddress, SelectedUser.Username,
                Device.Username, Device.Password);

            StatusText = $"User '{SelectedUser.Username}' deleted";
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Error deleting user: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
