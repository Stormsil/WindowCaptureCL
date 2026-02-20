namespace WindowCaptureCL;

/// <summary>
/// Exception thrown when Windows Graphics Capture API is not supported on the current system.
/// </summary>
public sealed class GraphicsCaptureNotSupportedException : CaptureException
{
    public GraphicsCaptureNotSupportedException()
        : base("Windows Graphics Capture API is not supported on this system. Requires Windows 10 version 1803 or later.")
    {
    }

    public GraphicsCaptureNotSupportedException(string message) : base(message)
    {
    }

    public GraphicsCaptureNotSupportedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a requested capture source (window, screen) cannot be found.
/// </summary>
public sealed class CaptureSourceNotFoundException : CaptureException
{
    public object? SourceId { get; }

    public CaptureSourceNotFoundException()
        : base("The requested capture source could not be found.")
    {
    }

    public CaptureSourceNotFoundException(string message) : base(message)
    {
    }

    public CaptureSourceNotFoundException(string message, object sourceId) : base(message)
    {
        SourceId = sourceId;
    }

    public CaptureSourceNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public CaptureSourceNotFoundException(string message, object sourceId, Exception innerException) : base(message, innerException)
    {
        SourceId = sourceId;
    }
}

/// <summary>
/// Exception thrown when a capture source is not in the expected state for the requested operation.
/// </summary>
public sealed class InvalidCaptureStateException : CaptureException
{
    public string? CurrentState { get; }

    public InvalidCaptureStateException()
        : base("The capture session is not in a valid state for this operation.")
    {
    }

    public InvalidCaptureStateException(string message) : base(message)
    {
    }

    public InvalidCaptureStateException(string message, string currentState) : base(message)
    {
        CurrentState = currentState;
    }

    public InvalidCaptureStateException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public InvalidCaptureStateException(string message, string currentState, Exception innerException) : base(message, innerException)
    {
        CurrentState = currentState;
    }
}

/// <summary>
/// Exception thrown when a requested operation or feature is not supported on the current platform or configuration.
/// </summary>
public sealed class UnsupportedOperationException : CaptureException
{
    public UnsupportedOperationException()
        : base("The requested operation is not supported.")
    {
    }

    public UnsupportedOperationException(string message) : base(message)
    {
    }

    public UnsupportedOperationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
