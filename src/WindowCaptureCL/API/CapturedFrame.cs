using System.Drawing;

namespace WindowCaptureCL;

/// <summary>
/// Represents a single captured frame with associated metadata.
/// </summary>
public sealed class CapturedFrame : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Gets the bitmap data of the captured frame.
    /// </summary>
    public Bitmap Bitmap { get; }

    /// <summary>
    /// Gets the timestamp when the frame was captured.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets the width of the frame in pixels.
    /// </summary>
    public int Width => Bitmap.Width;

    /// <summary>
    /// Gets the height of the frame in pixels.
    /// </summary>
    public int Height => Bitmap.Height;

    /// <summary>
    /// Initializes a new instance of the <see cref="CapturedFrame"/> class.
    /// </summary>
    /// <param name="bitmap">The bitmap data of the captured frame.</param>
    /// <param name="timestamp">The timestamp when the frame was captured.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bitmap"/> is null.</exception>
    public CapturedFrame(Bitmap bitmap, DateTime timestamp)
    {
        Bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));
        Timestamp = timestamp;
    }

    /// <summary>
    /// Saves the captured frame to a file.
    /// </summary>
    /// <param name="filePath">The path where the frame should be saved.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the frame has been disposed.</exception>
    public void Save(string filePath)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CapturedFrame));

        if (filePath == null)
            throw new ArgumentNullException(nameof(filePath));

        Bitmap.Save(filePath);
    }

    /// <summary>
    /// Saves the captured frame to a file with the specified format.
    /// </summary>
    /// <param name="filePath">The path where the frame should be saved.</param>
    /// <param name="format">The image format to use.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the frame has been disposed.</exception>
    public void Save(string filePath, System.Drawing.Imaging.ImageFormat format)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CapturedFrame));

        if (filePath == null)
            throw new ArgumentNullException(nameof(filePath));

        Bitmap.Save(filePath, format);
    }

    /// <summary>
    /// Releases all resources used by this <see cref="CapturedFrame"/>.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        Bitmap?.Dispose();
        _disposed = true;
    }
}
