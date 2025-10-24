using System.Diagnostics;
using System.Drawing;
using System.Threading;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Vortice.Direct3D11;
using WindowCaptureCL.Infrastructure.DirectX;
using WindowCaptureCL.Infrastructure.WGC;

namespace WindowCaptureCL.Core;

/// <summary>
/// Implements a capture session for capturing frames from windows, monitors, or screen regions.
/// </summary>
internal sealed class CaptureSession : ICaptureSession
{
    private readonly CaptureSourceInfo _captureSource;
    private readonly object _lock = new();
    private bool _disposed;
    private bool _isRunning;
    private int _targetFPS = 30;

    // WGC-related fields (T053-T055)
    private GraphicsCaptureItem? _graphicsCaptureItem;
    private Direct3D11CaptureFramePool? _framePool;
    private GraphicsCaptureSession? _captureSession;

    // For single-frame capture synchronization
    private TaskCompletionSource<ID3D11Texture2D>? _singleFrameCapture;

    // T069-T071: Continuous capture fields
    private readonly Stopwatch _stopwatch = new();
    private long _lastFrameTimestamp;
    private ulong _frameNumber;

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureSession"/> class.
    /// </summary>
    /// <param name="captureSource">The capture source information.</param>
    public CaptureSession(CaptureSourceInfo captureSource)
    {
        _captureSource = captureSource ?? throw new ArgumentNullException(nameof(captureSource));

        // T121: Read global DefaultTargetFPS and set TargetFPS property
        _targetFPS = CaptureConfiguration.DefaultTargetFPS;
    }

    // T049: IsRunning property with thread-safe access
    public bool IsRunning
    {
        get
        {
            lock (_lock)
            {
                return _isRunning;
            }
        }
        private set
        {
            lock (_lock)
            {
                _isRunning = value;
            }
        }
    }

    // T050: SourceSize property returning current capture source dimensions
    public System.Drawing.Size SourceSize
    {
        get
        {
            ThrowIfDisposed();

            lock (_lock)
            {
                if (_graphicsCaptureItem != null)
                {
                    return new System.Drawing.Size(
                        _graphicsCaptureItem.Size.Width,
                        _graphicsCaptureItem.Size.Height);
                }

                // If not initialized yet, calculate from source info
                if (_captureSource.SourceType == CaptureSourceType.Window)
                {
                    if (WgcInterop.GetWindowRect(_captureSource.WindowHandle, out var rect))
                    {
                        return new System.Drawing.Size(rect.Width, rect.Height);
                    }
                }

                return System.Drawing.Size.Empty;
            }
        }
    }

    // T051: TargetFPS property with validation (1-120 range) and lock-based thread safety
    public int TargetFPS
    {
        get
        {
            lock (_lock)
            {
                return _targetFPS;
            }
        }
        set
        {
            if (value < 1 || value > 120)
                throw new ArgumentOutOfRangeException(nameof(TargetFPS), "TargetFPS must be between 1 and 120.");

            lock (_lock)
            {
                _targetFPS = value;
            }
        }
    }

    // T052: CaptureSource property returning immutable CaptureSourceInfo
    public CaptureSourceInfo CaptureSource
    {
        get
        {
            ThrowIfDisposed();
            return _captureSource;
        }
    }

    // Interface properties
    public CaptureSourceInfo SourceInfo => CaptureSource;
    public bool IsActive => IsRunning;

    private CaptureConfiguration _configuration = new CaptureConfiguration();
    public CaptureConfiguration Configuration
    {
        get
        {
            lock (_lock)
            {
                return _configuration.Clone();
            }
        }
    }

    private ulong _totalFramesCaptured;
    public ulong TotalFramesCaptured
    {
        get
        {
            lock (_lock)
            {
                return _totalFramesCaptured;
            }
        }
    }

    public event EventHandler<FrameReadyEventArgs>? FrameReady;
    public event EventHandler<CaptureErrorEventArgs>? CaptureError;
    public event EventHandler<CaptureStoppedEventArgs>? CaptureStopped;

    // T056: Initialize WGC session creating GraphicsCaptureItem from source (window only for US1)
    private void InitializeWGCSession()
    {
        lock (_lock)
        {
            if (_graphicsCaptureItem != null)
                return; // Already initialized

            try
            {
                if (_captureSource.SourceType == CaptureSourceType.Window)
                {
                    // Validate window handle first
                    if (!WgcInterop.IsWindow(_captureSource.WindowHandle))
                    {
                        throw new WindowNotFoundException(
                            $"Window with handle {_captureSource.WindowHandle} not found or is invalid.",
                            _captureSource.WindowHandle);
                    }

                    // Check if window is visible and not minimized
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

                    // Create GraphicsCaptureItem for window
                    _graphicsCaptureItem = GraphicsCaptureItemHelper.CreateForWindow(_captureSource.WindowHandle);
                }
                // T093: Handle Monitor source type
                else if (_captureSource.SourceType == CaptureSourceType.Monitor)
                {
                    // T094: Monitor index validation
                    int monitorCount = WgcInterop.GetMonitorCount();
                    if (_captureSource.MonitorIndex < 0 || _captureSource.MonitorIndex >= monitorCount)
                    {
                        throw new InvalidMonitorException(
                            $"Monitor index {_captureSource.MonitorIndex} is out of range. Valid range: 0-{monitorCount - 1}.",
                            _captureSource.MonitorIndex);
                    }

                    // Get monitor handle and create GraphicsCaptureItem
                    IntPtr hMonitor = WgcInterop.GetMonitorHandle(_captureSource.MonitorIndex);
                    _graphicsCaptureItem = GraphicsCaptureItemHelper.CreateForMonitor(hMonitor);
                }
                // T103: Handle Region source type
                else if (_captureSource.SourceType == CaptureSourceType.Region)
                {
                    // T105: Region validation
                    WgcInterop.ValidateRegion(_captureSource.MonitorIndex, _captureSource.Region);

                    // Create GraphicsCaptureItem for the monitor (will crop during CopyTextureToBitmap)
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

    // T057: Create frame pool using DirectXManager device and FrameArrived event subscription
    private void CreateFramePool()
    {
        lock (_lock)
        {
            if (_graphicsCaptureItem == null)
                throw new InvalidOperationException("GraphicsCaptureItem must be initialized before creating frame pool.");

            try
            {
                var dxgiDevice = DirectXDeviceManager.Instance.DxgiDevice;

                // Create Direct3D device for WinRT
                IDirect3DDevice? direct3DDevice = null;
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

                // Get the size from GraphicsCaptureItem - simplified, no Win32 fallback
                var itemSize = _graphicsCaptureItem.Size;

                if (itemSize.Width <= 0 || itemSize.Height <= 0)
                {
                    throw new GraphicsDeviceException(
                        $"Capture target has invalid size: {itemSize.Width}x{itemSize.Height}. " +
                        $"For windows, ensure the window is visible and not minimized before capturing.");
                }

                // Create frame pool - use CreateFreeThreaded for console apps without UI message loop
                _framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                    direct3DDevice,
                    Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    2, // number of buffers
                    itemSize);

                // Subscribe to FrameArrived event
                _framePool.FrameArrived += OnFrameArrived;

                // Create capture session
                _captureSession = _framePool.CreateCaptureSession(_graphicsCaptureItem);

                // Apply cursor capture setting
                _captureSession.IsCursorCaptureEnabled = CaptureConfiguration.IncludeCursor;
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

    // T076-T080: Frame arrived event handler for both single and continuous capture
    private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        try
        {
            using var frame = sender.TryGetNextFrame();
            if (frame == null)
            {
                return;
            }

            // Get the D3D11 texture from the frame
            var surfaceTexture = Direct3D11Helper.GetD3D11Texture2DFromSurface(frame.Surface);

            // Handle single frame capture first
            if (_singleFrameCapture != null && !_singleFrameCapture.Task.IsCompleted)
            {
                _singleFrameCapture.TrySetResult(surfaceTexture);
                return;
            }

            // T076: Handle continuous capture with FPS throttling
            if (!IsRunning)
                return;

            // T079: Window validation - check if window is still valid (only for Window source)
            if (_captureSource.SourceType == CaptureSourceType.Window)
            {
                if (!WgcInterop.IsWindow(_captureSource.WindowHandle))
                {
                    // Window closed, stop session
                    StopSessionInternal(CaptureStopReason.SourceClosed);
                    return;
                }
            }

            // T077: FPS throttling - calculate elapsed time since last frame
            var currentTimestamp = _stopwatch.ElapsedMilliseconds;
            var targetInterval = 1000.0 / TargetFPS; // milliseconds per frame

            if (currentTimestamp - _lastFrameTimestamp < targetInterval)
            {
                // Not enough time has passed, skip this frame
                return;
            }

            // T078: Frame passes throttle - perform GPU-to-CPU copy
            var bitmap = CopyTextureToBitmap(surfaceTexture);
            var timestamp = DateTime.Now;

            // Update tracking
            _lastFrameTimestamp = currentTimestamp;
            lock (_lock)
            {
                _frameNumber++;
                _totalFramesCaptured = _frameNumber;
            }

            // Fire FrameReady event with bitmap
            var eventArgs = new FrameReadyEventArgs(bitmap, timestamp, _frameNumber);

            FrameReady?.Invoke(this, eventArgs);
        }
        catch (Exception ex)
        {
            // T080: Error handling
            if (_singleFrameCapture != null && !_singleFrameCapture.Task.IsCompleted)
            {
                _singleFrameCapture.TrySetException(ex);
                return;
            }

            // For continuous capture, fire error event and stop session
            CaptureError?.Invoke(this, new CaptureErrorEventArgs(ex, DateTime.Now, false));
            StopSessionInternal(CaptureStopReason.Error, ex);
        }
    }

    // Helper method to stop session and fire CaptureStopped event
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
                // Ignore cleanup errors
            }
            finally
            {
                _captureSession = null;
                _framePool = null;
            }
        }

        // Fire CaptureStopped event
        CaptureStopped?.Invoke(this, new CaptureStoppedEventArgs(reason, exception));
    }

    // T058: Implement TakeScreenshot() method
    public CapturedFrame CaptureFrame()
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            try
            {
                // Initialize WGC session if not already done
                if (_graphicsCaptureItem == null)
                {
                    InitializeWGCSession();
                }

                // Create frame pool if not already done
                if (_framePool == null)
                {
                    CreateFramePool();
                }

                // Set up single frame capture
                _singleFrameCapture = new TaskCompletionSource<ID3D11Texture2D>();

                // Start capture session
                _captureSession?.StartCapture();

                // Wait for frame (with timeout)
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
                var completedTask = Task.WhenAny(_singleFrameCapture.Task, timeoutTask).Result;

                if (completedTask == timeoutTask)
                {
                    throw new FrameCaptureException("Timeout waiting for frame capture.");
                }

                var texture = _singleFrameCapture.Task.Result;

                // Convert texture to Bitmap
                var bitmap = CopyTextureToBitmap(texture);

                // Create CapturedFrame with metadata
                var capturedFrame = new CapturedFrame(bitmap, DateTime.Now);

                // Cleanup for single frame capture
                _captureSession?.Dispose();
                _framePool?.Dispose();
                _framePool = null;
                _captureSession = null;

                return capturedFrame;
            }
            catch (Exception ex) when (ex is not CaptureException)
            {
                throw new FrameCaptureException("Failed to capture frame.", ex);
            }
            finally
            {
                _singleFrameCapture = null;
            }
        }
    }

    public Task<CapturedFrame> CaptureFrameAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return CaptureFrame();
        }, cancellationToken);
    }

    // T059, T104: Copy texture to Bitmap with staging texture, CopyResource, Map, pixel copy with stride handling
    // Handles region cropping for Region source type
    private Bitmap CopyTextureToBitmap(ID3D11Texture2D sourceTexture)
    {
        var device = DirectXDeviceManager.Instance.Device;
        var deviceContext = DirectXDeviceManager.Instance.DeviceContext;

        // T104: Use region-aware conversion for Region source type
        if (_captureSource.SourceType == CaptureSourceType.Region)
        {
            return FrameProcessor.ConvertTextureToBitmap(device, deviceContext, sourceTexture, _captureSource.Region);
        }

        return FrameProcessor.ConvertTextureToBitmap(device, deviceContext, sourceTexture);
    }

    // T072: Start continuous capture
    public void StartCapture()
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            if (_isRunning)
                throw new SessionAlreadyStartedException("The capture session is already running.");

            // Initialize WGC session if not already done
            if (_graphicsCaptureItem == null)
            {
                InitializeWGCSession();
            }

            // Create frame pool if not already done
            if (_framePool == null)
            {
                CreateFramePool();
            }

            // Reset timing and counters
            _stopwatch.Restart();
            _lastFrameTimestamp = 0;
            _frameNumber = 0;

            _captureSession?.StartCapture();
            _isRunning = true;
        }
    }

    // T073: Stop continuous capture
    public void StopCapture()
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            if (!_isRunning)
                throw new SessionNotStartedException("The capture session is not running.");
        }

        // Use StopSessionInternal to fire CaptureStopped event with UserRequested reason
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
            TargetFPS = configuration.MaxFramesPerSecond;
            // Other configuration properties can be updated here in future phases
        }
    }

    // T060: Implement Dispose() releasing WGC session, frame pool, and DirectX resources
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

    // T061: ObjectDisposedException guards
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CaptureSession));
    }
}
