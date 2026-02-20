using System.Drawing;

namespace WindowCaptureCL;

/// <summary>
/// Represents information about a window for capture purposes.
/// </summary>
public class WindowInfo
{
    public IntPtr Handle { get; }
    public string Title { get; }
    public string ClassName { get; }
    public Rectangle Bounds { get; }
    public bool IsVisible { get; }

    internal WindowInfo(IntPtr handle, string title, string className, Rectangle bounds, bool isVisible)
    {
        Handle = handle;
        Title = title;
        ClassName = className;
        Bounds = bounds;
        IsVisible = isVisible;
    }
}
