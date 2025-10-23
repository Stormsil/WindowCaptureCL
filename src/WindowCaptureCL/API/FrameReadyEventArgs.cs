using System.Drawing;

namespace WindowCaptureCL;

/// <summary>
/// Provides data for the <see cref="ICaptureSession.FrameReady"/> event.
/// </summary>
public sealed class FrameReadyEventArgs : EventArgs
{
    /// <summary>
    /// Gets the captured frame as a <see cref="Bitmap"/>.
    /// </summary>
    public Bitmap Frame { get; }

    /// <summary>
    /// Gets the timestamp when the frame was captured.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets the frame sequence number since the capture session started.
    /// </summary>
    public ulong FrameNumber { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameReadyEventArgs"/> class.
    /// </summary>
    /// <param name="frame">The captured frame.</param>
    /// <param name="timestamp">The timestamp when the frame was captured.</param>
    /// <param name="frameNumber">The frame sequence number.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="frame"/> is null.</exception>
    public FrameReadyEventArgs(Bitmap frame, DateTime timestamp, ulong frameNumber)
    {
        Frame = frame ?? throw new ArgumentNullException(nameof(frame));
        Timestamp = timestamp;
        FrameNumber = frameNumber;
    }
}
