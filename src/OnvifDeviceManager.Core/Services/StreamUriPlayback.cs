using OnvifDeviceManager.Models;

namespace OnvifDeviceManager.Services;

/// <summary>Fixes common ONVIF stream URL issues before LibVLC playback.</summary>
public static class StreamUriPlayback
{
    public static string? GetConnectionHost(OnvifDevice? device)
    {
        if (device == null) return null;
        if (!string.IsNullOrWhiteSpace(device.Address))
            return device.Address.Trim();
        if (Uri.TryCreate(device.ServiceAddress, UriKind.Absolute, out var u))
            return u.IdnHost;
        return null;
    }

    /// <summary>Many cameras return rtsp://127.0.0.1/... which only works on-device; use the host we used for ONVIF.</summary>
    public static string ApplyDeviceHost(string uriString, OnvifDevice? device)
    {
        if (string.IsNullOrWhiteSpace(uriString) || device == null) return uriString;
        if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri)) return uriString;
        var host = uri.IdnHost;
        if (!IsLoopbackOrUnusableHost(host)) return uriString;
        var preferred = GetConnectionHost(device);
        if (string.IsNullOrWhiteSpace(preferred)) return uriString;
        var b = new UriBuilder(uri) { Host = preferred };
        return b.Uri.ToString();
    }

    private static bool IsLoopbackOrUnusableHost(string host)
    {
        if (string.IsNullOrEmpty(host)) return false;
        if (host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)) return true;
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;
        if (host.Equals("::1", StringComparison.OrdinalIgnoreCase)) return true;
        if (host.Equals("[::1]", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
