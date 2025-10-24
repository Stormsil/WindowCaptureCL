using System.Runtime.InteropServices;

namespace WindowCaptureCL.Infrastructure.WGC;

/// <summary>
/// Provides methods for enumerating monitors on the system.
/// </summary>
internal static class MonitorEnumerator
{
    /// <summary>
    /// Gets all monitors currently connected to the system.
    /// </summary>
    /// <returns>A list of all monitors.</returns>
    public static List<MonitorInfo> GetAllMonitors()
    {
        var monitors = new List<MonitorInfo>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (hMonitor, hdcMonitor, lprcMonitor, dwData) =>
        {
            var info = GetMonitorInfo(hMonitor);
            if (info != null)
                monitors.Add(info);

            return true; // Continue enumeration
        }, IntPtr.Zero);

        return monitors;
    }

    /// <summary>
    /// Gets the primary monitor.
    /// </summary>
    /// <returns>Information about the primary monitor, or null if not found.</returns>
    public static MonitorInfo? GetPrimaryMonitor()
    {
        var monitors = GetAllMonitors();
        return monitors.FirstOrDefault(m => m.IsPrimary);
    }

    /// <summary>
    /// Gets a monitor by its index.
    /// </summary>
    /// <param name="index">The zero-based index of the monitor.</param>
    /// <returns>Information about the monitor, or null if not found.</returns>
    public static MonitorInfo? GetMonitorByIndex(int index)
    {
        if (index < 0)
            return null;

        var monitors = GetAllMonitors();
        return index < monitors.Count ? monitors[index] : null;
    }

    /// <summary>
    /// Gets a monitor by its device name.
    /// </summary>
    /// <param name="deviceName">The device name (e.g., "\\\\.\\DISPLAY1").</param>
    /// <returns>Information about the monitor, or null if not found.</returns>
    public static MonitorInfo? GetMonitorByDeviceName(string deviceName)
    {
        if (string.IsNullOrEmpty(deviceName))
            return null;

        var monitors = GetAllMonitors();
        return monitors.FirstOrDefault(m =>
            string.Equals(m.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase));
    }

    private static MonitorInfo? GetMonitorInfo(IntPtr hMonitor)
    {
        var info = new MONITORINFOEX();
        info.cbSize = Marshal.SizeOf(info);

        if (!GetMonitorInfo(hMonitor, ref info))
            return null;

        var width = info.rcMonitor.Right - info.rcMonitor.Left;
        var height = info.rcMonitor.Bottom - info.rcMonitor.Top;

        if (width <= 0 || height <= 0)
            return null;

        var isPrimary = (info.dwFlags & MONITORINFOF_PRIMARY) != 0;

        return new MonitorInfo(
            hMonitor,
            info.szDevice,
            width,
            height,
            isPrimary);
    }

    #region Win32 Interop

    private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(
        IntPtr hdc,
        IntPtr lprcClip,
        MonitorEnumDelegate lpfnEnum,
        IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    private const int CCHDEVICENAME = 32;
    private const int MONITORINFOF_PRIMARY = 1;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string szDevice;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    #endregion
}

/// <summary>
/// Represents information about a monitor.
/// </summary>
internal sealed class MonitorInfo
{
    public IntPtr Handle { get; }
    public string DeviceName { get; }
    public int Width { get; }
    public int Height { get; }
    public bool IsPrimary { get; }

    public MonitorInfo(IntPtr handle, string deviceName, int width, int height, bool isPrimary)
    {
        Handle = handle;
        DeviceName = deviceName;
        Width = width;
        Height = height;
        IsPrimary = isPrimary;
    }
}
