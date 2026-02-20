using System.Diagnostics;
using System.Drawing;
using Windows.Graphics.Capture;
using Vortice.Direct3D11;
using WindowCaptureCL.Infrastructure;
using WindowCaptureCL.Infrastructure.WGC;

namespace WindowCaptureCL.Core;

/// <summary>
/// Implements a capture session for capturing frames from windows, monitors, or screen regions.
/// </summary>
internal sealed partial class CaptureSession : ICaptureSession
{
    private readonly CaptureSourceInfo _captureSource;
    private readonly object _lock = new();
    private bool _disposed;
    private bool _isRunning;
    private int _targetFPS = 30;

    // WGC-related fields.
    private GraphicsCaptureItem? _graphicsCaptureItem;
    private Direct3D11CaptureFramePool? _framePool;
    private GraphicsCaptureSession? _captureSession;

    // For single-frame capture synchronization.
    private TaskCompletionSource<ID3D11Texture2D>? _singleFrameCapture;

    // Continuous capture fields.
    private readonly Stopwatch _stopwatch = new();
    private long _lastFrameTimestamp;
    private ulong _frameNumber;

    // Bitmap pool for frame reuse.
    private static readonly ObjectPool<Bitmap> _bitmapPool = new(
        factory: () => new Bitmap(1, 1),
        resetAction: null,
        disposeAction: bitmap => bitmap?.Dispose(),
        maxSize: 10);

    private CaptureConfiguration _configuration = new CaptureConfiguration();
    private ulong _totalFramesCaptured;

    public CaptureSession(CaptureSourceInfo captureSource)
    {
        _captureSource = captureSource ?? throw new ArgumentNullException(nameof(captureSource));

        _targetFPS = CaptureConfiguration.DefaultTargetFPS;
        DpiHelper.SetPerMonitorDpiAwareness();
    }

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

    public Size SourceSize
    {
        get
        {
            ThrowIfDisposed();

            lock (_lock)
            {
                if (_graphicsCaptureItem != null)
                {
                    return new Size(_graphicsCaptureItem.Size.Width, _graphicsCaptureItem.Size.Height);
                }

                if (_captureSource.SourceType == CaptureSourceType.Window &&
                    WgcInterop.GetWindowRect(_captureSource.WindowHandle, out var rect))
                {
                    return new Size(rect.Width, rect.Height);
                }

                return Size.Empty;
            }
        }
    }

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

    public CaptureSourceInfo CaptureSource
    {
        get
        {
            ThrowIfDisposed();
            return _captureSource;
        }
    }

    public CaptureSourceInfo SourceInfo => CaptureSource;
    public bool IsActive => IsRunning;

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

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CaptureSession));
    }
}
