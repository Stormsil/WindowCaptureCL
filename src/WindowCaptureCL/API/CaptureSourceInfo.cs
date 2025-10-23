using System.Drawing;

namespace WindowCaptureCL;

/// <summary>
/// Provides information about a capture source (window, monitor, or region).
/// </summary>
public sealed class CaptureSourceInfo
{
    /// <summary>
    /// Gets the unique identifier for the capture source.
    /// </summary>
    public object Id { get; }

    /// <summary>
    /// Gets the type of the capture source.
    /// </summary>
    public CaptureSourceType SourceType { get; }

    /// <summary>
    /// Gets the display name of the capture source (window title or monitor name).
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the width of the capture source in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the height of the capture source in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the window handle if the source type is Window, otherwise IntPtr.Zero.
    /// </summary>
    public IntPtr WindowHandle { get; }

    /// <summary>
    /// Gets the monitor index if the source type is Monitor or Region, otherwise -1.
    /// </summary>
    public int MonitorIndex { get; }

    /// <summary>
    /// Gets the capture region if the source type is Region, otherwise Rectangle.Empty.
    /// </summary>
    public Rectangle Region { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureSourceInfo"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the capture source.</param>
    /// <param name="sourceType">The type of the capture source.</param>
    /// <param name="displayName">The display name of the capture source.</param>
    /// <param name="width">The width of the capture source in pixels (0 if not yet determined).</param>
    /// <param name="height">The height of the capture source in pixels (0 if not yet determined).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> or <paramref name="displayName"/> is null.</exception>
    public CaptureSourceInfo(object id, CaptureSourceType sourceType, string displayName, int width, int height)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        SourceType = sourceType;
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Width = width;
        Height = height;

        // Set type-specific properties based on Id type
        if (id is IntPtr handle)
        {
            WindowHandle = handle;
            MonitorIndex = -1;
            Region = Rectangle.Empty;
        }
        else if (id is int index)
        {
            WindowHandle = IntPtr.Zero;
            MonitorIndex = index;
            Region = Rectangle.Empty;
        }
        else
        {
            WindowHandle = IntPtr.Zero;
            MonitorIndex = -1;
            Region = Rectangle.Empty;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureSourceInfo"/> class for region capture.
    /// </summary>
    /// <param name="monitorIndex">The monitor index.</param>
    /// <param name="region">The capture region.</param>
    /// <param name="displayName">The display name of the capture source.</param>
    public CaptureSourceInfo(int monitorIndex, Rectangle region, string displayName)
    {
        Id = (monitorIndex, region);
        SourceType = CaptureSourceType.Region;
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Width = region.Width;
        Height = region.Height;
        WindowHandle = IntPtr.Zero;
        MonitorIndex = monitorIndex;
        Region = region;
    }
}
