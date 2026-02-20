using System.Drawing;
using WindowCaptureCL.Infrastructure;

namespace WindowCaptureCL;

public static partial class CaptureFacade
{
    public static Bitmap CaptureScreen(bool includeCursor = false)
    {
        return CaptureMonitor(0, includeCursor);
    }

    public static Bitmap CaptureMonitor(int monitorIndex, bool includeCursor = false)
    {
        if (monitorIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(monitorIndex), "Monitor index cannot be negative.");

        DpiHelper.SetPerMonitorDpiAwareness();

        using var session = Capture.FromScreen(monitorIndex);
        var config = new CaptureConfiguration
        {
            SessionIncludeCursor = includeCursor,
        };
        session.UpdateConfiguration(config);

        using var frame = session.CaptureFrame();
        return new Bitmap(frame.Bitmap);
    }

    public static Bitmap CaptureRegion(int x, int y, int width, int height, bool includeCursor = false)
    {
        return CaptureRegion(0, x, y, width, height, includeCursor);
    }

    public static Bitmap CaptureRegion(int monitorIndex, int x, int y, int width, int height, bool includeCursor = false)
    {
        if (monitorIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(monitorIndex), "Monitor index cannot be negative.");
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");

        DpiHelper.SetPerMonitorDpiAwareness();

        var region = new Rectangle(x, y, width, height);
        using var session = Capture.FromScreenRegion(monitorIndex, region);
        var config = new CaptureConfiguration
        {
            SessionIncludeCursor = includeCursor,
        };
        session.UpdateConfiguration(config);

        using var frame = session.CaptureFrame();
        return new Bitmap(frame.Bitmap);
    }
}
