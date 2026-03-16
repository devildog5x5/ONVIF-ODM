namespace OnvifDeviceManager.Models;

public class PtzPreset
{
    public string Token { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public float PanPosition { get; set; }
    public float TiltPosition { get; set; }
    public float ZoomPosition { get; set; }
}

public class PtzStatus
{
    public float Pan { get; set; }
    public float Tilt { get; set; }
    public float Zoom { get; set; }
    public string MoveStatus { get; set; } = "IDLE";
}

public class PtzLimits
{
    public float MinPan { get; set; } = -1.0f;
    public float MaxPan { get; set; } = 1.0f;
    public float MinTilt { get; set; } = -1.0f;
    public float MaxTilt { get; set; } = 1.0f;
    public float MinZoom { get; set; } = 0.0f;
    public float MaxZoom { get; set; } = 1.0f;
}
