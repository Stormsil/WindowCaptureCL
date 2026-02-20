using System.Drawing;
using WindowCaptureCL.Infrastructure.WGC;

namespace WindowCaptureCL;

public static partial class CaptureFacade
{
    public static IReadOnlyList<WindowInfo> GetAvailableWindows()
    {
        var results = new List<WindowInfo>();
        var windows = WindowEnumerator.FindWindowsByPartialTitle(string.Empty);
        if (windows != null)
        {
            foreach (var w in windows)
            {
                if (w != null)
                {
                    results.Add(new WindowInfo(
                        w.Handle,
                        w.Title ?? string.Empty,
                        string.Empty,
                        new Rectangle(0, 0, w.Width, w.Height),
                        true));
                }
            }
        }

        return results;
    }

    public static int GetMonitorCount()
    {
        return WgcInterop.GetMonitorCount();
    }

    public static bool IsCaptureSupported()
    {
        return GraphicsCaptureHelper.IsSupported();
    }
}
