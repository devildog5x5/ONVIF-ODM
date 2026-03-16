using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OnvifDeviceManager.Models;

public class MediaProfile : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _token = string.Empty;
    private string _streamUri = string.Empty;
    private string _snapshotUri = string.Empty;
    private bool _isPtzEnabled;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string Token
    {
        get => _token;
        set { _token = value; OnPropertyChanged(); }
    }

    public string StreamUri
    {
        get => _streamUri;
        set { _streamUri = value; OnPropertyChanged(); }
    }

    public string SnapshotUri
    {
        get => _snapshotUri;
        set { _snapshotUri = value; OnPropertyChanged(); }
    }

    public bool IsPtzEnabled
    {
        get => _isPtzEnabled;
        set { _isPtzEnabled = value; OnPropertyChanged(); }
    }

    public VideoEncoderConfig? VideoEncoder { get; set; }
    public VideoSourceConfig? VideoSource { get; set; }
    public AudioEncoderConfig? AudioEncoder { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class VideoEncoderConfig
{
    public string Name { get; set; } = string.Empty;
    public string Encoding { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public int FrameRate { get; set; }
    public int BitRate { get; set; }
    public int Quality { get; set; }
    public int GovLength { get; set; }
    public string Profile { get; set; } = string.Empty;

    public string Resolution => $"{Width}x{Height}";
    public string Summary => $"{Encoding} {Resolution} @ {FrameRate}fps";
}

public class VideoSourceConfig
{
    public string Name { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string SourceToken { get; set; } = string.Empty;
    public int BoundsX { get; set; }
    public int BoundsY { get; set; }
    public int BoundsWidth { get; set; }
    public int BoundsHeight { get; set; }
}

public class AudioEncoderConfig
{
    public string Name { get; set; } = string.Empty;
    public string Encoding { get; set; } = string.Empty;
    public int BitRate { get; set; }
    public int SampleRate { get; set; }
}
