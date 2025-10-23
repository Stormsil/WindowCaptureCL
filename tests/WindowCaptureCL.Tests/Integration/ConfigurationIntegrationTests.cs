using System.Diagnostics;

namespace WindowCaptureCL.Tests.Integration;

public class ConfigurationIntegrationTests : IDisposable
{
    private Process? _notepadProcess;

    // T131: Verify new session uses DefaultTargetFPS value
    [Fact]
    public void NewSession_UsesDefaultTargetFPS()
    {
        // Arrange
        var originalDefaultFPS = CaptureConfiguration.DefaultTargetFPS;

        try
        {
            CaptureConfiguration.DefaultTargetFPS = 45;

            _notepadProcess = Process.Start("notepad.exe");
            Thread.Sleep(1000);

            var windowHandle = _notepadProcess.MainWindowHandle;
            Assert.NotEqual(IntPtr.Zero, windowHandle);

            // Act - Create new session
            using var session = Capture.FromWindow(windowHandle);

            // Assert - Session should use the global DefaultTargetFPS (45)
            // We can verify this by checking if the session respects the FPS
            var frameCount = 0;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            session.FrameReady += (sender, args) =>
            {
                Interlocked.Increment(ref frameCount);
                args.Frame.Dispose();
            };

            session.StartCapture();
            Thread.Sleep(2000); // Capture for 2 seconds
            session.StopCapture();
            stopwatch.Stop();

            // At 45 FPS for 2 seconds, we expect ~90 frames (with tolerance)
            Assert.InRange(frameCount, 76, 104); // 90 ± 15%
        }
        finally
        {
            // Restore original default
            CaptureConfiguration.DefaultTargetFPS = originalDefaultFPS;
        }
    }

    // T129-T130: Tests for IncludeCursor setting
    // Note: These tests verify that the setting is applied to the session,
    // but cannot reliably verify cursor presence in captured frames without
    // complex pixel analysis and cursor positioning.
    [Fact]
    public void CaptureSession_RespectsSameValueNoCursor()
    {
        var originalIncludeCursor = CaptureConfiguration.IncludeCursor;

        try
        {
            // Arrange
            CaptureConfiguration.IncludeCursor = false;

            _notepadProcess = Process.Start("notepad.exe");
            Thread.Sleep(1000);

            var windowHandle = _notepadProcess.MainWindowHandle;
            Assert.NotEqual(IntPtr.Zero, windowHandle);

            // Act
            using var session = Capture.FromWindow(windowHandle);
            using var frame = session.CaptureFrame();

            // Assert
            Assert.NotNull(frame);
            Assert.NotNull(frame.Bitmap);
            // Note: We cannot easily verify cursor absence in bitmap
            // The test verifies the setting is applied without errors
        }
        finally
        {
            CaptureConfiguration.IncludeCursor = originalIncludeCursor;
        }
    }

    [Fact]
    public void CaptureSession_RespectsIncludeCursorTrue()
    {
        var originalIncludeCursor = CaptureConfiguration.IncludeCursor;

        try
        {
            // Arrange
            CaptureConfiguration.IncludeCursor = true;

            _notepadProcess = Process.Start("notepad.exe");
            Thread.Sleep(1000);

            var windowHandle = _notepadProcess.MainWindowHandle;
            Assert.NotEqual(IntPtr.Zero, windowHandle);

            // Act
            using var session = Capture.FromWindow(windowHandle);
            using var frame = session.CaptureFrame();

            // Assert
            Assert.NotNull(frame);
            Assert.NotNull(frame.Bitmap);
            // Note: We cannot easily verify cursor presence in bitmap
            // The test verifies the setting is applied without errors
        }
        finally
        {
            CaptureConfiguration.IncludeCursor = originalIncludeCursor;
        }
    }

    // Test that DrawBorder setting doesn't cause errors
    [Fact]
    public void CaptureSession_RespectsDrawBorderSetting()
    {
        var originalDrawBorder = CaptureConfiguration.DrawBorder;

        try
        {
            // Arrange
            CaptureConfiguration.DrawBorder = true;

            _notepadProcess = Process.Start("notepad.exe");
            Thread.Sleep(1000);

            var windowHandle = _notepadProcess.MainWindowHandle;
            Assert.NotEqual(IntPtr.Zero, windowHandle);

            // Act
            using var session = Capture.FromWindow(windowHandle);
            using var frame = session.CaptureFrame();

            // Assert
            Assert.NotNull(frame);
            Assert.NotNull(frame.Bitmap);
            // Note: DrawBorder is not supported by WGC API, but setting it shouldn't cause errors
        }
        finally
        {
            CaptureConfiguration.DrawBorder = originalDrawBorder;
        }
    }

    // Test that changing global settings affects new sessions
    [Fact]
    public void GlobalSettings_AffectNewSessions()
    {
        var originalDefaultFPS = CaptureConfiguration.DefaultTargetFPS;
        var originalIncludeCursor = CaptureConfiguration.IncludeCursor;

        try
        {
            // Arrange
            CaptureConfiguration.DefaultTargetFPS = 15;
            CaptureConfiguration.IncludeCursor = true;

            _notepadProcess = Process.Start("notepad.exe");
            Thread.Sleep(1000);

            var windowHandle = _notepadProcess.MainWindowHandle;
            Assert.NotEqual(IntPtr.Zero, windowHandle);

            // Act
            using var session = Capture.FromWindow(windowHandle);

            var frameCount = 0;
            session.FrameReady += (sender, args) =>
            {
                Interlocked.Increment(ref frameCount);
                args.Frame.Dispose();
            };

            session.StartCapture();
            Thread.Sleep(2000);
            session.StopCapture();

            // Assert - At 15 FPS, expect ~30 frames in 2 seconds
            Assert.InRange(frameCount, 26, 34); // 30 ± ~13%
        }
        finally
        {
            CaptureConfiguration.DefaultTargetFPS = originalDefaultFPS;
            CaptureConfiguration.IncludeCursor = originalIncludeCursor;
        }
    }

    // Test that per-session configuration overrides global default
    [Fact]
    public void PerSessionConfiguration_OverridesGlobalDefault()
    {
        var originalDefaultFPS = CaptureConfiguration.DefaultTargetFPS;

        try
        {
            // Arrange
            CaptureConfiguration.DefaultTargetFPS = 30;

            _notepadProcess = Process.Start("notepad.exe");
            Thread.Sleep(1000);

            var windowHandle = _notepadProcess.MainWindowHandle;
            Assert.NotEqual(IntPtr.Zero, windowHandle);

            using var session = Capture.FromWindow(windowHandle);

            // Act - Override with per-session configuration
            var config = new CaptureConfiguration
            {
                MaxFramesPerSecond = 10
            };
            session.UpdateConfiguration(config);

            var frameCount = 0;
            session.FrameReady += (sender, args) =>
            {
                Interlocked.Increment(ref frameCount);
                args.Frame.Dispose();
            };

            session.StartCapture();
            Thread.Sleep(2000);
            session.StopCapture();

            // Assert - Should use 10 FPS, not 30 FPS
            Assert.InRange(frameCount, 17, 23); // 20 ± ~15%
        }
        finally
        {
            CaptureConfiguration.DefaultTargetFPS = originalDefaultFPS;
        }
    }

    public void Dispose()
    {
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
