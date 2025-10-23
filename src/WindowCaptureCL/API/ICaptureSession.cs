using System.Drawing;

namespace WindowCaptureCL;

/// <summary>
/// Represents an active capture session for a window, monitor, or screen region.
/// </summary>
public interface ICaptureSession : IDisposable
{
    /// <summary>
    /// Gets information about the capture source.
    /// </summary>
    CaptureSourceInfo SourceInfo { get; }

    /// <summary>
    /// Gets a value indicating whether the capture session is currently active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Gets the configuration used by this capture session.
    /// </summary>
    CaptureConfiguration Configuration { get; }

    /// <summary>
    /// Gets the total number of frames captured since the session started.
    /// </summary>
    ulong TotalFramesCaptured { get; }

    /// <summary>
    /// Event raised when a new frame is ready in continuous capture mode.
    /// </summary>
    event EventHandler<FrameReadyEventArgs>? FrameReady;

    /// <summary>
    /// Event raised when an error occurs during capture.
    /// </summary>
    event EventHandler<CaptureErrorEventArgs>? CaptureError;

    /// <summary>
    /// Event raised when the capture session stops.
    /// </summary>
    event EventHandler<CaptureStoppedEventArgs>? CaptureStopped;

    /// <summary>
    /// Starts continuous frame capture.
    /// Frames will be delivered via the <see cref="FrameReady"/> event.
    /// </summary>
    /// <exception cref="InvalidCaptureStateException">Thrown when the session is already active.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
    void StartCapture();

    /// <summary>
    /// Stops continuous frame capture.
    /// </summary>
    /// <exception cref="InvalidCaptureStateException">Thrown when the session is not active.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
    void StopCapture();

    /// <summary>
    /// Captures a single frame immediately.
    /// </summary>
    /// <returns>A <see cref="CapturedFrame"/> containing the captured image and metadata.</returns>
    /// <exception cref="FrameCaptureException">Thrown when frame capture fails.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
    CapturedFrame CaptureFrame();

    /// <summary>
    /// Captures a single frame asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing the captured frame.</returns>
    /// <exception cref="FrameCaptureException">Thrown when frame capture fails.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task<CapturedFrame> CaptureFrameAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the configuration for this capture session.
    /// Changes take effect immediately, even during active capture.
    /// </summary>
    /// <param name="configuration">The new configuration settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is null.</exception>
    /// <exception cref="InvalidConfigurationException">Thrown when the configuration is invalid.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
    void UpdateConfiguration(CaptureConfiguration configuration);
}
