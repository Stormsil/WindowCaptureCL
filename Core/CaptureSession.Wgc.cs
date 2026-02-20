using System.Drawing;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Vortice.Direct3D11;
using WindowCaptureCL.Infrastructure.DirectX;
using WindowCaptureCL.Infrastructure.WGC;

namespace WindowCaptureCL.Core;

internal sealed partial class CaptureSession
{
    private void InitializeWGCSession()
    {
        lock (_lock)
        {
            if (_graphicsCaptureItem != null)
                return;

            try
            {
                if (_captureSource.SourceType == CaptureSourceType.Window)
                {
                    if (!WgcInterop.IsWindow(_captureSource.WindowHandle))
                    {
                        throw new WindowNotFoundException(
                            $"Window with handle {_captureSource.WindowHandle} not found or is invalid.",
                            _captureSource.WindowHandle);
                    }

                    if (!WgcInterop.IsWindowVisible(_captureSource.WindowHandle))
                    {
                        throw new GraphicsDeviceException(
                            $"Window with handle {_captureSource.WindowHandle} is not visible. " +
                            "Windows Graphics Capture API can only capture visible windows. " +
                            "Please ensure the window is not hidden or on a different desktop.");
                    }

                    if (WgcInterop.IsIconic(_captureSource.WindowHandle))
                    {
                        throw new GraphicsDeviceException(
                            $"Window with handle {_captureSource.WindowHandle} is minimized. " +
                            "Windows Graphics Capture API cannot capture minimized windows. " +
                            "Please restore the window before capturing.");
                    }

                    _graphicsCaptureItem = GraphicsCaptureItemHelper.CreateForWindow(_captureSource.WindowHandle);
                }
                else if (_captureSource.SourceType == CaptureSourceType.Monitor)
                {
                    int monitorCount = WgcInterop.GetMonitorCount();
                    if (_captureSource.MonitorIndex < 0 || _captureSource.MonitorIndex >= monitorCount)
                    {
                        throw new InvalidMonitorException(
                            $"Monitor index {_captureSource.MonitorIndex} is out of range. Valid range: 0-{monitorCount - 1}.",
                            _captureSource.MonitorIndex);
                    }

                    IntPtr hMonitor = WgcInterop.GetMonitorHandle(_captureSource.MonitorIndex);
                    _graphicsCaptureItem = GraphicsCaptureItemHelper.CreateForMonitor(hMonitor);
                }
                else if (_captureSource.SourceType == CaptureSourceType.Region)
                {
                    WgcInterop.ValidateRegion(_captureSource.MonitorIndex, _captureSource.Region);
                    IntPtr hMonitor = WgcInterop.GetMonitorHandle(_captureSource.MonitorIndex);
                    _graphicsCaptureItem = GraphicsCaptureItemHelper.CreateForMonitor(hMonitor);
                }
                else
                {
                    throw new UnsupportedOperationException(
                        $"Capture source type {_captureSource.SourceType} is not supported in this version.");
                }
            }
            catch (Exception ex) when (ex is not CaptureException)
            {
                throw new GraphicsDeviceException(
                    "Failed to initialize Windows Graphics Capture session.",
                    ex);
            }
        }
    }

    private void CreateFramePool()
    {
        lock (_lock)
        {
            if (_graphicsCaptureItem == null)
                throw new InvalidOperationException("GraphicsCaptureItem must be initialized before creating frame pool.");

            try
            {
                var dxgiDevice = DirectXDeviceManager.Instance.DxgiDevice;

                IDirect3DDevice? direct3DDevice;
                try
                {
                    direct3DDevice = GraphicsCaptureHelper.CreateDirect3DDevice(dxgiDevice);
                }
                catch (Exception ex)
                {
                    throw new GraphicsDeviceException(
                        "Failed to create Direct3D device for WinRT. This may indicate GPU driver issues or unsupported hardware.",
                        ex);
                }

                var itemSize = _graphicsCaptureItem.Size;
                if (itemSize.Width <= 0 || itemSize.Height <= 0)
                {
                    throw new GraphicsDeviceException(
                        $"Capture target has invalid size: {itemSize.Width}x{itemSize.Height}. " +
                        "For windows, ensure the window is visible and not minimized before capturing.");
                }

                _framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                    direct3DDevice,
                    Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    2,
                    itemSize);

                _framePool.FrameArrived += OnFrameArrived;
                _captureSession = _framePool.CreateCaptureSession(_graphicsCaptureItem);
                _captureSession.IsCursorCaptureEnabled = _configuration.SessionIncludeCursor;
            }
            catch (GraphicsDeviceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new GraphicsDeviceException(
                    $"Failed to create capture frame pool. Error: {ex.Message}",
                    ex);
            }
        }
    }

    private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        try
        {
            using var frame = sender.TryGetNextFrame();
            if (frame == null)
            {
                return;
            }

            var surfaceTexture = Direct3D11Helper.GetD3D11Texture2DFromSurface(frame.Surface);

            if (_singleFrameCapture != null && !_singleFrameCapture.Task.IsCompleted)
            {
                _singleFrameCapture.TrySetResult(surfaceTexture);
                return;
            }

            if (!IsRunning)
                return;

            if (_captureSource.SourceType == CaptureSourceType.Window &&
                !WgcInterop.IsWindow(_captureSource.WindowHandle))
            {
                StopSessionInternal(CaptureStopReason.SourceClosed);
                return;
            }

            var currentTimestamp = _stopwatch.ElapsedMilliseconds;
            var targetInterval = 1000.0 / TargetFPS;
            if (currentTimestamp - _lastFrameTimestamp < targetInterval)
            {
                return;
            }

            var bitmap = CopyTextureToBitmap(surfaceTexture);
            var timestamp = DateTime.Now;

            _lastFrameTimestamp = currentTimestamp;
            lock (_lock)
            {
                _frameNumber++;
                _totalFramesCaptured = _frameNumber;
            }

            var eventArgs = new FrameReadyEventArgs(bitmap, timestamp, _frameNumber);
            FrameReady?.Invoke(this, eventArgs);
        }
        catch (Exception ex)
        {
            if (_singleFrameCapture != null && !_singleFrameCapture.Task.IsCompleted)
            {
                _singleFrameCapture.TrySetException(ex);
                return;
            }

            CaptureError?.Invoke(this, new CaptureErrorEventArgs(ex, DateTime.Now, false));
            StopSessionInternal(CaptureStopReason.Error, ex);
        }
    }

    private void StopSessionInternal(CaptureStopReason reason, Exception? exception = null)
    {
        lock (_lock)
        {
            if (!_isRunning)
                return;

            _isRunning = false;
            _stopwatch.Stop();

            try
            {
                _captureSession?.Dispose();
                _framePool?.Dispose();
            }
            catch
            {
            }
            finally
            {
                _captureSession = null;
                _framePool = null;
            }
        }

        CaptureStopped?.Invoke(this, new CaptureStoppedEventArgs(reason, exception));
    }
}
