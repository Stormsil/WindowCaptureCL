namespace WindowCaptureCL.Infrastructure;

public static partial class DpiHelper
{
    public static int GetDpiForMonitor(IntPtr hMonitor)
    {
        try
        {
            int dpiX;
            int dpiY;
            var result = GetDpiForMonitor(hMonitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
            if (result == 0)
            {
                return dpiX;
            }
        }
        catch
        {
            // Function not available on older Windows versions.
        }

        return DefaultDpi;
    }

    public static int GetPrimaryMonitorDpi()
    {
        try
        {
            var hMonitor = MonitorFromPoint(new POINT { x = 0, y = 0 }, MONITOR_DEFAULTTOPRIMARY);
            return GetDpiForMonitor(hMonitor);
        }
        catch
        {
            return DefaultDpi;
        }
    }

    public static int GetDpiForWindow(IntPtr hWnd)
    {
        try
        {
            return (int)GetDpiForWindowInternal(hWnd);
        }
        catch
        {
            return GetPrimaryMonitorDpi();
        }
    }

    public static int DipToPixels(int dipValue, int dpi)
    {
        return (int)(dipValue * dpi / (float)DefaultDpi);
    }

    public static int PixelsToDip(int pixelValue, int dpi)
    {
        return (int)(pixelValue * DefaultDpi / (float)dpi);
    }

    public static (int Width, int Height) ScaleSize(int width, int height, int sourceDpi, int targetDpi)
    {
        var scaleFactor = targetDpi / (float)sourceDpi;
        return ((int)(width * scaleFactor), (int)(height * scaleFactor));
    }
}
