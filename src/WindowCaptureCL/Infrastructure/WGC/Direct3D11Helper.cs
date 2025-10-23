using System.Runtime.InteropServices;
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
        // Get the native pointer from the DXGI device
        var dxgiDevicePtr = Marshal.GetIUnknownForObject(dxgiDevice);

        try
        {
            // Create the WinRT wrapper
            var hr = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevicePtr, out var inspectablePtr);

            if (hr != 0)
            {
                throw new DirectXException(
                    $"Failed to create Direct3D11 device from DXGI device. HRESULT: 0x{hr:X8}",
                    (int)hr);
            }

            if (inspectablePtr == IntPtr.Zero)
            {
                throw new DirectXException("CreateDirect3D11DeviceFromDXGIDevice returned null pointer.");
            }

            // Query for IDirect3DDevice interface
            var direct3DDeviceGuid = typeof(IDirect3DDevice).GUID;
            var queryHr = Marshal.QueryInterface(inspectablePtr, ref direct3DDeviceGuid, out var devicePtr);

            // Release the inspectable pointer as we now have the device pointer
            Marshal.Release(inspectablePtr);

            if (queryHr != 0 || devicePtr == IntPtr.Zero)
            {
                throw new DirectXException(
                    $"Failed to query IDirect3DDevice interface. HRESULT: 0x{queryHr:X8}",
                    queryHr);
            }

            try
            {
                // Now marshal using FromAbi with the correct interface pointer
                var device = MarshalInterface<IDirect3DDevice>.FromAbi(devicePtr);

                if (device == null)
                {
                    throw new DirectXException("MarshalInterface.FromAbi returned null.");
                }

                return device;
            }
            finally
            {
                // FromAbi takes ownership, but we should still release our reference
                if (devicePtr != IntPtr.Zero)
                    Marshal.Release(devicePtr);
            }
        }
        finally
        {
            if (dxgiDevicePtr != IntPtr.Zero)
                Marshal.Release(dxgiDevicePtr);
        }
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
            // Get the native interface pointer from the WinRT object
            var surfacePtr = Marshal.GetIUnknownForObject(surface);

            try
            {
                // Query for IDirect3DDxgiInterfaceAccess interface
                var hr = Marshal.QueryInterface(surfacePtr, ref IDirect3DDxgiInterfaceAccessGuid, out var accessPtr);

                if (hr != 0)
                {
                    throw new DirectXException(
                        $"Failed to query IDirect3DDxgiInterfaceAccess. HRESULT: 0x{hr:X8}",
                        hr);
                }

                try
                {
                    // Get the DXGI interface
                    var access = Marshal.GetObjectForIUnknown(accessPtr) as IDirect3DDxgiInterfaceAccess;

                    if (access == null)
                    {
                        throw new DirectXException("Failed to get IDirect3DDxgiInterfaceAccess interface.");
                    }

                    // Get the native texture
                    var texturePtr = access.GetInterface(ref ID3D11Texture2DGuid);

                    var texture = Marshal.GetObjectForIUnknown(texturePtr) as ID3D11Texture2D;

                    if (texture == null)
                    {
                        throw new DirectXException("Failed to get ID3D11Texture2D from surface.");
                    }

                    return texture;
                }
                finally
                {
                    if (accessPtr != IntPtr.Zero)
                        Marshal.Release(accessPtr);
                }
            }
            finally
            {
                if (surfacePtr != IntPtr.Zero)
                    Marshal.Release(surfacePtr);
            }
        }
        catch (Exception ex) when (ex is not DirectXException)
        {
            throw new DirectXException("Failed to extract D3D11 texture from surface.", ex);
        }
    }

    private static Guid IDirect3DDxgiInterfaceAccessGuid = new("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1");
    private static Guid ID3D11Texture2DGuid = new("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

    [DllImport("d3d11.dll", EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern uint CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

    [ComImport]
    [Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDirect3DDxgiInterfaceAccess
    {
        IntPtr GetInterface([In] ref Guid iid);
    }
}
