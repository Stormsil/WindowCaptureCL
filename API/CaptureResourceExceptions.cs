namespace WindowCaptureCL;

/// <summary>
/// Exception thrown when DirectX device initialization or operations fail.
/// </summary>
public sealed class DirectXException : CaptureException
{
    public new int? HResult { get; }

    public DirectXException()
        : base("A DirectX operation failed.")
    {
    }

    public DirectXException(string message) : base(message)
    {
    }

    public DirectXException(string message, int hresult) : base(message)
    {
        HResult = hresult;
    }

    public DirectXException(string message, Exception innerException) : base(message, innerException)
    {
    }

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
    public FrameCaptureException()
        : base("Failed to capture frame.")
    {
    }

    public FrameCaptureException(string message) : base(message)
    {
    }

    public FrameCaptureException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when resource allocation or management operations fail.
/// </summary>
public sealed class ResourceAllocationException : CaptureException
{
    public string? ResourceName { get; }

    public ResourceAllocationException()
        : base("Failed to allocate required resources.")
    {
    }

    public ResourceAllocationException(string message) : base(message)
    {
    }

    public ResourceAllocationException(string message, string resourceName) : base(message)
    {
        ResourceName = resourceName;
    }

    public ResourceAllocationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ResourceAllocationException(string message, string resourceName, Exception innerException) : base(message, innerException)
    {
        ResourceName = resourceName;
    }
}

/// <summary>
/// Exception thrown when graphics device operations fail.
/// </summary>
public sealed class GraphicsDeviceException : CaptureException
{
    public GraphicsDeviceException()
        : base("A graphics device operation failed.")
    {
    }

    public GraphicsDeviceException(string message) : base(message)
    {
    }

    public GraphicsDeviceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
