using System.Collections.ObjectModel;
using System.Windows.Input;
using OnvifDeviceManager.Models;
using OnvifDeviceManager.Services;

namespace OnvifDeviceManager.ViewModels;

public class ProfilesViewModel : ViewModelBase
{
    private readonly OnvifMediaService _mediaService;
    private OnvifDevice? _device;
    private MediaProfile? _selectedProfile;
    private string _statusText = string.Empty;
    private bool _isLoading;

    public ProfilesViewModel(OnvifMediaService mediaService)
    {
        _mediaService = mediaService;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    public OnvifDevice? Device
    {
        get => _device;
        set => SetProperty(ref _device, value);
    }

    public ObservableCollection<MediaProfile> Profiles { get; } = new();

    public MediaProfile? SelectedProfile
    {
        get => _selectedProfile;
        set => SetProperty(ref _selectedProfile, value);
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
        Profiles.Clear();

        foreach (var profile in device.Profiles)
        {
            Profiles.Add(profile);
        }

        if (Profiles.Count > 0)
            SelectedProfile = Profiles[0];

        StatusText = $"{Profiles.Count} profile(s) loaded";
    }

    public void ClearDevice()
    {
        Device = null;
        Profiles.Clear();
        SelectedProfile = null;
        StatusText = string.Empty;
    }

    private async Task RefreshAsync()
    {
        if (Device == null || !Device.Capabilities.HasMedia || string.IsNullOrEmpty(Device.Capabilities.MediaServiceAddress))
        {
            StatusText = "Media service not available";
            return;
        }

        IsLoading = true;
        try
        {
            var profiles = await _mediaService.GetProfilesAsync(
                Device.Capabilities.MediaServiceAddress, Device.Username, Device.Password);

            Profiles.Clear();
            Device.Profiles.Clear();

            foreach (var profile in profiles)
            {
                try
                {
                    profile.StreamUri = await _mediaService.GetStreamUriAsync(
                        Device.Capabilities.MediaServiceAddress, profile.Token, Device.Username, Device.Password);
                }
                catch { }

                try
                {
                    profile.SnapshotUri = await _mediaService.GetSnapshotUriAsync(
                        Device.Capabilities.MediaServiceAddress, profile.Token, Device.Username, Device.Password);
                }
                catch { }

                Profiles.Add(profile);
                Device.Profiles.Add(profile);
            }

            StatusText = $"Refreshed {profiles.Count} profile(s)";
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
