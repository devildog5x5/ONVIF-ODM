using System.IO;
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

    public LiveViewView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        InitializeLibVlc();
        SetupEventSimulatorBindings();

        if (DataContext is LiveViewViewModel vm)
            vm.RequestStopLiveStream += OnRequestStopLiveStream;
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is LiveViewViewModel vm)
            vm.RequestStopLiveStream -= OnRequestStopLiveStream;
        StopLiveStream();
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

            _libVlc = new LibVLC("--network-caching=1000", "--rtsp-tcp", "--rtsp-timeout=10");
            _mediaPlayer = new MediaPlayer(_libVlc);
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

        var authUri = BuildAuthenticatedRtspUri(uri, vm.Device.Username, vm.Device.Password);

        try
        {
            _mediaPlayer.Stop();
            var media = new Media(_libVlc, authUri, FromType.FromLocation);
            _mediaPlayer.Play(media);
            vm.IsLiveStreaming = true;
            StartLiveButton.IsEnabled = false;
            StopLiveButton.IsEnabled = true;
            vm.StatusText = $"Live stream started at {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            vm.StatusText = $"Stream error: {ex.Message}";
            MessageBox.Show($"Failed to start stream: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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

    private static string BuildAuthenticatedRtspUri(string uri, string? username, string? password)
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
}
