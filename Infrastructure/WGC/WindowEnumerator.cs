using System.Text;

namespace WindowCaptureCL.Infrastructure.WGC;

/// <summary>
/// Provides methods for enumerating and finding windows on the system.
/// </summary>
public static partial class WindowEnumerator
{
    /// <summary>
    /// Finds a window by its handle (HWND).
    /// </summary>
    /// <param name="hwnd">The window handle.</param>
    /// <returns>Information about the window, or null if not found.</returns>
    public static WindowInfo? FindWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return null;

        if (!IsWindow(hwnd))
            return null;

        var title = GetWindowTitle(hwnd);
        if (string.IsNullOrEmpty(title))
            return null;

        // Get window dimensions
        if (!GetWindowRect(hwnd, out var rect))
            return null;

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;

        if (width <= 0 || height <= 0)
            return null;

        return new WindowInfo(hwnd, title, width, height);
    }

    /// <summary>
    /// Finds a window by its process ID.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    /// <returns>Information about the first window found for the process, or null if not found.</returns>
    public static WindowInfo? FindWindowByProcessId(int processId)
    {
        WindowInfo? result = null;

        EnumWindows((hwnd, lParam) =>
        {
            GetWindowThreadProcessId(hwnd, out var windowProcessId);

            if (windowProcessId == processId)
            {
                result = FindWindow(hwnd);
                return result == null; // Continue if we didn't get valid info
            }

            return true; // Continue enumeration
        }, IntPtr.Zero);

        return result;
    }

    /// <summary>
    /// Finds a window by its title (exact match).
    /// </summary>
    /// <param name="title">The exact window title.</param>
    /// <returns>Information about the window, or null if not found.</returns>
    public static WindowInfo? FindWindowByTitle(string title)
    {
        if (string.IsNullOrEmpty(title))
            return null;

        WindowInfo? result = null;

        EnumWindows((hwnd, lParam) =>
        {
            var windowTitle = GetWindowTitle(hwnd);

            if (string.Equals(windowTitle, title, StringComparison.Ordinal))
            {
                result = FindWindow(hwnd);
                return result == null; // Continue if we didn't get valid info
            }

            return true; // Continue enumeration
        }, IntPtr.Zero);

        return result;
    }

    /// <summary>
    /// Finds windows whose title contains the specified text.
    /// </summary>
    /// <param name="partialTitle">The text to search for in window titles.</param>
    /// <returns>A list of matching windows.</returns>
    public static List<WindowInfo> FindWindowsByPartialTitle(string partialTitle)
    {
        if (string.IsNullOrEmpty(partialTitle))
            return new List<WindowInfo>();

        var results = new List<WindowInfo>();

        EnumWindows((hwnd, lParam) =>
        {
            var windowTitle = GetWindowTitle(hwnd);

            if (!string.IsNullOrEmpty(windowTitle) &&
                windowTitle.Contains(partialTitle, StringComparison.OrdinalIgnoreCase))
            {
                var info = FindWindow(hwnd);
                if (info != null)
                    results.Add(info);
            }

            return true; // Continue enumeration
        }, IntPtr.Zero);

        return results;
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
        var length = GetWindowTextLength(hwnd);
        if (length == 0)
            return string.Empty;

        var builder = new StringBuilder(length + 1);
        GetWindowText(hwnd, builder, builder.Capacity);
        return builder.ToString();
    }
}
