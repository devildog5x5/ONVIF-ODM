using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LibVLCSharp.Shared;
using OnvifDeviceManager.ViewModels;
using Media = LibVLCSharp.Shared.Media;

namespace OnvifDeviceManager.Wpf.Views;

public partial class LiveViewView : UserControl
{
    private bool _libVlcInitialized;
    private LibVLC? _libVlc;
    private MediaPlayer? _mediaPlayer;
    private Media? _playbackMedia;
    private LiveViewViewModel? _boundVm;

    public LiveViewView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        SetupEventSimulatorBindings();

        if (DataContext is LiveViewViewModel vm)
        {
            _boundVm = vm;
            vm.RequestStopLiveStream += OnRequestStopLiveStream;
        }
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_boundVm != null)
            _boundVm.RequestStopLiveStream -= OnRequestStopLiveStream;
        _boundVm = null;
        StopLiveStream();
        if (_mediaPlayer != null)
            _mediaPlayer.EncounteredError -= MediaPlayer_EncounteredError;
        _playbackMedia?.Dispose();
        _playbackMedia = null;
        _mediaPlayer?.Dispose();
        _libVlc?.Dispose();
    }

    private void OnRequestStopLiveStream()
    {
        Dispatcher.Invoke(StopLiveStream);
    }

    private void InitializeLibVlc()
    {
        if (_libVlcInitialized) return;

        try
        {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var paths = new[]
            {
                Path.Combine(exeDir, "libvlc", "win-x64"),
                Path.Combine(exeDir, "runtimes", "win-x64", "native"),
                exeDir
            };

            string? libVlcPath = null;
            foreach (var p in paths)
            {
                if (Directory.Exists(p) && File.Exists(Path.Combine(p, "libvlc.dll")))
                {
                    libVlcPath = p;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(libVlcPath))
                Core.Initialize(libVlcPath);
            else
                Core.Initialize();

            // --avcodec-hw=none: HW decode often breaks WPF VideoView (airspace / D3D); software decode is more reliable for embedded preview.
            _libVlc = new LibVLC("--network-caching=1500", "--rtsp-timeout=60", "--avcodec-hw=none");
            _mediaPlayer = new MediaPlayer(_libVlc);
            _mediaPlayer.EncounteredError += MediaPlayer_EncounteredError;
            VideoView.MediaPlayer = _mediaPlayer;
            _libVlcInitialized = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LibVLC init failed: {ex.Message}");
        }
    }

    private void SetupEventSimulatorBindings()
    {
        EventTypeCombo.Items.Clear();
        foreach (var et in LiveViewViewModel.EventTypes)
            EventTypeCombo.Items.Add(et);
        EventTypeCombo.SelectedIndex = 0;

        PresetCombo.Items.Clear();
        for (int i = 1; i <= 9; i++)
            PresetCombo.Items.Add(i);
        PresetCombo.SelectedIndex = 0;
    }

    private void EventTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is LiveViewViewModel vm && EventTypeCombo.SelectedIndex >= 0)
            vm.EventTypeIndex = EventTypeCombo.SelectedIndex;
    }

    private void PresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is LiveViewViewModel vm && PresetCombo.SelectedItem is int n)
            vm.EventTargetPresetNumber = n;
    }

    private void EventTargetRadio_Changed(object sender, RoutedEventArgs e)
    {
        var usePreset = PresetRadio?.IsChecked == true;
        if (PresetPanel != null) PresetPanel.Visibility = usePreset ? Visibility.Visible : Visibility.Collapsed;
        if (CoordsPanel != null) CoordsPanel.Visibility = usePreset ? Visibility.Collapsed : Visibility.Visible;

        if (DataContext is LiveViewViewModel vm)
            vm.EventTargetUsePreset = usePreset;
    }

    private async void StartLiveButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LiveViewViewModel vm || vm.Device == null)
            return;

        if (!_libVlcInitialized)
            InitializeLibVlc();

        if (string.IsNullOrWhiteSpace(vm.StreamUri))
        {
            var streamProfile = vm.Profiles.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.StreamUri));
            if (streamProfile != null)
                vm.SelectedProfile = streamProfile;
        }

        await vm.RefreshSelectedProfileStreamUriAsync().ConfigureAwait(true);

        var uri = vm.StreamUri;
        if (string.IsNullOrWhiteSpace(uri))
        {
            MessageBox.Show("No stream URI available. Select a profile with a stream.", "No Stream", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!_libVlcInitialized || _libVlc == null || _mediaPlayer == null)
        {
            MessageBox.Show("Video player not initialized.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var authUri = LiveViewViewModel.BuildPlaybackRtspUri(uri, vm.Device, vm.Device.Username, vm.Device.Password);

        try
        {
            _mediaPlayer.Stop();
            var (started, transportUsed) = await TryStartStreamAsync(authUri).ConfigureAwait(true);
            if (started && !string.IsNullOrWhiteSpace(transportUsed))
            {
                vm.IsLiveStreaming = true;
                StartLiveButton.IsEnabled = false;
                StopLiveButton.IsEnabled = true;
                vm.StatusText = $"Live stream started ({transportUsed}) at {DateTime.Now:HH:mm:ss}";
                VideoView.UpdateLayout();
                _ = Dispatcher.BeginInvoke(new Action(() => VideoView.UpdateLayout()), System.Windows.Threading.DispatcherPriority.Render);
            }
            else
            {
                vm.IsLiveStreaming = false;
                vm.StatusText = "Could not start RTSP (all transports failed after connecting). Try another profile or Open externally.";
                StartLiveButton.IsEnabled = true;
                StopLiveButton.IsEnabled = false;
            }
        }
        catch (Exception ex)
        {
            vm.StatusText = $"Stream error: {ex.Message}";
            MessageBox.Show($"Failed to start stream: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void MediaPlayer_EncounteredError(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (DataContext is LiveViewViewModel vm)
            {
                vm.IsLiveStreaming = false;
                vm.StatusText = "Live stream error from media engine. Try another profile or Open externally.";
            }
            StartLiveButton.IsEnabled = true;
            StopLiveButton.IsEnabled = false;
        });
    }

    private async Task<(bool ok, string transportUsed)> TryStartStreamAsync(string uri)
    {
        if (_libVlc == null || _mediaPlayer == null)
            return (false, string.Empty);

        // Fallback chain:
        // 1) Forced RTSP/TCP
        // 2) LibVLC default transport
        // 3) Compatibility mode (UDP + larger caching / relaxed clock sync)
        // 4) Compatibility mode (RTSP-over-HTTP style transport for plugin-era cameras)
        foreach (var mode in new[] { "rtsp-tcp", "default", "compat-udp", "compat-http" })
        {
            _mediaPlayer.Stop();
            await Task.Delay(120).ConfigureAwait(true);
            _playbackMedia?.Dispose();
            _playbackMedia = new Media(_libVlc, uri, FromType.FromLocation);
            switch (mode)
            {
                case "rtsp-tcp":
                    _playbackMedia.AddOption(":rtsp-tcp");
                    _playbackMedia.AddOption(":network-caching=1000");
                    break;
                case "default":
                    _playbackMedia.AddOption(":network-caching=1000");
                    break;
                case "compat-udp":
                    _playbackMedia.AddOption(":rtsp-udp");
                    _playbackMedia.AddOption(":network-caching=2500");
                    _playbackMedia.AddOption(":live-caching=2500");
                    _playbackMedia.AddOption(":clock-jitter=0");
                    _playbackMedia.AddOption(":clock-synchro=0");
                    _playbackMedia.AddOption(":no-audio");
                    break;
                case "compat-http":
                    _playbackMedia.AddOption(":rtsp-http");
                    _playbackMedia.AddOption(":network-caching=3000");
                    _playbackMedia.AddOption(":live-caching=3000");
                    _playbackMedia.AddOption(":http-reconnect=true");
                    _playbackMedia.AddOption(":clock-jitter=0");
                    _playbackMedia.AddOption(":clock-synchro=0");
                    _playbackMedia.AddOption(":no-audio");
                    break;
            }

            if (!_mediaPlayer.Play(_playbackMedia))
                continue;

            if (await WaitForPlayingOrTerminalAsync().ConfigureAwait(true))
                return (true, mode);
        }

        _playbackMedia?.Dispose();
        _playbackMedia = null;
        return (false, string.Empty);
    }

    /// <summary>RTSP often sits in Buffering/Opening for many seconds; only accepting Playing caused false failures and tore down good streams.</summary>
    private async Task<bool> WaitForPlayingOrTerminalAsync()
    {
        if (_mediaPlayer == null) return false;
        for (var i = 0; i < 250; i++)
        {
            await Task.Delay(100).ConfigureAwait(true);
            switch (_mediaPlayer.State)
            {
                case VLCState.Playing:
                    return true;
                case VLCState.Error:
                case VLCState.Ended:
                    return false;
            }
        }

        var s = _mediaPlayer.State;
        return s is VLCState.Playing or VLCState.Buffering or VLCState.Opening;
    }

    private void StopLiveButton_Click(object sender, RoutedEventArgs e)
    {
        StopLiveStream();
    }

    private void StopLiveStream()
    {
        try
        {
            _mediaPlayer?.Stop();
            _playbackMedia?.Dispose();
            _playbackMedia = null;
            if (DataContext is LiveViewViewModel vm)
            {
                vm.IsLiveStreaming = false;
                vm.StatusText = "Live stream stopped";
            }
        }
        catch { }
        StartLiveButton.IsEnabled = true;
        StopLiveButton.IsEnabled = false;
    }

}
