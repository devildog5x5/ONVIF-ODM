using System.Collections.ObjectModel;
using System.Windows.Input;
using OnvifDeviceManager.Models;
using OnvifDeviceManager.Services;

namespace OnvifDeviceManager.ViewModels;

public class PtzViewModel : ViewModelBase
{
    private readonly OnvifPtzService _ptzService;
    private OnvifDevice? _device;
    private MediaProfile? _selectedProfile;
    private float _panSpeed = 0.5f;
    private float _tiltSpeed = 0.5f;
    private float _zoomSpeed = 0.5f;
    private PtzStatus? _currentStatus;
    private string _statusText = string.Empty;
    private string _newPresetName = string.Empty;
    private PtzPreset? _selectedPreset;
    private bool _isMoving;

    public PtzViewModel(OnvifPtzService ptzService)
    {
        _ptzService = ptzService;

        MoveUpCommand = new AsyncRelayCommand(() => ContinuousMoveAsync(0, TiltSpeed, 0));
        MoveDownCommand = new AsyncRelayCommand(() => ContinuousMoveAsync(0, -TiltSpeed, 0));
        MoveLeftCommand = new AsyncRelayCommand(() => ContinuousMoveAsync(-PanSpeed, 0, 0));
        MoveRightCommand = new AsyncRelayCommand(() => ContinuousMoveAsync(PanSpeed, 0, 0));
        MoveUpLeftCommand = new AsyncRelayCommand(() => ContinuousMoveAsync(-PanSpeed, TiltSpeed, 0));
        MoveUpRightCommand = new AsyncRelayCommand(() => ContinuousMoveAsync(PanSpeed, TiltSpeed, 0));
        MoveDownLeftCommand = new AsyncRelayCommand(() => ContinuousMoveAsync(-PanSpeed, -TiltSpeed, 0));
        MoveDownRightCommand = new AsyncRelayCommand(() => ContinuousMoveAsync(PanSpeed, -TiltSpeed, 0));
        ZoomInCommand = new AsyncRelayCommand(() => ContinuousMoveAsync(0, 0, ZoomSpeed));
        ZoomOutCommand = new AsyncRelayCommand(() => ContinuousMoveAsync(0, 0, -ZoomSpeed));
        StopCommand = new AsyncRelayCommand(StopAsync);
        HomeCommand = new AsyncRelayCommand(GoHomeAsync);
        SetHomeCommand = new AsyncRelayCommand(SetHomeAsync);
        GetStatusCommand = new AsyncRelayCommand(GetStatusAsync);
        GotoPresetCommand = new AsyncRelayCommand(GotoPresetAsync);
        SavePresetCommand = new AsyncRelayCommand(SavePresetAsync);
        DeletePresetCommand = new AsyncRelayCommand(DeletePresetAsync);
        RefreshPresetsCommand = new AsyncRelayCommand(RefreshPresetsAsync);
    }

    public OnvifDevice? Device
    {
        get => _device;
        set => SetProperty(ref _device, value);
    }

    public ObservableCollection<MediaProfile> PtzProfiles { get; } = new();

    public MediaProfile? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (SetProperty(ref _selectedProfile, value) && value != null)
            {
                _ = RefreshPresetsAsync();
            }
        }
    }

    public float PanSpeed
    {
        get => _panSpeed;
        set => SetProperty(ref _panSpeed, Math.Clamp(value, 0.0f, 1.0f));
    }

    public float TiltSpeed
    {
        get => _tiltSpeed;
        set => SetProperty(ref _tiltSpeed, Math.Clamp(value, 0.0f, 1.0f));
    }

    public float ZoomSpeed
    {
        get => _zoomSpeed;
        set => SetProperty(ref _zoomSpeed, Math.Clamp(value, 0.0f, 1.0f));
    }

    public PtzStatus? CurrentStatus
    {
        get => _currentStatus;
        set => SetProperty(ref _currentStatus, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string NewPresetName
    {
        get => _newPresetName;
        set => SetProperty(ref _newPresetName, value);
    }

    public PtzPreset? SelectedPreset
    {
        get => _selectedPreset;
        set => SetProperty(ref _selectedPreset, value);
    }

    public bool IsMoving
    {
        get => _isMoving;
        set => SetProperty(ref _isMoving, value);
    }

    public ObservableCollection<PtzPreset> Presets { get; } = new();

    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }
    public ICommand MoveLeftCommand { get; }
    public ICommand MoveRightCommand { get; }
    public ICommand MoveUpLeftCommand { get; }
    public ICommand MoveUpRightCommand { get; }
    public ICommand MoveDownLeftCommand { get; }
    public ICommand MoveDownRightCommand { get; }
    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand HomeCommand { get; }
    public ICommand SetHomeCommand { get; }
    public ICommand GetStatusCommand { get; }
    public ICommand GotoPresetCommand { get; }
    public ICommand SavePresetCommand { get; }
    public ICommand DeletePresetCommand { get; }
    public ICommand RefreshPresetsCommand { get; }

    public void SetDevice(OnvifDevice device)
    {
        Device = device;
        PtzProfiles.Clear();

        foreach (var profile in device.Profiles.Where(p => p.IsPtzEnabled))
        {
            PtzProfiles.Add(profile);
        }

        if (PtzProfiles.Count > 0)
        {
            SelectedProfile = PtzProfiles[0];
        }
        else if (device.Profiles.Count > 0)
        {
            foreach (var p in device.Profiles) PtzProfiles.Add(p);
            SelectedProfile = PtzProfiles[0];
        }
    }

    private string GetPtzServiceUrl()
    {
        return Device?.Capabilities.PtzServiceAddress ?? Device?.ServiceAddress ?? string.Empty;
    }

    private async Task ContinuousMoveAsync(float pan, float tilt, float zoom)
    {
        if (Device == null || SelectedProfile == null) return;

        try
        {
            IsMoving = true;
            await _ptzService.ContinuousMoveAsync(
                GetPtzServiceUrl(), SelectedProfile.Token,
                pan, tilt, zoom,
                Device.Username, Device.Password);

            StatusText = $"Moving: Pan={pan:F2} Tilt={tilt:F2} Zoom={zoom:F2}";

            await Task.Delay(500);
            await _ptzService.StopAsync(
                GetPtzServiceUrl(), SelectedProfile.Token,
                true, true, Device.Username, Device.Password);

            await GetStatusAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Move error: {ex.Message}";
        }
        finally
        {
            IsMoving = false;
        }
    }

    private async Task StopAsync()
    {
        if (Device == null || SelectedProfile == null) return;

        try
        {
            await _ptzService.StopAsync(
                GetPtzServiceUrl(), SelectedProfile.Token,
                true, true, Device.Username, Device.Password);
            IsMoving = false;
            StatusText = "Stopped";
        }
        catch (Exception ex)
        {
            StatusText = $"Stop error: {ex.Message}";
        }
    }

    private async Task GoHomeAsync()
    {
        if (Device == null || SelectedProfile == null) return;

        try
        {
            await _ptzService.GotoHomeAsync(
                GetPtzServiceUrl(), SelectedProfile.Token,
                Device.Username, Device.Password);
            StatusText = "Moving to home position";
            await GetStatusAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Home error: {ex.Message}";
        }
    }

    private async Task SetHomeAsync()
    {
        if (Device == null || SelectedProfile == null) return;

        try
        {
            await _ptzService.SetHomeAsync(
                GetPtzServiceUrl(), SelectedProfile.Token,
                Device.Username, Device.Password);
            StatusText = "Home position set";
        }
        catch (Exception ex)
        {
            StatusText = $"Set home error: {ex.Message}";
        }
    }

    private async Task GetStatusAsync()
    {
        if (Device == null || SelectedProfile == null) return;

        try
        {
            CurrentStatus = await _ptzService.GetStatusAsync(
                GetPtzServiceUrl(), SelectedProfile.Token,
                Device.Username, Device.Password);
            StatusText = $"Position: Pan={CurrentStatus.Pan:F4} Tilt={CurrentStatus.Tilt:F4} Zoom={CurrentStatus.Zoom:F4}";
        }
        catch (Exception ex)
        {
            StatusText = $"Status error: {ex.Message}";
        }
    }

    private async Task RefreshPresetsAsync()
    {
        if (Device == null || SelectedProfile == null) return;

        try
        {
            var presets = await _ptzService.GetPresetsAsync(
                GetPtzServiceUrl(), SelectedProfile.Token,
                Device.Username, Device.Password);

            Presets.Clear();
            foreach (var preset in presets)
            {
                Presets.Add(preset);
            }

            StatusText = $"Loaded {presets.Count} preset(s)";
        }
        catch (Exception ex)
        {
            StatusText = $"Presets error: {ex.Message}";
        }
    }

    private async Task GotoPresetAsync()
    {
        if (Device == null || SelectedProfile == null || SelectedPreset == null) return;

        try
        {
            await _ptzService.GotoPresetAsync(
                GetPtzServiceUrl(), SelectedProfile.Token, SelectedPreset.Token,
                Device.Username, Device.Password);
            StatusText = $"Moving to preset: {SelectedPreset.Name}";
        }
        catch (Exception ex)
        {
            StatusText = $"Goto preset error: {ex.Message}";
        }
    }

    private async Task SavePresetAsync()
    {
        if (Device == null || SelectedProfile == null || string.IsNullOrWhiteSpace(NewPresetName)) return;

        try
        {
            var token = await _ptzService.SetPresetAsync(
                GetPtzServiceUrl(), SelectedProfile.Token, NewPresetName,
                null, Device.Username, Device.Password);
            StatusText = $"Preset '{NewPresetName}' saved (token: {token})";
            NewPresetName = string.Empty;
            await RefreshPresetsAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Save preset error: {ex.Message}";
        }
    }

    private async Task DeletePresetAsync()
    {
        if (Device == null || SelectedProfile == null || SelectedPreset == null) return;

        try
        {
            await _ptzService.RemovePresetAsync(
                GetPtzServiceUrl(), SelectedProfile.Token, SelectedPreset.Token,
                Device.Username, Device.Password);
            StatusText = $"Preset '{SelectedPreset.Name}' deleted";
            await RefreshPresetsAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Delete preset error: {ex.Message}";
        }
    }
}
