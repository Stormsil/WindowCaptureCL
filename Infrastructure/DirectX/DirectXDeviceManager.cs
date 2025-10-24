using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace WindowCaptureCL.Infrastructure.DirectX;

/// <summary>
/// Manages the lifecycle of DirectX devices required for screen capture.
/// Thread-safe singleton implementation.
/// </summary>
internal sealed class DirectXDeviceManager : IDisposable
{
    private static readonly Lazy<DirectXDeviceManager> _instance = new(() => new DirectXDeviceManager());
    private readonly object _lock = new();
    private bool _disposed;

    private ID3D11Device? _device;
    private ID3D11DeviceContext? _deviceContext;
    private IDXGIDevice? _dxgiDevice;

    /// <summary>
    /// Gets the singleton instance of the <see cref="DirectXDeviceManager"/>.
    /// </summary>
    public static DirectXDeviceManager Instance => _instance.Value;

    /// <summary>
    /// Gets the Direct3D11 device.
    /// </summary>
    public ID3D11Device Device
    {
        get
        {
            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(DirectXDeviceManager));

                if (_device == null)
                    InitializeDevice();

                return _device!;
            }
        }
    }

    /// <summary>
    /// Gets the Direct3D11 device context.
    /// </summary>
    public ID3D11DeviceContext DeviceContext
    {
        get
        {
            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(DirectXDeviceManager));

                if (_deviceContext == null)
                    InitializeDevice();

                return _deviceContext!;
            }
        }
    }

    /// <summary>
    /// Gets the DXGI device interface.
    /// </summary>
    public IDXGIDevice DxgiDevice
    {
        get
        {
            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(DirectXDeviceManager));

                if (_dxgiDevice == null)
                    InitializeDevice();

                return _dxgiDevice!;
            }
        }
    }

    private DirectXDeviceManager()
    {
    }

    private void InitializeDevice()
    {
        try
        {
            // Create D3D11 device with BGRA support for Windows Graphics Capture
            // Note: Never use Debug flag in production as it requires Graphics Tools to be installed
            var creationFlags = DeviceCreationFlags.BgraSupport;

            var featureLevels = new[]
            {
                FeatureLevel.Level_11_1,
                FeatureLevel.Level_11_0,
                FeatureLevel.Level_10_1,
                FeatureLevel.Level_10_0
            };

            var result = D3D11.D3D11CreateDevice(
                null, // Use default adapter
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

            // Get DXGI device interface
            // Note: We try to get IDXGIDevice3 first (required for Windows Graphics Capture on modern Windows)
            // If that fails, fall back to IDXGIDevice
            try
            {
                _dxgiDevice = _device!.QueryInterface<IDXGIDevice3>() ?? _device!.QueryInterface<IDXGIDevice>();
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

    /// <summary>
    /// Creates a staging texture for CPU readback of captured frames.
    /// </summary>
    /// <param name="width">The width of the texture.</param>
    /// <param name="height">The height of the texture.</param>
    /// <returns>A staging texture with CPU read access.</returns>
    public ID3D11Texture2D CreateStagingTexture(int width, int height)
    {
        lock (_lock)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DirectXDeviceManager));

            var desc = new Texture2DDescription
            {
                Width = (uint)width,
                Height = (uint)height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CPUAccessFlags = CpuAccessFlags.Read,
                MiscFlags = ResourceOptionFlags.None
            };

            try
            {
                var texture = Device.CreateTexture2D(desc);
                if (texture == null)
                {
                    throw new ResourceAllocationException(
                        $"Failed to create staging texture ({width}x{height}).",
                        "StagingTexture");
                }

                return texture;
            }
            catch (Exception ex) when (ex is not ResourceAllocationException)
            {
                throw new ResourceAllocationException(
                    $"Failed to create staging texture ({width}x{height}).",
                    "StagingTexture",
                    ex);
            }
        }
    }

    /// <summary>
    /// Releases all resources used by the DirectX device manager.
    /// </summary>
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
