using System.Drawing;
using Vortice.Direct3D11;
using WindowCaptureCL.Infrastructure.DirectX;

namespace WindowCaptureCL.Core;

internal sealed partial class CaptureSession
{
    public CapturedFrame CaptureFrame()
    {
        return CaptureFrameAsync().GetAwaiter().GetResult();
    }

    public async Task<CapturedFrame> CaptureFrameAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_graphicsCaptureItem == null)
        {
            InitializeWGCSession();
        }

        if (_framePool == null)
        {
            CreateFramePool();
        }

        lock (_lock)
        {
            _singleFrameCapture = new TaskCompletionSource<ID3D11Texture2D>();
        }

        try
        {
            _captureSession?.StartCapture();

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            var completedTask = await Task.WhenAny(_singleFrameCapture.Task, timeoutTask).ConfigureAwait(false);

            if (completedTask == timeoutTask)
            {
                throw new FrameCaptureException("Timeout waiting for frame capture.");
            }

            var texture = await _singleFrameCapture.Task.ConfigureAwait(false);
            var bitmap = CopyTextureToBitmap(texture);
            var capturedFrame = new CapturedFrame(bitmap, DateTime.Now);

            lock (_lock)
            {
                _captureSession?.Dispose();
                _framePool?.Dispose();
                _framePool = null;
                _captureSession = null;
            }

            return capturedFrame;
        }
        catch (Exception ex) when (ex is not CaptureException)
        {
            throw new FrameCaptureException("Failed to capture frame.", ex);
        }
        finally
        {
            lock (_lock)
            {
                _singleFrameCapture = null;
            }
        }
    }

    private Bitmap CopyTextureToBitmap(ID3D11Texture2D sourceTexture)
    {
        var device = DirectXDeviceManager.Instance.Device;
        var deviceContext = DirectXDeviceManager.Instance.DeviceContext;

        if (_captureSource.SourceType == CaptureSourceType.Region)
        {
            return FrameProcessor.ConvertTextureToBitmap(device, deviceContext, sourceTexture, _captureSource.Region);
        }

        return FrameProcessor.ConvertTextureToBitmap(device, deviceContext, sourceTexture);
    }
}
