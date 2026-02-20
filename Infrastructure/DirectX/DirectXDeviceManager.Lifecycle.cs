namespace WindowCaptureCL.Infrastructure.DirectX;

internal sealed partial class DirectXDeviceManager
{
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            _dxgiDevice?.Dispose();
            _dxgiDevice = null;

            _deviceContext?.Dispose();
            _deviceContext = null;

            _device?.Dispose();
            _device = null;

            _disposed = true;
        }
    }
}
