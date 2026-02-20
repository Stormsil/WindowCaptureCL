namespace WindowCaptureCL.Infrastructure.WGC;

/// <summary>
/// Represents information about a window.
/// </summary>
public sealed class WindowInfo
{
    /// <summary>
    /// Gets the window handle (HWND).
    /// </summary>
    public IntPtr Handle { get; }

    /// <summary>
    /// Gets the window title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the window width in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the window height in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowInfo"/> class.
    /// </summary>
    /// <param name="handle">The window handle.</param>
    /// <param name="title">The window title.</param>
    /// <param name="width">The window width in pixels.</param>
    /// <param name="height">The window height in pixels.</param>
    public WindowInfo(IntPtr handle, string title, int width, int height)
    {
        Handle = handle;
        Title = title;
        Width = width;
        Height = height;
    }
}
