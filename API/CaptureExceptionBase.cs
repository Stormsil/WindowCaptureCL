namespace WindowCaptureCL;

/// <summary>
/// Base exception class for all WindowCaptureCL errors.
/// </summary>
public abstract class CaptureException : Exception
{
    protected CaptureException()
    {
    }

    protected CaptureException(string message) : base(message)
    {
    }

    protected CaptureException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Abstract base exception for invalid capture targets (windows, monitors, regions).
/// </summary>
public abstract class CaptureTargetInvalidException : CaptureException
{
    public object? Target { get; }

    protected CaptureTargetInvalidException()
    {
    }

    protected CaptureTargetInvalidException(string message) : base(message)
    {
    }

    protected CaptureTargetInvalidException(string message, object target) : base(message)
    {
        Target = target;
    }

    protected CaptureTargetInvalidException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected CaptureTargetInvalidException(string message, object target, Exception innerException) : base(message, innerException)
    {
        Target = target;
    }
}
