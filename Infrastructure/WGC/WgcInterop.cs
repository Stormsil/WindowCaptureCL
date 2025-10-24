using System.Drawing;
using System.Runtime.InteropServices;

namespace WindowCaptureCL.Infrastructure.WGC;

/// <summary>
/// Static class providing P/Invoke methods and helpers for Windows Graphics Capture API.
/// </summary>
internal static class WgcInterop
{
    /// <summary>
    /// Retrieves the dimensions of the bounding rectangle of the specified window.
    /// </summary>
    /// <param name="hWnd">A handle to the window.</param>
    /// <param name="lpRect">A pointer to a RECT structure that receives the screen coordinates of the upper-left and lower-right corners of the window.</param>
    /// <returns>If the function succeeds, the return value is true. If the function fails, the return value is false.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    /// <summary>
    /// Determines whether the specified window handle identifies an existing window.
    /// </summary>
    /// <param name="hWnd">A handle to the window to be tested.</param>
    /// <returns>If the window handle identifies an existing window, the return value is true. Otherwise, it returns false.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindow(IntPtr hWnd);

    /// <summary>
    /// Determines whether the specified window is visible.
    /// </summary>
    /// <param name="hWnd">A handle to the window to be tested.</param>
    /// <returns>If the window is visible, the return value is true. Otherwise, it returns false.</returns>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    /// <summary>
    /// Determines whether the specified window is minimized (iconic).
    /// </summary>
    /// <param name="hWnd">A handle to the window to be tested.</param>
    /// <returns>If the window is minimized, the return value is true. Otherwise, it returns false.</returns>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    /// <summary>
    /// Represents a rectangle defined by the coordinates of its upper-left and lower-right corners.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        /// <summary>
        /// Gets the width of the rectangle.
        /// </summary>
        public int Width => Right - Left;

        /// <summary>
        /// Gets the height of the rectangle.
        /// </summary>
        public int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    // T090: GetMonitorCount() method
    /// <summary>
    /// Gets the number of monitors connected to the system.
    /// </summary>
    public static int GetMonitorCount()
    {
        return System.Windows.Forms.Screen.AllScreens.Length;
    }

    // T091: GetMonitorBounds() method
    /// <summary>
    /// Gets the bounds of the monitor at the specified index.
    /// </summary>
    /// <param name="monitorIndex">The zero-based index of the monitor.</param>
    /// <returns>A Rectangle containing the monitor's bounds.</returns>
    public static Rectangle GetMonitorBounds(int monitorIndex)
    {
        var screens = System.Windows.Forms.Screen.AllScreens;
        if (monitorIndex < 0 || monitorIndex >= screens.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(monitorIndex));
        }

        var screen = screens[monitorIndex];
        return new Rectangle(screen.Bounds.X, screen.Bounds.Y, screen.Bounds.Width, screen.Bounds.Height);
    }

    // T092: GetMonitorHandle() method
    /// <summary>
    /// Gets the HMONITOR handle for the monitor at the specified index.
    /// </summary>
    /// <param name="monitorIndex">The zero-based index of the monitor.</param>
    /// <returns>The HMONITOR handle.</returns>
    public static IntPtr GetMonitorHandle(int monitorIndex)
    {
        var screens = System.Windows.Forms.Screen.AllScreens;
        if (monitorIndex < 0 || monitorIndex >= screens.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(monitorIndex));
        }

        var screen = screens[monitorIndex];
        var point = new POINT
        {
            x = screen.Bounds.Left + screen.Bounds.Width / 2,
            y = screen.Bounds.Top + screen.Bounds.Height / 2
        };

        return MonitorFromPoint(point, MONITOR_DEFAULTTONEAREST);
    }

    // T102: ValidateRegion() method for region bounds checking
    /// <summary>
    /// Validates that a rectangular region is within the bounds of the specified monitor.
    /// </summary>
    /// <param name="monitorIndex">The zero-based index of the monitor.</param>
    /// <param name="region">The region to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when monitorIndex is invalid.</exception>
    /// <exception cref="RegionOutOfBoundsException">Thrown when the region extends beyond the monitor bounds.</exception>
    public static void ValidateRegion(int monitorIndex, Rectangle region)
    {
        var screens = System.Windows.Forms.Screen.AllScreens;
        if (monitorIndex < 0 || monitorIndex >= screens.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(monitorIndex));
        }

        var monitorBounds = GetMonitorBounds(monitorIndex);

        // Check if region is within monitor bounds
        bool isValid = region.X >= 0 &&
                      region.Y >= 0 &&
                      region.Width > 0 &&
                      region.Height > 0 &&
                      region.X + region.Width <= monitorBounds.Width &&
                      region.Y + region.Height <= monitorBounds.Height;

        if (!isValid)
        {
            throw new RegionOutOfBoundsException(
                $"Region {region} extends beyond monitor {monitorIndex} bounds ({monitorBounds.Width}x{monitorBounds.Height}).",
                region,
                new Size(monitorBounds.Width, monitorBounds.Height));
        }
    }
}
