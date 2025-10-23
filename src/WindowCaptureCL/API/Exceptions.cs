namespace WindowCaptureCL;

/// <summary>
/// Base exception class for all WindowCaptureCL errors.
/// </summary>
public abstract class CaptureException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureException"/> class.
    /// </summary>
    protected CaptureException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    protected CaptureException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    protected CaptureException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when Windows Graphics Capture API is not supported on the current system.
/// </summary>
public sealed class GraphicsCaptureNotSupportedException : CaptureException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphicsCaptureNotSupportedException"/> class.
    /// </summary>
    public GraphicsCaptureNotSupportedException()
        : base("Windows Graphics Capture API is not supported on this system. Requires Windows 10 version 1803 or later.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphicsCaptureNotSupportedException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public GraphicsCaptureNotSupportedException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphicsCaptureNotSupportedException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public GraphicsCaptureNotSupportedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a requested capture source (window, screen) cannot be found.
/// </summary>
public sealed class CaptureSourceNotFoundException : CaptureException
{
    /// <summary>
    /// Gets the identifier of the source that was not found.
    /// </summary>
    public object? SourceId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureSourceNotFoundException"/> class.
    /// </summary>
    public CaptureSourceNotFoundException()
        : base("The requested capture source could not be found.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureSourceNotFoundException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public CaptureSourceNotFoundException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureSourceNotFoundException"/> class with a specified error message
    /// and source identifier.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="sourceId">The identifier of the source that was not found.</param>
    public CaptureSourceNotFoundException(string message, object sourceId) : base(message)
    {
        SourceId = sourceId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureSourceNotFoundException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public CaptureSourceNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureSourceNotFoundException"/> class with a specified error message,
    /// source identifier, and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="sourceId">The identifier of the source that was not found.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
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
    /// <summary>
    /// Gets the current state of the capture session when the exception was thrown.
    /// </summary>
    public string? CurrentState { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCaptureStateException"/> class.
    /// </summary>
    public InvalidCaptureStateException()
        : base("The capture session is not in a valid state for this operation.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCaptureStateException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public InvalidCaptureStateException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCaptureStateException"/> class with a specified error message
    /// and current state.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="currentState">The current state of the capture session.</param>
    public InvalidCaptureStateException(string message, string currentState) : base(message)
    {
        CurrentState = currentState;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCaptureStateException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public InvalidCaptureStateException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCaptureStateException"/> class with a specified error message,
    /// current state, and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="currentState">The current state of the capture session.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public InvalidCaptureStateException(string message, string currentState, Exception innerException) : base(message, innerException)
    {
        CurrentState = currentState;
    }
}

/// <summary>
/// Exception thrown when DirectX device initialization or operations fail.
/// </summary>
public sealed class DirectXException : CaptureException
{
    /// <summary>
    /// Gets the HRESULT error code from the DirectX operation, if available.
    /// </summary>
    public new int? HResult { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectXException"/> class.
    /// </summary>
    public DirectXException()
        : base("A DirectX operation failed.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectXException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DirectXException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectXException"/> class with a specified error message
    /// and HRESULT code.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="hresult">The HRESULT error code from the DirectX operation.</param>
    public DirectXException(string message, int hresult) : base(message)
    {
        HResult = hresult;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectXException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public DirectXException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectXException"/> class with a specified error message,
    /// HRESULT code, and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="hresult">The HRESULT error code from the DirectX operation.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public DirectXException(string message, int hresult, Exception innerException) : base(message, innerException)
    {
        HResult = hresult;
    }
}

/// <summary>
/// Exception thrown when frame capture operations fail.
/// </summary>
public sealed class FrameCaptureException : CaptureException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FrameCaptureException"/> class.
    /// </summary>
    public FrameCaptureException()
        : base("Failed to capture frame.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameCaptureException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public FrameCaptureException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameCaptureException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public FrameCaptureException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when resource allocation or management operations fail.
/// </summary>
public sealed class ResourceAllocationException : CaptureException
{
    /// <summary>
    /// Gets the name of the resource that failed to allocate, if available.
    /// </summary>
    public string? ResourceName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceAllocationException"/> class.
    /// </summary>
    public ResourceAllocationException()
        : base("Failed to allocate required resources.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceAllocationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ResourceAllocationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceAllocationException"/> class with a specified error message
    /// and resource name.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="resourceName">The name of the resource that failed to allocate.</param>
    public ResourceAllocationException(string message, string resourceName) : base(message)
    {
        ResourceName = resourceName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceAllocationException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ResourceAllocationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceAllocationException"/> class with a specified error message,
    /// resource name, and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="resourceName">The name of the resource that failed to allocate.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ResourceAllocationException(string message, string resourceName, Exception innerException) : base(message, innerException)
    {
        ResourceName = resourceName;
    }
}

/// <summary>
/// Exception thrown when configuration validation fails.
/// </summary>
public sealed class InvalidConfigurationException : CaptureException
{
    /// <summary>
    /// Gets the name of the configuration property that is invalid, if available.
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidConfigurationException"/> class.
    /// </summary>
    public InvalidConfigurationException()
        : base("The configuration is invalid.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidConfigurationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public InvalidConfigurationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidConfigurationException"/> class with a specified error message
    /// and property name.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="propertyName">The name of the configuration property that is invalid.</param>
    public InvalidConfigurationException(string message, string propertyName) : base(message)
    {
        PropertyName = propertyName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidConfigurationException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public InvalidConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidConfigurationException"/> class with a specified error message,
    /// property name, and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="propertyName">The name of the configuration property that is invalid.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public InvalidConfigurationException(string message, string propertyName, Exception innerException) : base(message, innerException)
    {
        PropertyName = propertyName;
    }
}

/// <summary>
/// Exception thrown when a requested operation or feature is not supported on the current platform or configuration.
/// </summary>
public sealed class UnsupportedOperationException : CaptureException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedOperationException"/> class.
    /// </summary>
    public UnsupportedOperationException()
        : base("The requested operation is not supported.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedOperationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public UnsupportedOperationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedOperationException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public UnsupportedOperationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Abstract base exception for invalid capture targets (windows, monitors, regions).
/// </summary>
public abstract class CaptureTargetInvalidException : CaptureException
{
    /// <summary>
    /// Gets the invalid target identifier.
    /// </summary>
    public object? Target { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureTargetInvalidException"/> class.
    /// </summary>
    protected CaptureTargetInvalidException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureTargetInvalidException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    protected CaptureTargetInvalidException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureTargetInvalidException"/> class with a specified error message and target.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="target">The invalid target identifier.</param>
    protected CaptureTargetInvalidException(string message, object target) : base(message)
    {
        Target = target;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureTargetInvalidException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    protected CaptureTargetInvalidException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureTargetInvalidException"/> class with a specified error message,
    /// target, and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="target">The invalid target identifier.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    protected CaptureTargetInvalidException(string message, object target, Exception innerException) : base(message, innerException)
    {
        Target = target;
    }
}

/// <summary>
/// Exception thrown when a specified window handle is not found or is invalid.
/// </summary>
public sealed class WindowNotFoundException : CaptureTargetInvalidException
{
    /// <summary>
    /// Gets the window handle that was not found.
    /// </summary>
    public IntPtr WindowHandle { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowNotFoundException"/> class.
    /// </summary>
    public WindowNotFoundException()
        : base("The specified window was not found.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowNotFoundException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public WindowNotFoundException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowNotFoundException"/> class with a specified error message
    /// and window handle.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="windowHandle">The window handle that was not found.</param>
    public WindowNotFoundException(string message, IntPtr windowHandle) : base(message, windowHandle)
    {
        WindowHandle = windowHandle;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowNotFoundException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public WindowNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowNotFoundException"/> class with a specified error message,
    /// window handle, and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="windowHandle">The window handle that was not found.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public WindowNotFoundException(string message, IntPtr windowHandle, Exception innerException) : base(message, windowHandle, innerException)
    {
        WindowHandle = windowHandle;
    }
}

/// <summary>
/// Exception thrown when a specified monitor index is invalid or out of range.
/// </summary>
public sealed class InvalidMonitorException : CaptureTargetInvalidException
{
    /// <summary>
    /// Gets the monitor index that was invalid.
    /// </summary>
    public int MonitorIndex { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidMonitorException"/> class.
    /// </summary>
    public InvalidMonitorException()
        : base("The specified monitor index is invalid.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidMonitorException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public InvalidMonitorException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidMonitorException"/> class with a specified error message
    /// and monitor index.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="monitorIndex">The monitor index that was invalid.</param>
    public InvalidMonitorException(string message, int monitorIndex) : base(message, monitorIndex)
    {
        MonitorIndex = monitorIndex;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidMonitorException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public InvalidMonitorException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidMonitorException"/> class with a specified error message,
    /// monitor index, and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="monitorIndex">The monitor index that was invalid.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public InvalidMonitorException(string message, int monitorIndex, Exception innerException) : base(message, monitorIndex, innerException)
    {
        MonitorIndex = monitorIndex;
    }
}

/// <summary>
/// Exception thrown when a specified capture region extends beyond monitor bounds.
/// </summary>
public sealed class RegionOutOfBoundsException : CaptureException
{
    /// <summary>
    /// Gets the region that was attempted to be captured.
    /// </summary>
    public System.Drawing.Rectangle AttemptedRegion { get; }

    /// <summary>
    /// Gets the size of the monitor that was targeted.
    /// </summary>
    public System.Drawing.Size MonitorSize { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegionOutOfBoundsException"/> class.
    /// </summary>
    public RegionOutOfBoundsException()
        : base("The specified capture region extends beyond the monitor bounds.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegionOutOfBoundsException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public RegionOutOfBoundsException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegionOutOfBoundsException"/> class with a specified error message,
    /// attempted region, and monitor size.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="attemptedRegion">The region that was attempted to be captured.</param>
    /// <param name="monitorSize">The size of the monitor.</param>
    public RegionOutOfBoundsException(string message, System.Drawing.Rectangle attemptedRegion, System.Drawing.Size monitorSize) : base(message)
    {
        AttemptedRegion = attemptedRegion;
        MonitorSize = monitorSize;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegionOutOfBoundsException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public RegionOutOfBoundsException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegionOutOfBoundsException"/> class with a specified error message,
    /// attempted region, monitor size, and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="attemptedRegion">The region that was attempted to be captured.</param>
    /// <param name="monitorSize">The size of the monitor.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public RegionOutOfBoundsException(string message, System.Drawing.Rectangle attemptedRegion, System.Drawing.Size monitorSize, Exception innerException) : base(message, innerException)
    {
        AttemptedRegion = attemptedRegion;
        MonitorSize = monitorSize;
    }
}

/// <summary>
/// Exception thrown when attempting to start a capture session that is already running.
/// </summary>
public sealed class SessionAlreadyStartedException : CaptureException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionAlreadyStartedException"/> class.
    /// </summary>
    public SessionAlreadyStartedException()
        : base("The capture session is already running.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionAlreadyStartedException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public SessionAlreadyStartedException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionAlreadyStartedException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public SessionAlreadyStartedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when attempting to stop or perform operations on a capture session that is not running.
/// </summary>
public sealed class SessionNotStartedException : CaptureException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionNotStartedException"/> class.
    /// </summary>
    public SessionNotStartedException()
        : base("The capture session is not running.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionNotStartedException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public SessionNotStartedException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionNotStartedException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public SessionNotStartedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when graphics device operations fail.
/// </summary>
public sealed class GraphicsDeviceException : CaptureException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphicsDeviceException"/> class.
    /// </summary>
    public GraphicsDeviceException()
        : base("A graphics device operation failed.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphicsDeviceException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public GraphicsDeviceException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphicsDeviceException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public GraphicsDeviceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
