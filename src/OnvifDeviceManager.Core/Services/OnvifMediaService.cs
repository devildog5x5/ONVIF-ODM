using System.Xml.Linq;
using OnvifDeviceManager.Models;

namespace OnvifDeviceManager.Services;

public class OnvifMediaService : IDisposable
{
    private readonly SoapClient _soapClient = new();

    private static readonly XNamespace TrtNs = "http://www.onvif.org/ver10/media/wsdl";
    private static readonly XNamespace TtNs = "http://www.onvif.org/ver10/schema";

    public async Task<List<MediaProfile>> GetProfilesAsync(string serviceUrl, string? username = null, string? password = null)
    {
        var body = new XElement(TrtNs + "GetProfiles");
        var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);

        var profiles = new List<MediaProfile>();

        foreach (var profileElement in response.Descendants(TrtNs + "Profiles"))
        {
            var profile = new MediaProfile
            {
                Name = profileElement.Attribute("fixed")?.Value != null
                    ? profileElement.Element(TtNs + "Name")?.Value ?? profileElement.Attribute("token")?.Value ?? "Profile"
                    : profileElement.Element(TtNs + "Name")?.Value ?? "Profile",
                Token = profileElement.Attribute("token")?.Value ?? string.Empty
            };

            var videoEncoder = profileElement.Element(TtNs + "VideoEncoderConfiguration");
            if (videoEncoder != null)
            {
                profile.VideoEncoder = new VideoEncoderConfig
                {
                    Name = videoEncoder.Element(TtNs + "Name")?.Value ?? string.Empty,
                    Encoding = videoEncoder.Element(TtNs + "Encoding")?.Value ?? string.Empty,
                    Quality = float.TryParse(videoEncoder.Element(TtNs + "Quality")?.Value, out var q) ? (int)q : 0
                };

                var resolution = videoEncoder.Element(TtNs + "Resolution");
                if (resolution != null)
                {
                    profile.VideoEncoder.Width = int.TryParse(resolution.Element(TtNs + "Width")?.Value, out var w) ? w : 0;
                    profile.VideoEncoder.Height = int.TryParse(resolution.Element(TtNs + "Height")?.Value, out var h) ? h : 0;
                }

                var rateControl = videoEncoder.Element(TtNs + "RateControl");
                if (rateControl != null)
                {
                    profile.VideoEncoder.FrameRate = int.TryParse(rateControl.Element(TtNs + "FrameRateLimit")?.Value, out var fr) ? fr : 0;
                    profile.VideoEncoder.BitRate = int.TryParse(rateControl.Element(TtNs + "BitrateLimit")?.Value, out var br) ? br : 0;
                }

                var h264 = videoEncoder.Element(TtNs + "H264");
                if (h264 != null)
                {
                    profile.VideoEncoder.GovLength = int.TryParse(h264.Element(TtNs + "GovLength")?.Value, out var gl) ? gl : 0;
                    profile.VideoEncoder.Profile = h264.Element(TtNs + "H264Profile")?.Value ?? string.Empty;
                }
            }

            var videoSource = profileElement.Element(TtNs + "VideoSourceConfiguration");
            if (videoSource != null)
            {
                profile.VideoSource = new VideoSourceConfig
                {
                    Name = videoSource.Element(TtNs + "Name")?.Value ?? string.Empty,
                    Token = videoSource.Attribute("token")?.Value ?? string.Empty,
                    SourceToken = videoSource.Element(TtNs + "SourceToken")?.Value ?? string.Empty
                };

                var bounds = videoSource.Element(TtNs + "Bounds");
                if (bounds != null)
                {
                    profile.VideoSource.BoundsWidth = int.TryParse(bounds.Attribute("width")?.Value, out var bw) ? bw : 0;
                    profile.VideoSource.BoundsHeight = int.TryParse(bounds.Attribute("height")?.Value, out var bh) ? bh : 0;
                }
            }

            var audioEncoder = profileElement.Element(TtNs + "AudioEncoderConfiguration");
            if (audioEncoder != null)
            {
                profile.AudioEncoder = new AudioEncoderConfig
                {
                    Name = audioEncoder.Element(TtNs + "Name")?.Value ?? string.Empty,
                    Encoding = audioEncoder.Element(TtNs + "Encoding")?.Value ?? string.Empty,
                    BitRate = int.TryParse(audioEncoder.Element(TtNs + "Bitrate")?.Value, out var abr) ? abr : 0,
                    SampleRate = int.TryParse(audioEncoder.Element(TtNs + "SampleRate")?.Value, out var sr) ? sr : 0
                };
            }

            var ptzConfig = profileElement.Element(TtNs + "PTZConfiguration");
            profile.IsPtzEnabled = ptzConfig != null;

            profiles.Add(profile);
        }

        return profiles;
    }

    public async Task<string> GetStreamUriAsync(string serviceUrl, string profileToken, string? username = null, string? password = null)
    {
        var body = new XElement(TrtNs + "GetStreamUri",
            new XElement(TrtNs + "StreamSetup",
                new XElement(TtNs + "Stream", "RTP-Unicast"),
                new XElement(TtNs + "Transport",
                    new XElement(TtNs + "Protocol", "RTSP"))),
            new XElement(TrtNs + "ProfileToken", profileToken));

        var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);
        var uri = response.Descendants(TtNs + "Uri").FirstOrDefault()?.Value;

        return uri ?? string.Empty;
    }

    public async Task<string> GetSnapshotUriAsync(string serviceUrl, string profileToken, string? username = null, string? password = null)
    {
        var body = new XElement(TrtNs + "GetSnapshotUri",
            new XElement(TrtNs + "ProfileToken", profileToken));

        var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);
        var uri = response.Descendants(TtNs + "Uri").FirstOrDefault()?.Value;

        return uri ?? string.Empty;
    }

    public async Task<List<VideoEncoderConfig>> GetVideoEncoderConfigurationsAsync(string serviceUrl, string? username = null, string? password = null)
    {
        var body = new XElement(TrtNs + "GetVideoEncoderConfigurations");
        var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);

        var configs = new List<VideoEncoderConfig>();

        foreach (var configElement in response.Descendants(TrtNs + "Configurations"))
        {
            configs.Add(new VideoEncoderConfig
            {
                Name = configElement.Element(TtNs + "Name")?.Value ?? string.Empty,
                Encoding = configElement.Element(TtNs + "Encoding")?.Value ?? string.Empty,
                Width = int.TryParse(configElement.Descendants(TtNs + "Width").FirstOrDefault()?.Value, out var w) ? w : 0,
                Height = int.TryParse(configElement.Descendants(TtNs + "Height").FirstOrDefault()?.Value, out var h) ? h : 0,
                FrameRate = int.TryParse(configElement.Descendants(TtNs + "FrameRateLimit").FirstOrDefault()?.Value, out var fr) ? fr : 0,
                BitRate = int.TryParse(configElement.Descendants(TtNs + "BitrateLimit").FirstOrDefault()?.Value, out var br) ? br : 0,
                Quality = float.TryParse(configElement.Element(TtNs + "Quality")?.Value, out var q) ? (int)q : 0
            });
        }

        return configs;
    }

    public void Dispose()
    {
        _soapClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
