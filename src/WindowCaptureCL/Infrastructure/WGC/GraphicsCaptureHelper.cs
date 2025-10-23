using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;

namespace WindowCaptureCL.Infrastructure.WGC;

/// <summary>
/// Helper class for working with Windows Graphics Capture API.
/// </summary>
internal static class GraphicsCaptureHelper
{
    private static bool? _isSupported;

    /// <summary>
    /// Checks if Windows Graphics Capture API is supported on the current system.
    /// </summary>
    /// <returns>True if WGC is supported, false otherwise.</returns>
    public static bool IsSupported()
    {
        if (_isSupported.HasValue)
            return _isSupported.Value;

        try
        {
            // Try to access the GraphicsCaptureSession type
            // This will fail on systems that don't support WGC (pre-Windows 10 1803)
            _isSupported = GraphicsCaptureSession.IsSupported();
        }
        catch
        {
            _isSupported = false;
        }

        return _isSupported.Value;
    }

    /// <summary>
    /// Ensures that Windows Graphics Capture API is supported, throwing an exception if not.
    /// </summary>
    /// <exception cref="GraphicsCaptureNotSupportedException">Thrown when WGC is not supported.</exception>
    public static void EnsureSupported()
    {
        if (!IsSupported())
        {
            throw new GraphicsCaptureNotSupportedException(
                "Windows Graphics Capture API is not supported on this system. " +
                "Requires Windows 10 version 1803 (April 2018 Update) or later.");
        }
    }

    /// <summary>
    /// Creates a Direct3D11 device wrapper for use with Windows Graphics Capture.
    /// </summary>
    /// <param name="dxgiDevice">The DXGI device to wrap.</param>
    /// <returns>An IDirect3DDevice instance.</returns>
    public static IDirect3DDevice CreateDirect3DDevice(Vortice.DXGI.IDXGIDevice dxgiDevice)
    {
        if (dxgiDevice == null)
            throw new ArgumentNullException(nameof(dxgiDevice));

        try
        {
            // Create the Direct3D11 device using WinRT interop
            var device = Direct3D11Helper.CreateDevice(dxgiDevice);
            if (device == null)
            {
                throw new DirectXException("Failed to create Direct3D11 device for Windows Graphics Capture.");
            }

            return device;
        }
        catch (Exception ex) when (ex is not DirectXException)
        {
            throw new DirectXException("Failed to create Direct3D11 device for Windows Graphics Capture.", ex);
        }
    }
}
