namespace WindowCaptureCL;

/// <summary>
/// Exception thrown when configuration validation fails.
/// </summary>
public sealed class InvalidConfigurationException : CaptureException
{
    public string? PropertyName { get; }

    public InvalidConfigurationException()
        : base("The configuration is invalid.")
    {
    }

    public InvalidConfigurationException(string message) : base(message)
    {
    }

    public InvalidConfigurationException(string message, string propertyName) : base(message)
    {
        PropertyName = propertyName;
    }

    public InvalidConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public InvalidConfigurationException(string message, string propertyName, Exception innerException) : base(message, innerException)
    {
        PropertyName = propertyName;
    }
}

/// <summary>
/// Exception thrown when a specified window handle is not found or is invalid.
/// </summary>
public sealed class WindowNotFoundException : CaptureTargetInvalidException
{
    public IntPtr WindowHandle { get; }

    public WindowNotFoundException()
        : base("The specified window was not found.")
    {
    }

    public WindowNotFoundException(string message) : base(message)
    {
    }

    public WindowNotFoundException(string message, IntPtr windowHandle) : base(message, windowHandle)
    {
        WindowHandle = windowHandle;
    }

    public WindowNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }

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
    public int MonitorIndex { get; }

    public InvalidMonitorException()
        : base("The specified monitor index is invalid.")
    {
    }

    public InvalidMonitorException(string message) : base(message)
    {
    }

    public InvalidMonitorException(string message, int monitorIndex) : base(message, monitorIndex)
    {
        MonitorIndex = monitorIndex;
    }

    public InvalidMonitorException(string message, Exception innerException) : base(message, innerException)
    {
    }

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
    public System.Drawing.Rectangle AttemptedRegion { get; }
    public System.Drawing.Size MonitorSize { get; }

    public RegionOutOfBoundsException()
        : base("The specified capture region extends beyond the monitor bounds.")
    {
    }

    public RegionOutOfBoundsException(string message) : base(message)
    {
    }

    public RegionOutOfBoundsException(string message, System.Drawing.Rectangle attemptedRegion, System.Drawing.Size monitorSize) : base(message)
    {
        AttemptedRegion = attemptedRegion;
        MonitorSize = monitorSize;
    }

    public RegionOutOfBoundsException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public RegionOutOfBoundsException(string message, System.Drawing.Rectangle attemptedRegion, System.Drawing.Size monitorSize, Exception innerException) : base(message, innerException)
    {
        AttemptedRegion = attemptedRegion;
        MonitorSize = monitorSize;
    }
}
