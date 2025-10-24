using System.Runtime.InteropServices;
using System.Reflection;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;
using WinRT.Interop;

namespace WindowCaptureCL.Infrastructure.WGC;

/// <summary>
/// Helper class for creating WinRT Direct3D11 devices from native DXGI devices.
/// Uses COM interop to bridge between native DirectX and WinRT APIs.
/// </summary>
internal static class Direct3D11Helper
{
    private static readonly Guid IInspectable = new("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90");
    private static readonly Guid ID3D11Device = new("db6f6ddb-ac77-4e88-8253-819df9bbf140");
    private static readonly Guid IDirect3DDevice = new("A37624AB-8D5F-4650-9D3E-9EAE3D9BC670");

    /// <summary>
    /// Creates a WinRT IDirect3DDevice from a native DXGI device.
    /// </summary>
    /// <param name="dxgiDevice">The DXGI device to wrap.</param>
    /// <returns>A WinRT IDirect3DDevice instance.</returns>
    public static IDirect3DDevice CreateDevice(IDXGIDevice dxgiDevice)
    {
        if (dxgiDevice == null)
            throw new ArgumentNullException(nameof(dxgiDevice));

        try
        {
            // Try primary method using Windows Graphics Capture interop
            try
            {
                return CreateDevicePrimary(dxgiDevice);
            }
            catch (DirectXException ex) when (ex.HResult == unchecked((int)0x887A002D)) // DXGI_ERROR_NOT_CURRENTLY_AVAILABLE
            {
                // Primary method failed with NOT_CURRENTLY_AVAILABLE
                // This often means the function is not available in current context
                throw new DirectXException(
                    "Windows Graphics Capture is not available. This may occur when:\n" +
                    "  1. Running in a Remote Desktop session (RDP/VNC/NoMachine)\n" +
                    "  2. Running in a Virtual Machine without GPU passthrough\n" +
                    "  3. Graphics drivers are outdated or incompatible\n" +
                    "  4. Windows Graphics Capture API is not properly initialized\n\n" +
                    "Please ensure you're running on a physical machine with direct GPU access and updated drivers.\n" +
                    $"HRESULT: 0x{(ex.HResult.HasValue ? ex.HResult.Value : 0):X8}",
                    ex.HResult ?? 0);
            }
        }
        catch (Exception ex) when (ex is not DirectXException)
        {
            throw new DirectXException("Failed to create WinRT Direct3D11 device.", ex);
        }
    }

    private static IDirect3DDevice CreateDevicePrimary(IDXGIDevice dxgiDevice)
    {
        // Use the DXGI device's native pointer directly
        var dxgiDevicePtr = dxgiDevice.NativePointer;

        // Call the d3d11.dll function directly
        int hr = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevicePtr, out var graphicsDevicePtr);

        if (hr != 0 || graphicsDevicePtr == IntPtr.Zero)
        {
            throw new DirectXException(
                $"Failed to create Direct3D11 device from DXGI device. HRESULT: 0x{hr:X8}",
                hr);
        }

        // Use WinRT.MarshalInterface to convert to IDirect3DDevice
        var device = MarshalInterface<IDirect3DDevice>.FromAbi(graphicsDevicePtr);

        if (device == null)
        {
            throw new DirectXException("Failed to marshal IDirect3DDevice from IInspectable.");
        }

        return device;
    }


    /// <summary>
    /// Extracts a native ID3D11Texture2D from a WinRT IDirect3DSurface.
    /// </summary>
    /// <param name="surface">The WinRT surface.</param>
    /// <returns>The native D3D11 texture.</returns>
    public static ID3D11Texture2D GetD3D11Texture2DFromSurface(IDirect3DSurface surface)
    {
        if (surface == null)
            throw new ArgumentNullException(nameof(surface));

        try
        {
            // Use WinRT interop to get the native pointer correctly
            // This is the proper way for .NET 6+ with WinRT
            var surfaceInterop = surface.As<IDirect3DDxgiInterfaceAccess>();

            // Get the native D3D11Texture2D pointer
            var texturePtr = surfaceInterop.GetInterface(ID3D11Texture2DGuid);

            if (texturePtr == IntPtr.Zero)
            {
                throw new DirectXException("Failed to get ID3D11Texture2D pointer from surface.");
            }

            // Create Vortice wrapper from native pointer
            var texture = new ID3D11Texture2D(texturePtr);

            return texture;
        }
        catch (Exception ex) when (ex is not DirectXException)
        {
            throw new DirectXException("Failed to extract D3D11 texture from surface.", ex);
        }
    }

    private static Guid IDirect3DDxgiInterfaceAccessGuid = new("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1");
    private static Guid ID3D11Texture2DGuid = new("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

    // Direct import from d3d11.dll - the correct way for .NET 6+
    [DllImport("d3d11.dll", SetLastError = true, ExactSpelling = true)]
    private static extern int CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

    [ComImport]
    [Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDirect3DDxgiInterfaceAccess
    {
        IntPtr GetInterface([In] ref Guid iid);
    }
}
