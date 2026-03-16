using System.Globalization;
using System.Xml.Linq;
using OnvifDeviceManager.Models;

namespace OnvifDeviceManager.Services;

public class OnvifPtzService : IDisposable
{
    private readonly SoapClient _soapClient = new();

    private static readonly XNamespace PtzNs = "http://www.onvif.org/ver20/ptz/wsdl";
    private static readonly XNamespace TtNs = "http://www.onvif.org/ver10/schema";

    private static XElement? Find(XElement parent, string localName)
        => parent.Descendants().FirstOrDefault(e => e.Name.LocalName == localName);

    public async Task ContinuousMoveAsync(string serviceUrl, string profileToken, float panSpeed, float tiltSpeed, float zoomSpeed, string? username = null, string? password = null)
    {
        try
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
        catch (SoapFaultException) { throw; }
        catch (Exception ex)
        {
            CrashLogger.Log("ContinuousMoveAsync", ex);
            throw new SoapFaultException($"PTZ move failed: {ex.Message}");
        }
    }

    public async Task StopAsync(string serviceUrl, string profileToken, bool panTilt = true, bool zoom = true, string? username = null, string? password = null)
    {
        try
        {
            var body = new XElement(PtzNs + "Stop",
                new XElement(PtzNs + "ProfileToken", profileToken),
                new XElement(PtzNs + "PanTilt", panTilt.ToString().ToLower()),
                new XElement(PtzNs + "Zoom", zoom.ToString().ToLower()));

            await _soapClient.SendRequestAsync(serviceUrl, body, username, password);
        }
        catch (SoapFaultException) { throw; }
        catch (Exception ex)
        {
            CrashLogger.Log("StopAsync", ex);
        }
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
        var status = new PtzStatus();

        try
        {
            var body = new XElement(PtzNs + "GetStatus",
                new XElement(PtzNs + "ProfileToken", profileToken));

            var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);

            var position = Find(response, "Position");
            if (position != null)
            {
                var panTilt = Find(position, "PanTilt");
                if (panTilt != null)
                {
                    float.TryParse(panTilt.Attribute("x")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pan);
                    float.TryParse(panTilt.Attribute("y")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tilt);
                    status.Pan = pan;
                    status.Tilt = tilt;
                }

                var zoom = Find(position, "Zoom");
                if (zoom != null)
                {
                    float.TryParse(zoom.Attribute("x")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var z);
                    status.Zoom = z;
                }
            }

            var moveStatus = Find(response, "MoveStatus");
            if (moveStatus != null)
                status.MoveStatus = Find(moveStatus, "PanTilt")?.Value ?? "IDLE";
        }
        catch (SoapFaultException) { throw; }
        catch (Exception ex)
        {
            CrashLogger.Log("GetStatusAsync", ex);
        }

        return status;
    }

    public async Task<List<PtzPreset>> GetPresetsAsync(string serviceUrl, string profileToken, string? username = null, string? password = null)
    {
        var presets = new List<PtzPreset>();

        try
        {
            var body = new XElement(PtzNs + "GetPresets",
                new XElement(PtzNs + "ProfileToken", profileToken));

            var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);

            foreach (var el in response.Descendants().Where(e => e.Name.LocalName == "Preset"))
            {
                try
                {
                    var preset = new PtzPreset
                    {
                        Token = el.Attribute("token")?.Value ?? string.Empty,
                        Name = el.Descendants().FirstOrDefault(e => e.Name.LocalName == "Name")?.Value ?? string.Empty
                    };

                    var panTilt = Find(el, "PanTilt");
                    if (panTilt != null)
                    {
                        float.TryParse(panTilt.Attribute("x")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pan);
                        float.TryParse(panTilt.Attribute("y")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tilt);
                        preset.PanPosition = pan;
                        preset.TiltPosition = tilt;
                    }

                    var zoom = Find(el, "Zoom");
                    if (zoom != null)
                    {
                        float.TryParse(zoom.Attribute("x")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var z);
                        preset.ZoomPosition = z;
                    }

                    presets.Add(preset);
                }
                catch (Exception ex)
                {
                    CrashLogger.Log("GetPresetsAsync - parsing preset", ex);
                }
            }
        }
        catch (SoapFaultException) { throw; }
        catch (Exception ex)
        {
            CrashLogger.Log("GetPresetsAsync", ex);
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
            body.Add(new XElement(PtzNs + "PresetToken", presetToken));

        var response = await _soapClient.SendRequestAsync(serviceUrl, body, username, password);
        return response.Descendants().FirstOrDefault(e => e.Name.LocalName == "PresetToken")?.Value ?? string.Empty;
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
