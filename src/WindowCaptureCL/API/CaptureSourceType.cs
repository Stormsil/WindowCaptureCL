namespace WindowCaptureCL;

/// <summary>
/// Specifies the type of capture source.
/// </summary>
public enum CaptureSourceType
{
    /// <summary>
    /// Capture source is a window.
    /// </summary>
    Window = 0,

    /// <summary>
    /// Capture source is a full monitor/screen.
    /// </summary>
    Monitor = 1,

    /// <summary>
    /// Capture source is a region of a monitor/screen.
    /// </summary>
    Region = 2
}
