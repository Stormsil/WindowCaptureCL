namespace WindowCaptureCL;

/// <summary>
/// Exception thrown when attempting to start a capture session that is already running.
/// </summary>
public sealed class SessionAlreadyStartedException : CaptureException
{
    public SessionAlreadyStartedException()
        : base("The capture session is already running.")
    {
    }

    public SessionAlreadyStartedException(string message) : base(message)
    {
    }

    public SessionAlreadyStartedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when attempting to stop or perform operations on a capture session that is not running.
/// </summary>
public sealed class SessionNotStartedException : CaptureException
{
    public SessionNotStartedException()
        : base("The capture session is not running.")
    {
    }

    public SessionNotStartedException(string message) : base(message)
    {
    }

    public SessionNotStartedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
