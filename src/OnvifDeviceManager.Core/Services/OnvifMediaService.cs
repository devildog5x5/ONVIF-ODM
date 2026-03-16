using System.Xml.Linq;
using OnvifDeviceManager.Models;

namespace OnvifDeviceManager.Services;

public class OnvifMediaService : IDisposable
{
    private readonly SoapClient _soapClient = new();

    private static readonly XNamespace TrtNs = "http://www.onvif.org/ver10/media/wsdl";
    private static readonly XNamespace TtNs = "http://www.onvif.org/ver10/schema";

    private static XElement? Find(XElement parent, string localName)
        => parent.Descendants().FirstOrDefault(e => e.Name.LocalName == localName);

    private static string Val(XElement? parent, string localName, string fallback = "")
        => parent?.Descendants().FirstOrDefault(e => e.Name.LocalName == localName)?.Value ?? fallback;

    public async Task<List<MediaProfile>> GetProfilesAsync(string serviceUrl, string? username = null, string? password = null)
    {
        var profiles = new List<MediaProfile>();

        try
        {
            var body = new XElement(TrtNs + "GetProfiles");
            var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);

            foreach (var el in response.Descendants().Where(e => e.Name.LocalName == "Profiles"))
            {
                try
                {
                    var profile = new MediaProfile
                    {
                        Name = Val(el, "Name", el.Attribute("token")?.Value ?? "Profile"),
                        Token = el.Attribute("token")?.Value ?? string.Empty
                    };

                    var videoEncoder = Find(el, "VideoEncoderConfiguration");
                    if (videoEncoder != null)
                    {
                        profile.VideoEncoder = new VideoEncoderConfig
                        {
                            Name = Val(videoEncoder, "Name"),
                            Encoding = Val(videoEncoder, "Encoding"),
                            Quality = float.TryParse(Val(videoEncoder, "Quality", "0"), out var q) ? (int)q : 0,
                            Width = int.TryParse(Val(videoEncoder, "Width", "0"), out var w) ? w : 0,
                            Height = int.TryParse(Val(videoEncoder, "Height", "0"), out var h) ? h : 0,
                            FrameRate = int.TryParse(Val(videoEncoder, "FrameRateLimit", "0"), out var fr) ? fr : 0,
                            BitRate = int.TryParse(Val(videoEncoder, "BitrateLimit", "0"), out var br) ? br : 0
                        };

                        var h264 = Find(videoEncoder, "H264");
                        if (h264 != null)
                        {
                            profile.VideoEncoder.GovLength = int.TryParse(Val(h264, "GovLength", "0"), out var gl) ? gl : 0;
                            profile.VideoEncoder.Profile = Val(h264, "H264Profile");
                        }
                    }

                    var videoSource = Find(el, "VideoSourceConfiguration");
                    if (videoSource != null)
                    {
                        profile.VideoSource = new VideoSourceConfig
                        {
                            Name = Val(videoSource, "Name"),
                            Token = videoSource.Attribute("token")?.Value ?? string.Empty,
                            SourceToken = Val(videoSource, "SourceToken")
                        };

                        var bounds = Find(videoSource, "Bounds");
                        if (bounds != null)
                        {
                            profile.VideoSource.BoundsWidth = int.TryParse(bounds.Attribute("width")?.Value, out var bw) ? bw : 0;
                            profile.VideoSource.BoundsHeight = int.TryParse(bounds.Attribute("height")?.Value, out var bh) ? bh : 0;
                        }
                    }

                    var audioEncoder = Find(el, "AudioEncoderConfiguration");
                    if (audioEncoder != null)
                    {
                        profile.AudioEncoder = new AudioEncoderConfig
                        {
                            Name = Val(audioEncoder, "Name"),
                            Encoding = Val(audioEncoder, "Encoding"),
                            BitRate = int.TryParse(Val(audioEncoder, "Bitrate", "0"), out var abr) ? abr : 0,
                            SampleRate = int.TryParse(Val(audioEncoder, "SampleRate", "0"), out var sr) ? sr : 0
                        };
                    }

                    profile.IsPtzEnabled = Find(el, "PTZConfiguration") != null;
                    profiles.Add(profile);
                }
                catch (Exception ex)
                {
                    CrashLogger.Log("GetProfilesAsync - parsing individual profile", ex);
                }
            }
        }
        catch (SoapFaultException) { throw; }
        catch (Exception ex)
        {
            CrashLogger.Log("GetProfilesAsync", ex);
            throw new SoapFaultException($"Failed to get profiles: {ex.Message}");
        }

        return profiles;
    }

    public async Task<string> GetStreamUriAsync(string serviceUrl, string profileToken, string? username = null, string? password = null)
    {
        try
        {
            var body = new XElement(TrtNs + "GetStreamUri",
                new XElement(TrtNs + "StreamSetup",
                    new XElement(TtNs + "Stream", "RTP-Unicast"),
                    new XElement(TtNs + "Transport",
                        new XElement(TtNs + "Protocol", "RTSP"))),
                new XElement(TrtNs + "ProfileToken", profileToken));

            var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);
            return response.Descendants().FirstOrDefault(e => e.Name.LocalName == "Uri")?.Value ?? string.Empty;
        }
        catch (SoapFaultException) { throw; }
        catch (Exception ex)
        {
            CrashLogger.Log("GetStreamUriAsync", ex);
            return string.Empty;
        }
    }

    public async Task<string> GetSnapshotUriAsync(string serviceUrl, string profileToken, string? username = null, string? password = null)
    {
        try
        {
            var body = new XElement(TrtNs + "GetSnapshotUri",
                new XElement(TrtNs + "ProfileToken", profileToken));

            var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);
            return response.Descendants().FirstOrDefault(e => e.Name.LocalName == "Uri")?.Value ?? string.Empty;
        }
        catch (SoapFaultException) { throw; }
        catch (Exception ex)
        {
            CrashLogger.Log("GetSnapshotUriAsync", ex);
            return string.Empty;
        }
    }

    public async Task<List<VideoEncoderConfig>> GetVideoEncoderConfigurationsAsync(string serviceUrl, string? username = null, string? password = null)
    {
        var configs = new List<VideoEncoderConfig>();

        try
        {
            var body = new XElement(TrtNs + "GetVideoEncoderConfigurations");
            var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);

            foreach (var el in response.Descendants().Where(e => e.Name.LocalName == "Configurations"))
            {
                configs.Add(new VideoEncoderConfig
                {
                    Name = Val(el, "Name"),
                    Encoding = Val(el, "Encoding"),
                    Width = int.TryParse(Val(el, "Width", "0"), out var w) ? w : 0,
                    Height = int.TryParse(Val(el, "Height", "0"), out var h) ? h : 0,
                    FrameRate = int.TryParse(Val(el, "FrameRateLimit", "0"), out var fr) ? fr : 0,
                    BitRate = int.TryParse(Val(el, "BitrateLimit", "0"), out var br) ? br : 0,
                    Quality = float.TryParse(Val(el, "Quality", "0"), out var q) ? (int)q : 0
                });
            }
        }
        catch (SoapFaultException) { throw; }
        catch (Exception ex)
        {
            CrashLogger.Log("GetVideoEncoderConfigurationsAsync", ex);
        }

        return configs;
    }

    public void Dispose()
    {
        _soapClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
