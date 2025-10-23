using System.Drawing;
using WindowCaptureCL.Core;
using WindowCaptureCL.Infrastructure.WGC;

namespace WindowCaptureCL;

/// <summary>
/// Static facade class providing the main entry points for screen and window capture operations.
/// </summary>
public static class Capture
{
    /// <summary>
    /// Creates a capture session for the specified window.
    /// </summary>
    /// <param name="windowHandle">The handle to the window to capture.</param>
    /// <returns>An <see cref="ICaptureSession"/> that can be used to capture frames from the window.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="windowHandle"/> is <see cref="IntPtr.Zero"/>.</exception>
    /// <exception cref="WindowNotFoundException">Thrown when the window handle is invalid or the window no longer exists.</exception>
    /// <exception cref="GraphicsCaptureNotSupportedException">Thrown when Windows Graphics Capture is not supported on the current system.</exception>
    public static ICaptureSession FromWindow(IntPtr windowHandle)
    {
        // T064: Input validation
        if (windowHandle == IntPtr.Zero)
        {
            throw new ArgumentException("Window handle cannot be IntPtr.Zero.", nameof(windowHandle));
        }

        // Verify WGC is supported
        GraphicsCaptureHelper.EnsureSupported();

        // Validate that window exists
        if (!WgcInterop.IsWindow(windowHandle))
        {
            throw new WindowNotFoundException(
                $"Window with handle {windowHandle} was not found or is no longer valid.",
                windowHandle);
        }

        // Create CaptureSourceInfo with Window type
        var sourceInfo = new CaptureSourceInfo(
            id: windowHandle,
            sourceType: CaptureSourceType.Window,
            displayName: "Window",
            width: 0,  // Will be determined by CaptureSession
            height: 0);

        // Return new CaptureSession
        return new CaptureSession(sourceInfo);
    }

    // T095-T097: Implement Capture.FromScreen() method for monitor capture
    /// <summary>
    /// Creates a capture session for a monitor.
    /// </summary>
    /// <param name="monitorIndex">The zero-based index of the monitor to capture. 0 represents the primary monitor.</param>
    /// <returns>An <see cref="ICaptureSession"/> that can be used to capture frames from the monitor.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="monitorIndex"/> is negative.</exception>
    /// <exception cref="InvalidMonitorException">Thrown when the monitor index is out of range or the monitor is not found.</exception>
    /// <exception cref="GraphicsCaptureNotSupportedException">Thrown when Windows Graphics Capture is not supported on the current system.</exception>
    public static ICaptureSession FromScreen(int monitorIndex)
    {
        // T096: Input validation
        if (monitorIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(monitorIndex), "Monitor index cannot be negative.");
        }

        // Verify WGC is supported
        GraphicsCaptureHelper.EnsureSupported();

        // Check if monitor index is valid
        int monitorCount = WgcInterop.GetMonitorCount();
        if (monitorIndex >= monitorCount)
        {
            throw new InvalidMonitorException(
                $"Monitor index {monitorIndex} is out of range. Valid range: 0-{monitorCount - 1}.",
                monitorIndex);
        }

        // T097: Create CaptureSourceInfo with Monitor type
        var bounds = WgcInterop.GetMonitorBounds(monitorIndex);
        var sourceInfo = new CaptureSourceInfo(
            id: monitorIndex,
            sourceType: CaptureSourceType.Monitor,
            displayName: $"Monitor {monitorIndex}",
            width: bounds.Width,
            height: bounds.Height);

        // Return new CaptureSession
        return new CaptureSession(sourceInfo);
    }

    // T106-T108: Implement Capture.FromScreenRegion() method for region capture
    /// <summary>
    /// Creates a capture session for a specific rectangular region on a monitor.
    /// </summary>
    /// <param name="monitorIndex">The zero-based index of the monitor.</param>
    /// <param name="region">The rectangular region to capture, relative to the monitor's top-left corner.</param>
    /// <returns>An <see cref="ICaptureSession"/> that can be used to capture frames from the specified region.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="monitorIndex"/> is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="region"/> has invalid dimensions (width or height <= 0).</exception>
    /// <exception cref="InvalidMonitorException">Thrown when the monitor index is out of range.</exception>
    /// <exception cref="RegionOutOfBoundsException">Thrown when the region extends beyond the monitor bounds.</exception>
    /// <exception cref="GraphicsCaptureNotSupportedException">Thrown when Windows Graphics Capture is not supported on the current system.</exception>
    public static ICaptureSession FromScreenRegion(int monitorIndex, Rectangle region)
    {
        // T107: Input validation
        if (monitorIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(monitorIndex), "Monitor index cannot be negative.");
        }

        if (region.Width <= 0 || region.Height <= 0)
        {
            throw new ArgumentException("Region width and height must be greater than zero.", nameof(region));
        }

        // Verify WGC is supported
        GraphicsCaptureHelper.EnsureSupported();

        // Validate monitor index
        int monitorCount = WgcInterop.GetMonitorCount();
        if (monitorIndex >= monitorCount)
        {
            throw new InvalidMonitorException(
                $"Monitor index {monitorIndex} is out of range. Valid range: 0-{monitorCount - 1}.",
                monitorIndex);
        }

        // Validate region bounds
        WgcInterop.ValidateRegion(monitorIndex, region);

        // T108: Create CaptureSourceInfo with Region type
        var sourceInfo = new CaptureSourceInfo(
            monitorIndex: monitorIndex,
            region: region,
            displayName: $"Region on Monitor {monitorIndex}");

        // Return new CaptureSession
        return new CaptureSession(sourceInfo);
    }
}
