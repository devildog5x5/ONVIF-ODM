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
using Media = LibVLCSharp.Shared.Media;

namespace OnvifDeviceManager.Views;

public partial class LiveViewView : UserControl
{
    private bool _libVlcInitialized;
    private LibVLC? _libVlc;
    private MediaPlayer? _mediaPlayer;
    private Media? _playbackMedia;
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
        _playbackMedia?.Dispose();
        _playbackMedia = null;
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

    private async Task InitializeLibVlcAsync()
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

            _libVlc = new LibVLC("--network-caching=1500", "--rtsp-timeout=60", "--avcodec-hw=none");
            _mediaPlayer = new MediaPlayer(_libVlc);
            _mediaPlayer.EncounteredError += MediaPlayer_EncounteredError;

            // Defer VideoView attach until after layout — reduces NativeControlHost "child window" failures on Windows.
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    EnsureVideoViewCreated();
                    if (_videoView != null)
                        _videoView.MediaPlayer = _mediaPlayer;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"VideoView attach failed: {ex.Message}");
                }
            }, DispatcherPriority.Loaded);

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

    private async void StartLiveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not LiveViewViewModel vm || vm.Device == null)
            return;

        if (!_libVlcInitialized)
            await InitializeLibVlcAsync().ConfigureAwait(true);

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
            vm.StatusText = "No stream URI — pick a profile with a stream.";
            return;
        }

        if (!_libVlcInitialized || _libVlc == null || _mediaPlayer == null)
        {
            vm.StatusText = "Video engine not ready — ensure LibVLC native files are deployed (Windows: VideoLAN.LibVLC.Windows).";
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
                _videoView?.InvalidateVisual();
                _videoView?.InvalidateArrange();
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

    private void StopLiveButton_Click(object? sender, RoutedEventArgs e)
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
        catch { /* ignore */ }

        StartLiveButton.IsEnabled = true;
        StopLiveButton.IsEnabled = false;
    }
}
