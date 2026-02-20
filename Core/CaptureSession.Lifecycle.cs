namespace WindowCaptureCL.Core;

internal sealed partial class CaptureSession
{
    public void StartCapture()
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            if (_isRunning)
                throw new SessionAlreadyStartedException("The capture session is already running.");

            if (_graphicsCaptureItem == null)
            {
                InitializeWGCSession();
            }

            if (_framePool == null)
            {
                CreateFramePool();
            }

            _stopwatch.Restart();
            _lastFrameTimestamp = 0;
            _frameNumber = 0;

            _captureSession?.StartCapture();
            _isRunning = true;
        }
    }

    public void StopCapture()
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            if (!_isRunning)
                throw new SessionNotStartedException("The capture session is not running.");
        }

        StopSessionInternal(CaptureStopReason.UserRequested);
    }

    public void UpdateConfiguration(CaptureConfiguration configuration)
    {
        ThrowIfDisposed();

        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        configuration.Validate();

        lock (_lock)
        {
            _configuration = configuration.Clone();
            TargetFPS = configuration.MaxFramesPerSecond;

            if (_captureSession != null)
            {
                try
                {
                    _captureSession.IsCursorCaptureEnabled = configuration.SessionIncludeCursor;
                }
                catch
                {
                }
            }
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            try
            {
                if (_isRunning)
                {
                    _captureSession?.Dispose();
                    _isRunning = false;
                }

                _framePool?.Dispose();
                _captureSession = null;
                _framePool = null;
                _graphicsCaptureItem = null;
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}
