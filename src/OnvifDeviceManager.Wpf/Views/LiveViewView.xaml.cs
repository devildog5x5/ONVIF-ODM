using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LibVLCSharp.Shared;
using OnvifDeviceManager.ViewModels;

namespace OnvifDeviceManager.Wpf.Views;

public partial class LiveViewView : UserControl
{
    private bool _libVlcInitialized;
    private LibVLC? _libVlc;
    private MediaPlayer? _mediaPlayer;
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

            _libVlc = new LibVLC("--network-caching=1000", "--rtsp-timeout=10");
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

    private void StartLiveButton_Click(object sender, RoutedEventArgs e)
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

        var authUri = LiveViewViewModel.BuildAuthenticatedRtspUri(uri, vm.Device.Username, vm.Device.Password);

        try
        {
            _mediaPlayer.Stop();
            var started = TryStartStream(authUri, out var transportUsed);
            if (started && !string.IsNullOrWhiteSpace(transportUsed))
            {
                vm.IsLiveStreaming = true;
                StartLiveButton.IsEnabled = false;
                StopLiveButton.IsEnabled = true;
                vm.StatusText = $"Live stream started ({transportUsed}) at {DateTime.Now:HH:mm:ss}";
            }
            else
            {
                vm.IsLiveStreaming = false;
                vm.StatusText = "Could not start RTSP stream (TCP/default fallback attempted). Try another profile or Open externally.";
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

    private bool TryStartStream(string uri, out string transportUsed)
    {
        transportUsed = string.Empty;
        if (_libVlc == null || _mediaPlayer == null)
            return false;

        // Fallback chain:
        // 1) Forced RTSP/TCP
        // 2) LibVLC default transport
        // 3) Compatibility mode (UDP + larger caching / relaxed clock sync)
        // 4) Compatibility mode (RTSP-over-HTTP style transport for plugin-era cameras)
        foreach (var mode in new[] { "rtsp-tcp", "default", "compat-udp", "compat-http" })
        {
            using var media = new Media(_libVlc, uri, FromType.FromLocation);
            switch (mode)
            {
                case "rtsp-tcp":
                    media.AddOption(":rtsp-tcp");
                    media.AddOption(":network-caching=1000");
                    break;
                case "default":
                    media.AddOption(":network-caching=1000");
                    break;
                case "compat-udp":
                    media.AddOption(":rtsp-udp");
                    media.AddOption(":network-caching=2500");
                    media.AddOption(":live-caching=2500");
                    media.AddOption(":clock-jitter=0");
                    media.AddOption(":clock-synchro=0");
                    media.AddOption(":no-audio");
                    break;
                case "compat-http":
                    media.AddOption(":rtsp-http");
                    media.AddOption(":network-caching=3000");
                    media.AddOption(":live-caching=3000");
                    media.AddOption(":http-reconnect=true");
                    media.AddOption(":clock-jitter=0");
                    media.AddOption(":clock-synchro=0");
                    media.AddOption(":no-audio");
                    break;
            }

            if (_mediaPlayer.Play(media))
            {
                transportUsed = mode;
                return true;
            }
        }

        return false;
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
