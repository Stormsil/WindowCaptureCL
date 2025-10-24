namespace WindowCaptureCL;

/// <summary>
/// Provides data for capture session stopped events.
/// </summary>
public sealed class CaptureStoppedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the reason why the capture session stopped.
    /// </summary>
    public CaptureStopReason Reason { get; }

    /// <summary>
    /// Gets the exception that caused the capture to stop, if Reason is Error.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureStoppedEventArgs"/> class.
    /// </summary>
    /// <param name="reason">The reason why the capture stopped.</param>
    /// <param name="exception">The exception that caused the stop, if applicable.</param>
    public CaptureStoppedEventArgs(CaptureStopReason reason, Exception? exception = null)
    {
        Reason = reason;
        Exception = exception;
    }
}
