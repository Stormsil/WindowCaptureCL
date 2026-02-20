using Vortice.Direct3D11;
using Vortice.DXGI;

namespace WindowCaptureCL.Infrastructure.DirectX;

internal sealed partial class DirectXDeviceManager : IDisposable
{
    private static readonly Lazy<DirectXDeviceManager> _instance = new(() => new DirectXDeviceManager());
    private readonly object _lock = new();
    private bool _disposed;

    private ID3D11Device? _device;
    private ID3D11DeviceContext? _deviceContext;
    private IDXGIDevice? _dxgiDevice;

    public static DirectXDeviceManager Instance => _instance.Value;

    public ID3D11Device Device
    {
        get
        {
            lock (_lock)
            {
                EnsureNotDisposed();
                if (_device == null)
                    InitializeDevice();

                return _device!;
            }
        }
    }

    public ID3D11DeviceContext DeviceContext
    {
        get
        {
            lock (_lock)
            {
                EnsureNotDisposed();
                if (_deviceContext == null)
                    InitializeDevice();

                return _deviceContext!;
            }
        }
    }

    public IDXGIDevice DxgiDevice
    {
        get
        {
            lock (_lock)
            {
                EnsureNotDisposed();
                if (_dxgiDevice == null)
                    InitializeDevice();

                return _dxgiDevice!;
            }
        }
    }

    private DirectXDeviceManager()
    {
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DirectXDeviceManager));
    }
}
