using System.Globalization;
using System.Xml.Linq;
using OnvifDeviceManager.Models;

namespace OnvifDeviceManager.Services;

public class OnvifPtzService : IDisposable
{
    private readonly SoapClient _soapClient = new();

    private static readonly XNamespace PtzNs = "http://www.onvif.org/ver20/ptz/wsdl";
    private static readonly XNamespace TtNs = "http://www.onvif.org/ver10/schema";

    public async Task ContinuousMoveAsync(string serviceUrl, string profileToken, float panSpeed, float tiltSpeed, float zoomSpeed, string? username = null, string? password = null)
    {
        var body = new XElement(PtzNs + "ContinuousMove",
            new XElement(PtzNs + "ProfileToken", profileToken),
            new XElement(PtzNs + "Velocity",
                new XElement(TtNs + "PanTilt",
                    new XAttribute("x", panSpeed.ToString("F2", CultureInfo.InvariantCulture)),
                    new XAttribute("y", tiltSpeed.ToString("F2", CultureInfo.InvariantCulture))),
                new XElement(TtNs + "Zoom",
                    new XAttribute("x", zoomSpeed.ToString("F2", CultureInfo.InvariantCulture)))));

        await _soapClient.SendRequestAsync(serviceUrl, body, username, password);
    }

    public async Task StopAsync(string serviceUrl, string profileToken, bool panTilt = true, bool zoom = true, string? username = null, string? password = null)
    {
        var body = new XElement(PtzNs + "Stop",
            new XElement(PtzNs + "ProfileToken", profileToken),
            new XElement(PtzNs + "PanTilt", panTilt.ToString().ToLower()),
            new XElement(PtzNs + "Zoom", zoom.ToString().ToLower()));

        await _soapClient.SendRequestAsync(serviceUrl, body, username, password);
    }

    public async Task AbsoluteMoveAsync(string serviceUrl, string profileToken, float pan, float tilt, float zoom, string? username = null, string? password = null)
    {
        var body = new XElement(PtzNs + "AbsoluteMove",
            new XElement(PtzNs + "ProfileToken", profileToken),
            new XElement(PtzNs + "Position",
                new XElement(TtNs + "PanTilt",
                    new XAttribute("x", pan.ToString("F4", CultureInfo.InvariantCulture)),
                    new XAttribute("y", tilt.ToString("F4", CultureInfo.InvariantCulture))),
                new XElement(TtNs + "Zoom",
                    new XAttribute("x", zoom.ToString("F4", CultureInfo.InvariantCulture)))));

        await _soapClient.SendRequestAsync(serviceUrl, body, username, password);
    }

    public async Task RelativeMoveAsync(string serviceUrl, string profileToken, float pan, float tilt, float zoom, string? username = null, string? password = null)
    {
        var body = new XElement(PtzNs + "RelativeMove",
            new XElement(PtzNs + "ProfileToken", profileToken),
            new XElement(PtzNs + "Translation",
                new XElement(TtNs + "PanTilt",
                    new XAttribute("x", pan.ToString("F4", CultureInfo.InvariantCulture)),
                    new XAttribute("y", tilt.ToString("F4", CultureInfo.InvariantCulture))),
                new XElement(TtNs + "Zoom",
                    new XAttribute("x", zoom.ToString("F4", CultureInfo.InvariantCulture)))));

        await _soapClient.SendRequestAsync(serviceUrl, body, username, password);
    }

    public async Task<PtzStatus> GetStatusAsync(string serviceUrl, string profileToken, string? username = null, string? password = null)
    {
        var body = new XElement(PtzNs + "GetStatus",
            new XElement(PtzNs + "ProfileToken", profileToken));

        var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);
        var status = new PtzStatus();

        var ptzStatus = response.Descendants(PtzNs + "PTZStatus").FirstOrDefault();
        if (ptzStatus != null)
        {
            var position = ptzStatus.Element(TtNs + "Position");
            if (position != null)
            {
                var panTilt = position.Element(TtNs + "PanTilt");
                if (panTilt != null)
                {
                    float.TryParse(panTilt.Attribute("x")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pan);
                    float.TryParse(panTilt.Attribute("y")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tilt);
                    status.Pan = pan;
                    status.Tilt = tilt;
                }

                var zoom = position.Element(TtNs + "Zoom");
                if (zoom != null)
                {
                    float.TryParse(zoom.Attribute("x")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var z);
                    status.Zoom = z;
                }
            }

            var moveStatus = ptzStatus.Element(TtNs + "MoveStatus");
            if (moveStatus != null)
            {
                status.MoveStatus = moveStatus.Element(TtNs + "PanTilt")?.Value ?? "IDLE";
            }
        }

        return status;
    }

    public async Task<List<PtzPreset>> GetPresetsAsync(string serviceUrl, string profileToken, string? username = null, string? password = null)
    {
        var body = new XElement(PtzNs + "GetPresets",
            new XElement(PtzNs + "ProfileToken", profileToken));

        var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);
        var presets = new List<PtzPreset>();

        foreach (var presetElement in response.Descendants(PtzNs + "Preset"))
        {
            var preset = new PtzPreset
            {
                Token = presetElement.Attribute("token")?.Value ?? string.Empty,
                Name = presetElement.Element(TtNs + "Name")?.Value ?? string.Empty
            };

            var position = presetElement.Element(TtNs + "PTZPosition");
            if (position != null)
            {
                var panTilt = position.Element(TtNs + "PanTilt");
                if (panTilt != null)
                {
                    float.TryParse(panTilt.Attribute("x")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pan);
                    float.TryParse(panTilt.Attribute("y")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tilt);
                    preset.PanPosition = pan;
                    preset.TiltPosition = tilt;
                }

                var zoom = position.Element(TtNs + "Zoom");
                if (zoom != null)
                {
                    float.TryParse(zoom.Attribute("x")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var z);
                    preset.ZoomPosition = z;
                }
            }

            presets.Add(preset);
        }

        return presets;
    }

    public async Task GotoPresetAsync(string serviceUrl, string profileToken, string presetToken, string? username = null, string? password = null)
    {
        var body = new XElement(PtzNs + "GotoPreset",
            new XElement(PtzNs + "ProfileToken", profileToken),
            new XElement(PtzNs + "PresetToken", presetToken));

        await _soapClient.SendRequestAsync(serviceUrl, body, username, password);
    }

    public async Task<string> SetPresetAsync(string serviceUrl, string profileToken, string presetName, string? presetToken = null, string? username = null, string? password = null)
    {
        var body = new XElement(PtzNs + "SetPreset",
            new XElement(PtzNs + "ProfileToken", profileToken),
            new XElement(PtzNs + "PresetName", presetName));

        if (!string.IsNullOrEmpty(presetToken))
        {
            body.Add(new XElement(PtzNs + "PresetToken", presetToken));
        }

        var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);
        return response.Descendants(PtzNs + "PresetToken").FirstOrDefault()?.Value ?? string.Empty;
    }

    public async Task RemovePresetAsync(string serviceUrl, string profileToken, string presetToken, string? username = null, string? password = null)
    {
        var body = new XElement(PtzNs + "RemovePreset",
            new XElement(PtzNs + "ProfileToken", profileToken),
            new XElement(PtzNs + "PresetToken", presetToken));

        await _soapClient.SendRequestAsync(serviceUrl, body, username, password);
    }

    public async Task GotoHomeAsync(string serviceUrl, string profileToken, string? username = null, string? password = null)
    {
        var body = new XElement(PtzNs + "GotoHomePosition",
            new XElement(PtzNs + "ProfileToken", profileToken));

        await _soapClient.SendRequestAsync(serviceUrl, body, username, password);
    }

    public async Task SetHomeAsync(string serviceUrl, string profileToken, string? username = null, string? password = null)
    {
        var body = new XElement(PtzNs + "SetHomePosition",
            new XElement(PtzNs + "ProfileToken", profileToken));

        await _soapClient.SendRequestAsync(serviceUrl, body, username, password);
    }

    public void Dispose()
    {
        _soapClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
