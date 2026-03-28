using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
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
    private int _snapshotRefreshBusy;
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
    private float _ptzPan;
    private float _ptzTilt;
    private float _ptzZoom;
    private bool _ptzPositionKnown;
    private CancellationTokenSource? _ptzPollCts;

    public bool IsLiveStreaming
    {
        get => _isLiveStreaming;
        set
        {
            if (SetProperty(ref _isLiveStreaming, value))
            {
                OnPropertyChanged(nameof(ShowVideoPlaceholder));
                OnPropertyChanged(nameof(ShowSnapshotAsStill));
            }
        }
    }

    public bool ShowVideoPlaceholder => !IsLiveStreaming && SnapshotBytes == null;

    /// <summary>Still snapshot drawn on top of the video surface when not playing RTSP.</summary>
    public bool ShowSnapshotAsStill => !IsLiveStreaming && SnapshotBytes != null;

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
        OpenRtspExternallyCommand = new AsyncRelayCommand(OpenRtspExternallyAsync);
        SimulateEventCommand = new AsyncRelayCommand(SimulateEventAsync);
        MoveUpCommand = new AsyncRelayCommand(() => ContinuousMoveAsync(0, TiltSpeed, 0));
        MoveDownCommand = new AsyncRelayCommand(() => ContinuousMoveAsync(0, -TiltSpeed, 0));
        MoveLeftCommand = new AsyncRelayCommand(() => ContinuousMoveAsync(-PanSpeed, 0, 0));
        MoveRightCommand = new AsyncRelayCommand(() => ContinuousMoveAsync(PanSpeed, 0, 0));
        ZoomInCommand = new AsyncRelayCommand(() => ContinuousMoveAsync(0, 0, ZoomSpeed));
        ZoomOutCommand = new AsyncRelayCommand(() => ContinuousMoveAsync(0, 0, -ZoomSpeed));
        StopCommand = new AsyncRelayCommand(StopMoveAsync);
        HomeCommand = new AsyncRelayCommand(GoHomeAsync);
        RefreshPtzPositionCommand = new AsyncRelayCommand(() => RefreshPtzPositionAsync(false));
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
            if (SetProperty(ref _selectedProfile, value))
            {
                StopPtzPositionPolling();
                OnPropertyChanged(nameof(ShowPtzPanel));
                NotifyPtzDisplayProperties();
                if (value != null)
                {
                    StreamUri = StreamUriPlayback.ApplyDeviceHost(value.StreamUri, Device);
                    _ = RefreshSnapshotAsync();
                    if (value.IsPtzEnabled && Device != null)
                    {
                        _ = RefreshPtzPositionAsync(false);
                        StartPtzPositionPolling();
                    }
                    else
                        ClearPtzDisplay();
                }
                else
                    ClearPtzDisplay();
            }
        }
    }

    /// <summary>True when the current media profile supports PTZ (d-pad in Live View).</summary>
    public bool ShowPtzPanel => Device != null && SelectedProfile?.IsPtzEnabled == true;

    public byte[]? SnapshotBytes
    {
        get => _snapshotBytes;
        set
        {
            if (SetProperty(ref _snapshotBytes, value))
            {
                OnPropertyChanged(nameof(ShowVideoPlaceholder));
                OnPropertyChanged(nameof(ShowSnapshotAsStill));
            }
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
    public ICommand OpenRtspExternallyCommand { get; }
    public ICommand SimulateEventCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }
    public ICommand MoveLeftCommand { get; }
    public ICommand MoveRightCommand { get; }
    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand HomeCommand { get; }
    public ICommand RefreshPtzPositionCommand { get; }

    /// <summary>Normalized pan from last GetStatus (−1…1 typical).</summary>
    public float PtzPan => _ptzPan;

    public float PtzTilt => _ptzTilt;

    public float PtzZoom => _ptzZoom;

    public bool PtzPositionKnown => _ptzPositionKnown;

    public string PtzPositionText =>
        _ptzPositionKnown
            ? $"Pan {_ptzPan:F3} · Tilt {_ptzTilt:F3} · Zoom {_ptzZoom:F3}"
            : "Position: use PTZ or Refresh — values from camera";

    /// <summary>Horizontal offset (px) for reticle dot; center = 0.</summary>
    public double PtzIndicatorOffsetX => Math.Clamp(_ptzPan, -1f, 1f) * 38d;

    /// <summary>Vertical offset (px); positive tilt up moves dot up.</summary>
    public double PtzIndicatorOffsetY => -Math.Clamp(_ptzTilt, -1f, 1f) * 38d;

    public float PanSpeed { get => _panSpeed; set => SetProperty(ref _panSpeed, Math.Clamp(value, 0f, 1f)); }
    public float TiltSpeed { get => _tiltSpeed; set => SetProperty(ref _tiltSpeed, Math.Clamp(value, 0f, 1f)); }
    public float ZoomSpeed { get => _zoomSpeed; set => SetProperty(ref _zoomSpeed, Math.Clamp(value, 0f, 1f)); }

    public int EventTargetPresetNumber { get => _eventTargetPresetNumber; set => SetProperty(ref _eventTargetPresetNumber, Math.Clamp(value, 1, 9)); }
    public bool EventTargetUsePreset
    {
        get => _eventTargetUsePreset;
        set
        {
            if (!SetProperty(ref _eventTargetUsePreset, value)) return;
            OnPropertyChanged(nameof(EventTargetUseCoordinates));
        }
    }

    /// <summary>Inverse of <see cref="EventTargetUsePreset"/> for paired radio buttons in the Avalonia UI.</summary>
    public bool EventTargetUseCoordinates
    {
        get => !_eventTargetUsePreset;
        set
        {
            if (value)
                EventTargetUsePreset = false;
        }
    }
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

    public static readonly int[] PresetNumberChoices = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

    public void SetDevice(OnvifDevice device)
    {
        Device = device;
        OnPropertyChanged(nameof(ShowPtzPanel));
        StopAutoRefresh();
        StopPtzPositionPolling();
        RequestStopLiveStream?.Invoke();

        Profiles.Clear();
        foreach (var profile in device.Profiles)
            Profiles.Add(profile);

        if (Profiles.Count > 0)
        {
            // Prefer a profile that can actually stream, while still favoring PTZ when possible.
            SelectedProfile =
                Profiles.FirstOrDefault(p => p.IsPtzEnabled && !string.IsNullOrWhiteSpace(p.StreamUri)) ??
                Profiles.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.StreamUri)) ??
                Profiles.FirstOrDefault(p => p.IsPtzEnabled) ??
                Profiles[0];

            if (string.IsNullOrWhiteSpace(SelectedProfile?.StreamUri))
                StatusText = "Connected. Selected profile has no stream URI; choose another profile for live video.";
            else if (SelectedProfile?.IsPtzEnabled != true)
                StatusText = "Connected. Live video ready. PTZ controls will auto-switch to a PTZ profile if available.";
        }
        else
        {
            SelectedProfile = null;
            ClearPtzDisplay();
        }
    }

    /// <summary>Fired when the device changes; the view should stop any live RTSP stream.</summary>
    public event Action? RequestStopLiveStream;

    public void ClearDevice()
    {
        StopAutoRefresh();
        StopPtzPositionPolling();
        RequestStopLiveStream?.Invoke();
        Device = null;
        Profiles.Clear();
        SelectedProfile = null;
        SnapshotBytes = null;
        StreamUri = string.Empty;
        StatusText = string.Empty;
        OnPropertyChanged(nameof(ShowPtzPanel));
        ClearPtzDisplay();
    }

    private void ClearPtzDisplay()
    {
        _ptzPan = 0;
        _ptzTilt = 0;
        _ptzZoom = 0;
        _ptzPositionKnown = false;
        NotifyPtzDisplayProperties();
    }

    private void NotifyPtzDisplayProperties()
    {
        OnPropertyChanged(nameof(PtzPan));
        OnPropertyChanged(nameof(PtzTilt));
        OnPropertyChanged(nameof(PtzZoom));
        OnPropertyChanged(nameof(PtzPositionKnown));
        OnPropertyChanged(nameof(PtzPositionText));
        OnPropertyChanged(nameof(PtzIndicatorOffsetX));
        OnPropertyChanged(nameof(PtzIndicatorOffsetY));
    }

    private void StartPtzPositionPolling()
    {
        StopPtzPositionPolling();
        if (Device == null || SelectedProfile == null || !SelectedProfile.IsPtzEnabled)
            return;

        _ptzPollCts = new CancellationTokenSource();
        var token = _ptzPollCts.Token;
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await _dispatcher.InvokeAsync(async () => await RefreshPtzPositionAsync(true));
                    await Task.Delay(1600, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, token);
    }

    private void StopPtzPositionPolling()
    {
        _ptzPollCts?.Cancel();
        _ptzPollCts?.Dispose();
        _ptzPollCts = null;
    }

    private async Task RefreshPtzPositionAsync(bool quiet)
    {
        if (Device == null || SelectedProfile == null || !SelectedProfile.IsPtzEnabled)
        {
            ClearPtzDisplay();
            return;
        }

        try
        {
            var st = await _ptzService.GetStatusAsync(
                GetPtzServiceUrl(), SelectedProfile.Token,
                Device.Username, Device.Password);

            _ptzPan = st.Pan;
            _ptzTilt = st.Tilt;
            _ptzZoom = st.Zoom;
            _ptzPositionKnown = true;
            NotifyPtzDisplayProperties();
            if (!quiet)
                StatusText = $"PTZ position updated ({DateTime.Now:HH:mm:ss})";
        }
        catch (Exception ex)
        {
            if (!quiet)
                StatusText = $"PTZ position error: {ex.Message}";
        }
    }

    public async Task RefreshSnapshotAsync()
    {
        if (Device == null || SelectedProfile == null) return;

        var snapshotUriRaw = SelectedProfile.SnapshotUri;
        if (string.IsNullOrEmpty(snapshotUriRaw))
        {
            await _dispatcher.InvokeAsync(() =>
            {
                StatusText = "No snapshot URI available for this profile";
            });
            return;
        }

        // Same loopback fix as RTSP: cameras often return http://127.0.0.1/... which only works on the device.
        var snapshotUri = StreamUriPlayback.ApplyDeviceHost(snapshotUriRaw, Device);
        if (string.IsNullOrWhiteSpace(snapshotUri))
        {
            await _dispatcher.InvokeAsync(() => { StatusText = "Invalid snapshot URI"; });
            return;
        }

        // Avoid overlapping fetches (auto-refresh + manual) corrupting binding / Interlocked state.
        if (Interlocked.CompareExchange(ref _snapshotRefreshBusy, 1, 0) != 0)
            return;

        await _dispatcher.InvokeAsync(() => { IsLoading = true; });

        byte[]? data = null;
        var status = string.Empty;
        try
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
                UseDefaultCredentials = false
            };
            if (!string.IsNullOrEmpty(Device.Username))
                handler.Credentials = new NetworkCredential(Device.Username, Device.Password ?? string.Empty);

            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
            data = await client.GetByteArrayAsync(snapshotUri).ConfigureAwait(false);

            if (data.Length == 0)
                status = "Snapshot error: empty response";
            else if (!LooksLikeRasterImage(data))
                status = "Snapshot error: response was not a JPEG/PNG (often HTTP 401 HTML — check credentials)";
            else
                status = $"Snapshot captured at {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            status = $"Snapshot error: {ex.Message}";
            data = null;
        }
        finally
        {
            await _dispatcher.InvokeAsync(() =>
            {
                IsLoading = false;
                Interlocked.Exchange(ref _snapshotRefreshBusy, 0);
                if (data != null && LooksLikeRasterImage(data))
                    SnapshotBytes = data;
                else if (data != null)
                    SnapshotBytes = null;
                StatusText = status;
            });
        }
    }

    private static bool LooksLikeRasterImage(byte[] bytes)
    {
        if (bytes.Length < 4) return false;
        if (bytes[0] == 0xFF && bytes[1] == 0xD8) return true;
        return bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47;
    }

    private void StartAutoRefresh()
    {
        StopAutoRefresh();
        IsRefreshing = true;
        _refreshCts = new CancellationTokenSource();
        var token = _refreshCts.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await _dispatcher.InvokeAsync(RefreshSnapshotAsync).ConfigureAwait(false);
                    await Task.Delay(RefreshInterval, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, token);
    }

    private void StopAutoRefresh()
    {
        try
        {
            _refreshCts?.Cancel();
        }
        catch
        {
            /* ignore */
        }

        _refreshCts?.Dispose();
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

    /// <summary>Re-fetches stream URI from the camera (RTSP then RTP-over-TCP SOAP) and updates the selected profile.</summary>
    public async Task RefreshSelectedProfileStreamUriAsync()
    {
        if (Device == null || SelectedProfile == null) return;
        var mediaUrl = Device.Capabilities.MediaServiceAddress;
        if (string.IsNullOrWhiteSpace(mediaUrl)) return;

        try
        {
            var u = await _mediaService.GetStreamUriAsync(mediaUrl, SelectedProfile.Token, Device.Username, Device.Password);
            if (string.IsNullOrWhiteSpace(u))
                u = await _mediaService.GetStreamUriRtpOverTcpAsync(mediaUrl, SelectedProfile.Token, Device.Username, Device.Password);
            if (!string.IsNullOrWhiteSpace(u))
            {
                SelectedProfile.StreamUri = u;
                StreamUri = StreamUriPlayback.ApplyDeviceHost(u, Device);
                StatusText = "Stream address refreshed from camera.";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Could not refresh stream URI: {ex.Message}";
        }
    }

    /// <summary>Opens the RTSP URL with the OS default handler (often VLC).</summary>
    private Task OpenRtspExternallyAsync()
    {
        if (Device == null || string.IsNullOrWhiteSpace(StreamUri))
        {
            StatusText = "No stream URI to open";
            return Task.CompletedTask;
        }

        try
        {
            var uri = BuildPlaybackRtspUri(StreamUri, Device, Device.Username, Device.Password);
            Process.Start(new ProcessStartInfo { FileName = uri, UseShellExecute = true });
            StatusText = "Opened stream URL with the default application";
        }
        catch (Exception ex)
        {
            StatusText = $"Could not open stream: {ex.Message}";
        }

        return Task.CompletedTask;
    }

    public static string BuildAuthenticatedRtspUri(string uri, string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username))
            return uri;

        if (!Uri.TryCreate(uri, UriKind.Absolute, out var u) || string.IsNullOrEmpty(u.UserInfo))
        {
            var builder = new UriBuilder(uri)
            {
                UserName = username,
                Password = password ?? ""
            };
            return builder.Uri.ToString();
        }

        return uri;
    }

    /// <summary>Loopback host fix + credentials for LibVLC / external players.</summary>
    public static string BuildPlaybackRtspUri(string streamUri, OnvifDevice? device, string? username, string? password)
    {
        var hostFixed = StreamUriPlayback.ApplyDeviceHost(streamUri, device);
        return BuildAuthenticatedRtspUri(hostFixed, username, password);
    }

    private string GetPtzServiceUrl() =>
        Device?.Capabilities.PtzServiceAddress ?? Device?.ServiceAddress ?? string.Empty;

    private bool EnsurePtzReady()
    {
        if (Device == null || SelectedProfile == null)
            return false;

        if (SelectedProfile.IsPtzEnabled)
            return true;

        var ptzProfile = Profiles.FirstOrDefault(p => p.IsPtzEnabled);
        if (ptzProfile != null)
        {
            SelectedProfile = ptzProfile;
            StatusText = $"Switched to PTZ profile '{ptzProfile.Name}' for PTZ controls.";
            return true;
        }

        StatusText = "No PTZ-enabled profile is available on this camera.";
        return false;
    }

    private async Task ContinuousMoveAsync(float pan, float tilt, float zoom)
    {
        if (!EnsurePtzReady()) return;
        var device = Device;
        var profile = SelectedProfile;
        if (device == null || profile == null) return;
        try
        {
            await _ptzService.ContinuousMoveAsync(
                GetPtzServiceUrl(), profile.Token,
                pan, tilt, zoom, device.Username, device.Password);
            await Task.Delay(500);
            await _ptzService.StopAsync(
                GetPtzServiceUrl(), profile.Token,
                true, true, device.Username, device.Password);
            await RefreshPtzPositionAsync(true);
        }
        catch (Exception ex) { StatusText = $"Move error: {ex.Message}"; }
    }

    private async Task StopMoveAsync()
    {
        if (!EnsurePtzReady()) return;
        var device = Device;
        var profile = SelectedProfile;
        if (device == null || profile == null) return;
        try
        {
            await _ptzService.StopAsync(
                GetPtzServiceUrl(), profile.Token,
                true, true, device.Username, device.Password);
            await RefreshPtzPositionAsync(true);
        }
        catch { }
    }

    private async Task GoHomeAsync()
    {
        if (!EnsurePtzReady()) return;
        var device = Device;
        var profile = SelectedProfile;
        if (device == null || profile == null) return;
        try
        {
            await _ptzService.GotoHomeAsync(
                GetPtzServiceUrl(), profile.Token,
                device.Username, device.Password);
            StatusText = "Moving to home position";
            await Task.Delay(900);
            await RefreshPtzPositionAsync(true);
        }
        catch (Exception ex) { StatusText = $"Home error: {ex.Message}"; }
    }

    public async Task SimulateEventAsync()
    {
        if (!EnsurePtzReady()) return;
        var device = Device;
        var profile = SelectedProfile;
        if (device == null || profile == null) return;

        var eventType = EventTypes[Math.Clamp(EventTypeIndex, 0, EventTypes.Length - 1)];
        LastSimulatedEvent = $"{eventType} @ {DateTime.Now:HH:mm:ss}";

        try
        {
            if (EventTargetUsePreset)
            {
                var presets = await _ptzService.GetPresetsAsync(
                    GetPtzServiceUrl(), profile.Token,
                    device.Username, device.Password);
                var idx = Math.Clamp(EventTargetPresetNumber - 1, 0, Math.Max(0, presets.Count - 1));
                if (idx < presets.Count)
                {
                    await _ptzService.GotoPresetAsync(
                        GetPtzServiceUrl(), profile.Token, presets[idx].Token,
                        device.Username, device.Password);
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
                    GetPtzServiceUrl(), profile.Token, pan, tilt, 0.5f,
                    device.Username, device.Password);
                StatusText = $"Event '{eventType}' → Pan:{pan:F2} Tilt:{tilt:F2}";
            }
        }
        catch (Exception ex) { StatusText = $"Simulate error: {ex.Message}"; }

        await RefreshPtzPositionAsync(true);
    }
}
