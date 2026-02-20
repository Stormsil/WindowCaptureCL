using System.Drawing;
using WindowCaptureCL.Infrastructure;
using WindowCaptureCL.Infrastructure.WGC;

namespace WindowCaptureCL;

public static partial class CaptureFacade
{
    public static Bitmap CaptureWindow(IntPtr windowHandle, bool includeCursor = false, bool drawBorder = false)
    {
        if (windowHandle == IntPtr.Zero)
            throw new ArgumentException("Window handle cannot be zero.", nameof(windowHandle));

        DpiHelper.SetPerMonitorDpiAwareness();

        using var session = Capture.FromWindow(windowHandle);
        var config = new CaptureConfiguration
        {
            SessionIncludeCursor = includeCursor,
            SessionDrawBorder = drawBorder,
        };
        session.UpdateConfiguration(config);

        using var frame = session.CaptureFrame();
        return new Bitmap(frame.Bitmap);
    }

    public static async Task<Bitmap> CaptureWindowAsync(
        IntPtr windowHandle,
        bool includeCursor = false,
        bool drawBorder = false,
        CancellationToken cancellationToken = default)
    {
        if (windowHandle == IntPtr.Zero)
            throw new ArgumentException("Window handle cannot be zero.", nameof(windowHandle));

        DpiHelper.SetPerMonitorDpiAwareness();

        using var session = Capture.FromWindow(windowHandle);
        var config = new CaptureConfiguration
        {
            SessionIncludeCursor = includeCursor,
            SessionDrawBorder = drawBorder,
        };
        session.UpdateConfiguration(config);

        using var frame = await session.CaptureFrameAsync(cancellationToken).ConfigureAwait(false);
        return new Bitmap(frame.Bitmap);
    }

    public static Bitmap CaptureWindowByTitle(string windowTitle, bool includeCursor = false, bool drawBorder = false)
    {
        if (string.IsNullOrWhiteSpace(windowTitle))
            throw new ArgumentException("Window title cannot be null or empty.", nameof(windowTitle));

        var window = WindowEnumerator.FindWindowsByPartialTitle(windowTitle).FirstOrDefault();
        if (window == null)
            throw new WindowNotFoundException($"Window with title containing '{windowTitle}' not found.", IntPtr.Zero);

        return CaptureWindow(window.Handle, includeCursor, drawBorder);
    }
}
