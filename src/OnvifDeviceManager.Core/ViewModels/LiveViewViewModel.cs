using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows.Input;
using OnvifDeviceManager.Models;
using OnvifDeviceManager.Services;

namespace OnvifDeviceManager.ViewModels;

public class LiveViewViewModel : ViewModelBase
{
    private readonly OnvifMediaService _mediaService;
    private readonly OnvifPtzService _ptzService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IClipboardService _clipboard;
    private OnvifDevice? _device;
    private MediaProfile? _selectedProfile;
    private byte[]? _snapshotBytes;
    private string _streamUri = string.Empty;
    private string _statusText = string.Empty;
    private bool _isLoading;
    private bool _isRefreshing;
    private int _refreshInterval = 1000;
    private CancellationTokenSource? _refreshCts;
    private float _panSpeed = 0.5f;
    private float _tiltSpeed = 0.5f;
    private float _zoomSpeed = 0.5f;
    private int _eventTargetPresetNumber = 1;
    private bool _eventTargetUsePreset = true;
    private float _eventTargetPan;
    private float _eventTargetTilt;
    private string _eventTargetPanString = "0.0";
    private string _eventTargetTiltString = "0.0";
    private int _eventTypeIndex;
    private string _lastSimulatedEvent = string.Empty;
    private bool _isLiveStreaming;

    public bool IsLiveStreaming
    {
        get => _isLiveStreaming;
        set
        {
            if (SetProperty(ref _isLiveStreaming, value))
                OnPropertyChanged(nameof(ShowVideoPlaceholder));
        }
    }

    public bool ShowVideoPlaceholder => !IsLiveStreaming && SnapshotBytes == null;

    public LiveViewViewModel(OnvifMediaService mediaService, OnvifPtzService ptzService, IUiDispatcher dispatcher, IClipboardService clipboard)
    {
        _mediaService = mediaService;
        _ptzService = ptzService;
        _dispatcher = dispatcher;
        _clipboard = clipboard;
        RefreshSnapshotCommand = new AsyncRelayCommand(RefreshSnapshotAsync);
        StartAutoRefreshCommand = new RelayCommand(StartAutoRefresh);
        StopAutoRefreshCommand = new RelayCommand(StopAutoRefresh);
        CopyStreamUriCommand = new AsyncRelayCommand(CopyStreamUriAsync);
        SimulateEventCommand = new AsyncRelayCommand(SimulateEventAsync);
        MoveUpCommand = new AsyncRelayCommand(() => ContinuousMoveAsync(0, TiltSpeed, 0));
        MoveDownCommand = new AsyncRelayCommand(() => ContinuousMoveAsync(0, -TiltSpeed, 0));
        MoveLeftCommand = new AsyncRelayCommand(() => ContinuousMoveAsync(-PanSpeed, 0, 0));
        MoveRightCommand = new AsyncRelayCommand(() => ContinuousMoveAsync(PanSpeed, 0, 0));
        ZoomInCommand = new AsyncRelayCommand(() => ContinuousMoveAsync(0, 0, ZoomSpeed));
        ZoomOutCommand = new AsyncRelayCommand(() => ContinuousMoveAsync(0, 0, -ZoomSpeed));
        StopCommand = new AsyncRelayCommand(StopMoveAsync);
        HomeCommand = new AsyncRelayCommand(GoHomeAsync);
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
        set
        {
            if (SetProperty(ref _selectedProfile, value) && value != null)
            {
                StreamUri = value.StreamUri;
                _ = RefreshSnapshotAsync();
            }
        }
    }

    public byte[]? SnapshotBytes
    {
        get => _snapshotBytes;
        set
        {
            if (SetProperty(ref _snapshotBytes, value))
                OnPropertyChanged(nameof(ShowVideoPlaceholder));
        }
    }

    public string StreamUri
    {
        get => _streamUri;
        set => SetProperty(ref _streamUri, value);
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

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    public int RefreshInterval
    {
        get => _refreshInterval;
        set => SetProperty(ref _refreshInterval, Math.Max(100, value));
    }

    public ICommand RefreshSnapshotCommand { get; }
    public ICommand StartAutoRefreshCommand { get; }
    public ICommand StopAutoRefreshCommand { get; }
    public ICommand CopyStreamUriCommand { get; }
    public ICommand SimulateEventCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }
    public ICommand MoveLeftCommand { get; }
    public ICommand MoveRightCommand { get; }
    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand HomeCommand { get; }

    public float PanSpeed { get => _panSpeed; set => SetProperty(ref _panSpeed, Math.Clamp(value, 0f, 1f)); }
    public float TiltSpeed { get => _tiltSpeed; set => SetProperty(ref _tiltSpeed, Math.Clamp(value, 0f, 1f)); }
    public float ZoomSpeed { get => _zoomSpeed; set => SetProperty(ref _zoomSpeed, Math.Clamp(value, 0f, 1f)); }

    public int EventTargetPresetNumber { get => _eventTargetPresetNumber; set => SetProperty(ref _eventTargetPresetNumber, Math.Clamp(value, 1, 9)); }
    public bool EventTargetUsePreset { get => _eventTargetUsePreset; set => SetProperty(ref _eventTargetUsePreset, value); }
    public float EventTargetPan { get => _eventTargetPan; set => SetProperty(ref _eventTargetPan, value); }
    public float EventTargetTilt { get => _eventTargetTilt; set => SetProperty(ref _eventTargetTilt, value); }
    public string EventTargetPanString { get => _eventTargetPanString; set => SetProperty(ref _eventTargetPanString, value ?? "0"); }
    public string EventTargetTiltString { get => _eventTargetTiltString; set => SetProperty(ref _eventTargetTiltString, value ?? "0"); }
    public int EventTypeIndex { get => _eventTypeIndex; set => SetProperty(ref _eventTypeIndex, value); }
    public string LastSimulatedEvent { get => _lastSimulatedEvent; set => SetProperty(ref _lastSimulatedEvent, value); }

    public static readonly string[] EventTypes = new[]
    {
        "Gunshot Detection", "Motion Detected", "Perimeter Breach", "General Alarm", "Vehicle Detected", "Custom Event"
    };

    public void SetDevice(OnvifDevice device)
    {
        Device = device;
        StopAutoRefresh();
        RequestStopLiveStream?.Invoke();

        Profiles.Clear();
        foreach (var profile in device.Profiles)
            Profiles.Add(profile);

        if (Profiles.Count > 0)
            SelectedProfile = Profiles[0];
    }

    /// <summary>Fired when the device changes; the view should stop any live RTSP stream.</summary>
    public event Action? RequestStopLiveStream;

    public void ClearDevice()
    {
        StopAutoRefresh();
        RequestStopLiveStream?.Invoke();
        Device = null;
        Profiles.Clear();
        SelectedProfile = null;
        SnapshotBytes = null;
        StreamUri = string.Empty;
        StatusText = string.Empty;
    }

    public async Task RefreshSnapshotAsync()
    {
        if (Device == null || SelectedProfile == null) return;

        var snapshotUri = SelectedProfile.SnapshotUri;
        if (string.IsNullOrEmpty(snapshotUri))
        {
            StatusText = "No snapshot URI available for this profile";
            return;
        }

        IsLoading = true;

        try
        {
            using var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

            if (!string.IsNullOrEmpty(Device.Username))
            {
                var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{Device.Username}:{Device.Password}"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            }

            SnapshotBytes = await client.GetByteArrayAsync(snapshotUri);
            StatusText = $"Snapshot captured at {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusText = $"Snapshot error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void StartAutoRefresh()
    {
        StopAutoRefresh();
        IsRefreshing = true;
        _refreshCts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            while (!_refreshCts.Token.IsCancellationRequested)
            {
                await _dispatcher.InvokeAsync(async () => await RefreshSnapshotAsync());
                try { await Task.Delay(RefreshInterval, _refreshCts.Token); }
                catch (OperationCanceledException) { break; }
            }
        });
    }

    private void StopAutoRefresh()
    {
        _refreshCts?.Cancel();
        _refreshCts = null;
        IsRefreshing = false;
    }

    private async Task CopyStreamUriAsync()
    {
        if (!string.IsNullOrEmpty(StreamUri))
        {
            await _clipboard.SetTextAsync(StreamUri);
            StatusText = "Stream URI copied to clipboard";
        }
    }

    private string GetPtzServiceUrl() =>
        Device?.Capabilities.PtzServiceAddress ?? Device?.ServiceAddress ?? string.Empty;

    private async Task ContinuousMoveAsync(float pan, float tilt, float zoom)
    {
        if (Device == null || SelectedProfile == null || !SelectedProfile.IsPtzEnabled) return;
        try
        {
            await _ptzService.ContinuousMoveAsync(
                GetPtzServiceUrl(), SelectedProfile.Token,
                pan, tilt, zoom, Device.Username, Device.Password);
            await Task.Delay(500);
            await _ptzService.StopAsync(
                GetPtzServiceUrl(), SelectedProfile.Token,
                true, true, Device.Username, Device.Password);
        }
        catch (Exception ex) { StatusText = $"Move error: {ex.Message}"; }
    }

    private async Task StopMoveAsync()
    {
        if (Device == null || SelectedProfile == null) return;
        try
        {
            await _ptzService.StopAsync(
                GetPtzServiceUrl(), SelectedProfile.Token,
                true, true, Device.Username, Device.Password);
        }
        catch { }
    }

    private async Task GoHomeAsync()
    {
        if (Device == null || SelectedProfile == null || !SelectedProfile.IsPtzEnabled) return;
        try
        {
            await _ptzService.GotoHomeAsync(
                GetPtzServiceUrl(), SelectedProfile.Token,
                Device.Username, Device.Password);
            StatusText = "Moving to home position";
        }
        catch (Exception ex) { StatusText = $"Home error: {ex.Message}"; }
    }

    public async Task SimulateEventAsync()
    {
        if (Device == null || SelectedProfile == null) return;

        var eventType = EventTypes[Math.Clamp(EventTypeIndex, 0, EventTypes.Length - 1)];
        LastSimulatedEvent = $"{eventType} @ {DateTime.Now:HH:mm:ss}";

        try
        {
            if (EventTargetUsePreset)
            {
                var presets = await _ptzService.GetPresetsAsync(
                    GetPtzServiceUrl(), SelectedProfile.Token,
                    Device.Username, Device.Password);
                var idx = Math.Clamp(EventTargetPresetNumber - 1, 0, Math.Max(0, presets.Count - 1));
                if (idx < presets.Count)
                {
                    await _ptzService.GotoPresetAsync(
                        GetPtzServiceUrl(), SelectedProfile.Token, presets[idx].Token,
                        Device.Username, Device.Password);
                    StatusText = $"Event '{eventType}' → Preset {EventTargetPresetNumber}";
                }
                else StatusText = $"Preset {EventTargetPresetNumber} not found";
            }
            else
            {
                float.TryParse(EventTargetPanString, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var panVal);
                float.TryParse(EventTargetTiltString, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var tiltVal);
                var pan = Math.Clamp(panVal, -1f, 1f);
                var tilt = Math.Clamp(tiltVal, -1f, 1f);
                await _ptzService.AbsoluteMoveAsync(
                    GetPtzServiceUrl(), SelectedProfile.Token, pan, tilt, 0.5f,
                    Device.Username, Device.Password);
                StatusText = $"Event '{eventType}' → Pan:{pan:F2} Tilt:{tilt:F2}";
            }
        }
        catch (Exception ex) { StatusText = $"Simulate error: {ex.Message}"; }
    }
}
