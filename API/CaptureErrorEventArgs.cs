namespace WindowCaptureCL;

/// <summary>
/// Provides data for the <see cref="ICaptureSession.CaptureError"/> event.
/// </summary>
public sealed class CaptureErrorEventArgs : EventArgs
{
    /// <summary>
    /// Gets the exception that caused the error.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets the timestamp when the error occurred.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets a value indicating whether the capture session can continue after this error.
    /// </summary>
    public bool CanContinue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureErrorEventArgs"/> class.
    /// </summary>
    /// <param name="exception">The exception that caused the error.</param>
    /// <param name="timestamp">The timestamp when the error occurred.</param>
    /// <param name="canContinue">Indicates whether the capture session can continue after this error.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public CaptureErrorEventArgs(Exception exception, DateTime timestamp, bool canContinue)
    {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        Timestamp = timestamp;
        CanContinue = canContinue;
    }
}
