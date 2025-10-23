namespace WindowCaptureCL;

/// <summary>
/// Specifies the reason why a capture session stopped.
/// </summary>
public enum CaptureStopReason
{
    /// <summary>
    /// The capture was stopped by the user calling Stop().
    /// </summary>
    UserRequested = 0,

    /// <summary>
    /// The capture source (window or monitor) was closed or became invalid.
    /// </summary>
    SourceClosed = 1,

    /// <summary>
    /// The capture stopped due to an error.
    /// </summary>
    Error = 2
}
