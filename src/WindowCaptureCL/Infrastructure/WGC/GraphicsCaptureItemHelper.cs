using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Capture;

namespace WindowCaptureCL.Infrastructure.WGC;

/// <summary>
/// Helper class for creating GraphicsCaptureItem instances for windows and monitors.
/// </summary>
internal static class GraphicsCaptureItemHelper
{
    private static readonly Guid GraphicsCaptureItemGuid = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");

    /// <summary>
    /// Creates a GraphicsCaptureItem for a window handle.
    /// </summary>
    /// <param name="hwnd">The window handle.</param>
    /// <returns>A GraphicsCaptureItem for the window.</returns>
    /// <exception cref="CaptureSourceNotFoundException">Thrown when the window cannot be captured.</exception>
    public static GraphicsCaptureItem CreateForWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            throw new ArgumentException("Invalid window handle.", nameof(hwnd));

        try
        {
            var item = CaptureHelper.CreateItemForWindow(hwnd);

            if (item == null)
            {
                throw new CaptureSourceNotFoundException(
                    $"Failed to create capture item for window handle {hwnd}.",
                    hwnd);
            }

            return item;
        }
        catch (Exception ex) when (ex is not CaptureSourceNotFoundException)
        {
            throw new CaptureSourceNotFoundException(
                $"Failed to create capture item for window handle {hwnd}.",
                hwnd,
                ex);
        }
    }

    /// <summary>
    /// Creates a GraphicsCaptureItem for a monitor handle.
    /// </summary>
    /// <param name="hmonitor">The monitor handle.</param>
    /// <returns>A GraphicsCaptureItem for the monitor.</returns>
    /// <exception cref="CaptureSourceNotFoundException">Thrown when the monitor cannot be captured.</exception>
    public static GraphicsCaptureItem CreateForMonitor(IntPtr hmonitor)
    {
        if (hmonitor == IntPtr.Zero)
            throw new ArgumentException("Invalid monitor handle.", nameof(hmonitor));

        try
        {
            var item = CaptureHelper.CreateItemForMonitor(hmonitor);

            if (item == null)
            {
                throw new CaptureSourceNotFoundException(
                    $"Failed to create capture item for monitor handle {hmonitor}.",
                    hmonitor);
            }

            return item;
        }
        catch (Exception ex) when (ex is not CaptureSourceNotFoundException)
        {
            throw new CaptureSourceNotFoundException(
                $"Failed to create capture item for monitor handle {hmonitor}.",
                hmonitor,
                ex);
        }
    }

    /// <summary>
    /// Wrapper class for Windows.Graphics.Capture.Interop APIs.
    /// </summary>
    private static class CaptureHelper
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        private const uint MONITOR_DEFAULTTOPRIMARY = 1;

        public static GraphicsCaptureItem? CreateItemForWindow(IntPtr hwnd)
        {
            try
            {
                var interop = GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();

                var itemPointer = interop.CreateForWindow(
                    hwnd,
                    GraphicsCaptureItemGuid);

                var item = GraphicsCaptureItem.FromAbi(itemPointer);
                Marshal.Release(itemPointer);

                return item;
            }
            catch
            {
                return null;
            }
        }

        public static GraphicsCaptureItem? CreateItemForMonitor(IntPtr hmonitor)
        {
            try
            {
                var interop = GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();

                var itemPointer = interop.CreateForMonitor(
                    hmonitor,
                    GraphicsCaptureItemGuid);

                var item = GraphicsCaptureItem.FromAbi(itemPointer);
                Marshal.Release(itemPointer);

                return item;
            }
            catch
            {
                return null;
            }
        }

        [ComImport]
        [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IGraphicsCaptureItemInterop
        {
            IntPtr CreateForWindow(
                [In] IntPtr window,
                [In] Guid riid);

            IntPtr CreateForMonitor(
                [In] IntPtr monitor,
                [In] Guid riid);
        }
    }
}
