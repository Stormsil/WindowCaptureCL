using System.Diagnostics;
using System.Drawing;

namespace WindowCaptureCL.Tests.Integration;

public class WindowCaptureTests : IDisposable
{
    private Process? _notepadProcess;

    [Fact]
    public void CaptureWindow_ValidWindow_ReturnsBitmap()
    {
        // Arrange
        _notepadProcess = Process.Start("notepad.exe");
        Thread.Sleep(1000); // Wait for window to be fully created

        var windowHandle = _notepadProcess.MainWindowHandle;
        Assert.NotEqual(IntPtr.Zero, windowHandle);

        // Act
        using var session = Capture.FromWindow(windowHandle);
        using var frame = session.CaptureFrame();

        // Assert
        Assert.NotNull(frame);
        Assert.NotNull(frame.Bitmap);
        Assert.True(frame.Bitmap.Width > 0);
        Assert.True(frame.Bitmap.Height > 0);
    }

    [Fact]
    public void CaptureWindow_VerifyDimensionsMatchWindowSize()
    {
        // Arrange
        _notepadProcess = Process.Start("notepad.exe");
        Thread.Sleep(1000);

        var windowHandle = _notepadProcess.MainWindowHandle;

        // Act
        using var session = Capture.FromWindow(windowHandle);
        var sourceSize = session.SourceInfo;
        using var frame = session.CaptureFrame();

        // Assert
        Assert.Equal(sourceSize.Width, frame.Bitmap.Width);
        Assert.Equal(sourceSize.Height, frame.Bitmap.Height);
    }

    [Fact]
    public void CaptureWindow_InvalidHandle_ThrowsWindowNotFoundException()
    {
        // Arrange
        var invalidHandle = new IntPtr(99999);

        // Act & Assert
        Assert.Throws<WindowNotFoundException>(() => Capture.FromWindow(invalidHandle));
    }

    [Fact]
    public void CaptureWindow_ObscuredWindow_ReturnsValidBitmap()
    {
        // Arrange - Launch first Notepad
        _notepadProcess = Process.Start("notepad.exe");
        Thread.Sleep(1000);

        var windowHandle = _notepadProcess.MainWindowHandle;

        // Launch second overlapping Notepad
        using var overlappingProcess = Process.Start("notepad.exe");
        Thread.Sleep(1000);

        // Act - Capture the first (obscured) window
        using var session = Capture.FromWindow(windowHandle);
        using var frame = session.CaptureFrame();

        // Assert - Should still capture successfully even when obscured
        Assert.NotNull(frame);
        Assert.NotNull(frame.Bitmap);
        Assert.True(frame.Bitmap.Width > 0);
        Assert.True(frame.Bitmap.Height > 0);

        // Cleanup overlapping window
        try
        {
            overlappingProcess.Kill();
            overlappingProcess.WaitForExit();
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public void FromWindow_IntPtrZero_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Capture.FromWindow(IntPtr.Zero));
    }

    public void Dispose()
    {
        // Cleanup Notepad process if it's still running
        if (_notepadProcess != null && !_notepadProcess.HasExited)
        {
            try
            {
                _notepadProcess.Kill();
                _notepadProcess.WaitForExit();
            }
            catch
            {
                // Ignore cleanup errors
            }
            finally
            {
                _notepadProcess.Dispose();
            }
        }
    }
}
