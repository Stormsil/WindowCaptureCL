namespace WindowCaptureCL;

/// <summary>
/// Configuration settings for a capture session.
/// Provides both global default settings and per-session configuration.
/// </summary>
public sealed partial class CaptureConfiguration
{
    private static readonly object _staticLock = new();
    private static bool _globalIncludeCursor;
    private static bool _globalDrawBorder;
    private static int _globalDefaultTargetFPS = 30;

    /// <summary>
    /// The minimum allowed frames per second value.
    /// </summary>
    public const int MinimumFPS = 1;

    /// <summary>
    /// The maximum allowed frames per second value.
    /// </summary>
    public const int MaximumFPS = 120;

    static CaptureConfiguration()
    {
        _globalIncludeCursor = false;
        _globalDrawBorder = false;
        _globalDefaultTargetFPS = 30;
    }

    private int _maxFramesPerSecond = 30;
    private bool _includeCursor;
    private bool _drawBorder;
}
