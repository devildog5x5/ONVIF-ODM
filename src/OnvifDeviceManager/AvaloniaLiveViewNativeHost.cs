namespace OnvifDeviceManager;

/// <summary>
/// When Avalonia Win32 NativeControlHost (LibVLC VideoView) cannot create a child HWND, repeated
/// layout attempts keep failing and can destabilize the rest of the Live View page (e.g. snapshot).
/// We stop trying after the first confirmed failure and steer users to external playback.
/// </summary>
public static class AvaloniaLiveViewNativeHost
{
    public static bool EmbeddedVideoDisabledDueToHostFailure { get; private set; }

    public static event Action? EmbeddedVideoCapabilityChanged;

    public static void ReportHostFailure()
    {
        if (EmbeddedVideoDisabledDueToHostFailure)
            return;
        EmbeddedVideoDisabledDueToHostFailure = true;
        EmbeddedVideoCapabilityChanged?.Invoke();
    }
}
