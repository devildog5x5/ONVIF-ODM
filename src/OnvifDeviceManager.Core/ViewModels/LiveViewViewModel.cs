using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows.Input;
using OnvifDeviceManager.Models;
using OnvifDeviceManager.Services;

namespace OnvifDeviceManager.ViewModels;

public class LiveViewViewModel : ViewModelBase
{
    private readonly OnvifMediaService _mediaService;
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

    public LiveViewViewModel(OnvifMediaService mediaService, IUiDispatcher dispatcher, IClipboardService clipboard)
    {
        _mediaService = mediaService;
        _dispatcher = dispatcher;
        _clipboard = clipboard;
        RefreshSnapshotCommand = new AsyncRelayCommand(RefreshSnapshotAsync);
        StartAutoRefreshCommand = new RelayCommand(StartAutoRefresh);
        StopAutoRefreshCommand = new RelayCommand(StopAutoRefresh);
        CopyStreamUriCommand = new AsyncRelayCommand(CopyStreamUriAsync);
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
        set => SetProperty(ref _snapshotBytes, value);
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

    public void SetDevice(OnvifDevice device)
    {
        Device = device;
        StopAutoRefresh();

        Profiles.Clear();
        foreach (var profile in device.Profiles)
            Profiles.Add(profile);

        if (Profiles.Count > 0)
            SelectedProfile = Profiles[0];
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
}
