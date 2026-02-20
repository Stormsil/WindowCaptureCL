using WindowCaptureCL.Infrastructure;

namespace WindowCaptureCL;

public static partial class CaptureFacade
{
    public static void SetDefaultIncludeCursor(bool includeCursor)
    {
        CaptureConfiguration.IncludeCursor = includeCursor;
    }

    public static void SetDefaultDrawBorder(bool drawBorder)
    {
        CaptureConfiguration.DrawBorder = drawBorder;
    }

    public static void SetDefaultTargetFPS(int fps)
    {
        CaptureConfiguration.DefaultTargetFPS = fps;
    }

    public static void EnsureDpiAwareness()
    {
        DpiHelper.SetPerMonitorDpiAwareness();
    }
}
