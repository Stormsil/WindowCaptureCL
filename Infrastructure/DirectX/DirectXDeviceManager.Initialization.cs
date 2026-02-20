using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace WindowCaptureCL.Infrastructure.DirectX;

internal sealed partial class DirectXDeviceManager
{
    private void InitializeDevice()
    {
        try
        {
            var creationFlags = DeviceCreationFlags.BgraSupport;

            var featureLevels = new[]
            {
                FeatureLevel.Level_11_1,
                FeatureLevel.Level_11_0,
                FeatureLevel.Level_10_1,
                FeatureLevel.Level_10_0
            };

            var result = D3D11.D3D11CreateDevice(
                null,
                DriverType.Hardware,
                creationFlags,
                featureLevels,
                out _device,
                out _deviceContext);

            if (result.Failure)
            {
                throw new DirectXException(
                    $"Failed to create Direct3D11 device. HRESULT: 0x{result.Code:X8}",
                    result.Code);
            }

            try
            {
                _dxgiDevice = _device!.QueryInterface<IDXGIDevice3>() ?? _device.QueryInterface<IDXGIDevice>();
            }
            catch
            {
                _dxgiDevice = _device!.QueryInterface<IDXGIDevice>();
            }

            if (_dxgiDevice == null)
            {
                throw new DirectXException("Failed to obtain DXGI device interface.");
            }
        }
        catch (Exception ex) when (ex is not DirectXException)
        {
            throw new DirectXException("Failed to initialize DirectX device.", ex);
        }
    }
}
