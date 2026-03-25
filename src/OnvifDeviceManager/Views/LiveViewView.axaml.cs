using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LibVLCSharp.Avalonia;
using LibVLCSharp.Shared;
using OnvifDeviceManager.ViewModels;

namespace OnvifDeviceManager.Views;

public partial class LiveViewView : UserControl
{
    private bool _libVlcInitialized;
    private LibVLC? _libVlc;
    private MediaPlayer? _mediaPlayer;
    private VideoView? _videoView;
    private LiveViewViewModel? _boundVm;

    public LiveViewView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += (_, __) =>
        {
            UnsubscribeVm();
            SubscribeVm();
        };
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Do not touch native video runtime until the user starts RTSP.
        // This keeps view navigation stable if libvlc is missing/misaligned.
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        UnsubscribeVm();
        StopLiveStream();
        if (_mediaPlayer != null)
            _mediaPlayer.EncounteredError -= MediaPlayer_EncounteredError;
        if (_videoView != null)
            _videoView.MediaPlayer = null;
        _mediaPlayer?.Dispose();
        _mediaPlayer = null;
        _libVlc?.Dispose();
        _libVlc = null;
        _libVlcInitialized = false;
    }

    private void SubscribeVm()
    {
        UnsubscribeVm();
        if (DataContext is LiveViewViewModel vm)
        {
            _boundVm = vm;
            vm.RequestStopLiveStream += OnRequestStopLiveStream;
        }
    }

    private void UnsubscribeVm()
    {
        if (_boundVm != null)
        {
            _boundVm.RequestStopLiveStream -= OnRequestStopLiveStream;
            _boundVm = null;
        }
    }

    private void OnRequestStopLiveStream()
    {
        Dispatcher.UIThread.Post(StopLiveStream);
    }

    private static string? NativeLibVlcFileName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "libvlc.dll";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "libvlc.so";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "libvlc.dylib";
        return null;
    }

    private static string? FindLibVlcDirectory(string exeDir)
    {
        var libFile = NativeLibVlcFileName();
        if (libFile == null)
            return null;

        var rid = RuntimeInformation.RuntimeIdentifier;
        var ridFamily =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win-x64" :
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux-x64" :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx-x64" : string.Empty;

        foreach (var p in new[]
                 {
                     Path.Combine(exeDir, "libvlc", "win-x64"),
                     Path.Combine(exeDir, "libvlc", "linux-x64"),
                     Path.Combine(exeDir, "libvlc", "osx-x64"),
                     Path.Combine(exeDir, "libvlc", rid),
                     Path.Combine(exeDir, "runtimes", rid, "native"),
                     Path.Combine(exeDir, "runtimes", ridFamily, "native"),
                     Path.Combine(exeDir, "runtimes", "win-x64", "native"),
                     exeDir
                 })
        {
            if (Directory.Exists(p) && File.Exists(Path.Combine(p, libFile)))
                return p;
        }

        return null;
    }

    private void InitializeLibVlc()
    {
        if (_libVlcInitialized)
            return;

        try
        {
            var exeDir = AppContext.BaseDirectory;
            var libDir = FindLibVlcDirectory(exeDir);
            if (!string.IsNullOrEmpty(libDir))
                Core.Initialize(libDir);
            else
                Core.Initialize();

            _libVlc = new LibVLC("--network-caching=1000", "--rtsp-timeout=10");
            _mediaPlayer = new MediaPlayer(_libVlc);
            _mediaPlayer.EncounteredError += MediaPlayer_EncounteredError;
            EnsureVideoViewCreated();
            if (_videoView != null)
                _videoView.MediaPlayer = _mediaPlayer;
            _libVlcInitialized = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LibVLC init failed: {ex.Message}");
        }
    }

    private void EnsureVideoViewCreated()
    {
        if (_videoView != null)
            return;
        try
        {
            _videoView = new VideoView
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
            };
            VideoHost.Children.Clear();
            VideoHost.Children.Add(_videoView);
        }
        catch (Exception ex)
        {
            _videoView = null;
            System.Diagnostics.Debug.WriteLine($"VideoView create failed: {ex.Message}");
        }
    }

    private void StartLiveButton_Click(object? sender, RoutedEventArgs e)
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
            vm.StatusText = "No stream URI — pick a profile with a stream.";
            return;
        }

        if (!_libVlcInitialized || _libVlc == null || _mediaPlayer == null)
        {
            vm.StatusText = "Video engine not ready — ensure LibVLC native files are deployed (Windows: VideoLAN.LibVLC.Windows).";
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
        }
    }

    private void MediaPlayer_EncounteredError(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
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

    private void StopLiveButton_Click(object? sender, RoutedEventArgs e)
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
        catch { /* ignore */ }

        StartLiveButton.IsEnabled = true;
        StopLiveButton.IsEnabled = false;
    }
}
