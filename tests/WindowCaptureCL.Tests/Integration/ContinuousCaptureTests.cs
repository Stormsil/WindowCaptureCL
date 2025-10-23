using System.Diagnostics;
using System.Drawing;

namespace WindowCaptureCL.Tests.Integration;

public class ContinuousCaptureTests : IDisposable
{
    private Process? _notepadProcess;

    // T082-T085: Test starting session at 10 FPS for 2 seconds and verify frame count, timing, and event data
    [Fact]
    public void StartCapture_At10FPS_CapturesExpectedFrameCount()
    {
        // Arrange
        _notepadProcess = Process.Start("notepad.exe");
        Thread.Sleep(1000); // Wait for window to be fully created

        var windowHandle = _notepadProcess.MainWindowHandle;
        Assert.NotEqual(IntPtr.Zero, windowHandle);

        using var session = Capture.FromWindow(windowHandle);

        var frameCount = 0;
        var frameTimestamps = new List<DateTime>();
        var receivedBitmaps = new List<Bitmap>();

        session.FrameReady += (sender, args) =>
        {
            Interlocked.Increment(ref frameCount);
            lock (frameTimestamps)
            {
                frameTimestamps.Add(args.Timestamp);
            }
            lock (receivedBitmaps)
            {
                receivedBitmaps.Add((Bitmap)args.Frame.Clone());
            }
        };

        // Configure to 10 FPS
        var config = new CaptureConfiguration
        {
            MaxFramesPerSecond = 10
        };
        session.UpdateConfiguration(config);

        // Act
        session.StartCapture();
        Thread.Sleep(2000); // Capture for 2 seconds
        session.StopCapture();

        // Assert - T083: Frame count should be ~20 frames (10 FPS * 2 seconds) with ±10% tolerance
        Assert.InRange(frameCount, 18, 22); // 20 ± 10%

        // T084: Verify frame timing accuracy
        lock (frameTimestamps)
        {
            if (frameTimestamps.Count > 1)
            {
                var intervals = new List<double>();
                for (int i = 1; i < frameTimestamps.Count; i++)
                {
                    var interval = (frameTimestamps[i] - frameTimestamps[i - 1]).TotalMilliseconds;
                    intervals.Add(interval);
                }

                var averageInterval = intervals.Average();
                var expectedInterval = 1000.0 / 10.0; // 100ms for 10 FPS

                // Average interval should be close to expected (±20% tolerance for timing variations)
                Assert.InRange(averageInterval, expectedInterval * 0.8, expectedInterval * 1.2);
            }
        }

        // T085: Verify FrameReady event delivers non-null Bitmap with metadata
        lock (receivedBitmaps)
        {
            Assert.NotEmpty(receivedBitmaps);
            foreach (var bitmap in receivedBitmaps)
            {
                Assert.NotNull(bitmap);
                Assert.True(bitmap.Width > 0);
                Assert.True(bitmap.Height > 0);

                // Dispose bitmaps to prevent memory leaks
                bitmap.Dispose();
            }
        }

        // Also verify timestamps are valid
        lock (frameTimestamps)
        {
            Assert.NotEmpty(frameTimestamps);
            foreach (var timestamp in frameTimestamps)
            {
                Assert.NotEqual(DateTime.MinValue, timestamp);
            }
        }
    }

    // T086: Test closing window mid-capture and verify CaptureStopped event fires with SourceClosed reason
    [Fact]
    public void StartCapture_WindowClosedMidCapture_FiresStoppedEventWithSourceClosedReason()
    {
        // Arrange
        _notepadProcess = Process.Start("notepad.exe");
        Thread.Sleep(1000);

        var windowHandle = _notepadProcess.MainWindowHandle;
        Assert.NotEqual(IntPtr.Zero, windowHandle);

        using var session = Capture.FromWindow(windowHandle);

        CaptureStoppedEventArgs? stoppedArgs = null;
        var stoppedEvent = new ManualResetEventSlim(false);

        session.CaptureStopped += (sender, args) =>
        {
            stoppedArgs = args;
            stoppedEvent.Set();
        };

        // Act
        session.StartCapture();
        Thread.Sleep(500); // Let it capture for a bit

        // Close the window
        _notepadProcess.Kill();
        _notepadProcess.WaitForExit();

        // Wait for CaptureStopped event
        var eventFired = stoppedEvent.Wait(TimeSpan.FromSeconds(3));

        // Assert
        Assert.True(eventFired, "CaptureStopped event should fire when window is closed");
        Assert.NotNull(stoppedArgs);
        Assert.Equal(CaptureStopReason.SourceClosed, stoppedArgs.Reason);
    }

    // T087: Test FPS throttling - session at 30 FPS should skip frames if source updates at higher rate
    [Fact]
    public void StartCapture_WithFPSThrottling_LimitsFrameRate()
    {
        // Arrange
        _notepadProcess = Process.Start("notepad.exe");
        Thread.Sleep(1000);

        var windowHandle = _notepadProcess.MainWindowHandle;
        Assert.NotEqual(IntPtr.Zero, windowHandle);

        using var session = Capture.FromWindow(windowHandle);

        var frameTimestamps = new List<DateTime>();

        session.FrameReady += (sender, args) =>
        {
            lock (frameTimestamps)
            {
                frameTimestamps.Add(args.Timestamp);
            }
            args.Frame.Dispose(); // Dispose bitmap to prevent memory leak
        };

        // Configure to 30 FPS
        var config = new CaptureConfiguration
        {
            MaxFramesPerSecond = 30
        };
        session.UpdateConfiguration(config);

        // Act
        session.StartCapture();
        Thread.Sleep(1000); // Capture for 1 second
        session.StopCapture();

        // Assert - Should get ~30 frames in 1 second
        lock (frameTimestamps)
        {
            Assert.InRange(frameTimestamps.Count, 25, 35); // 30 ± ~17%

            // Verify minimum interval between frames (should not be less than ~33ms for 30 FPS)
            if (frameTimestamps.Count > 1)
            {
                var minInterval = double.MaxValue;
                for (int i = 1; i < frameTimestamps.Count; i++)
                {
                    var interval = (frameTimestamps[i] - frameTimestamps[i - 1]).TotalMilliseconds;
                    if (interval < minInterval)
                        minInterval = interval;
                }

                // Minimum interval should be at least 25ms (accounting for timing variations)
                // This proves throttling is working and we're not capturing every available frame
                Assert.True(minInterval >= 25, $"Minimum interval {minInterval}ms suggests insufficient throttling");
            }
        }
    }

    // T088: Test SessionAlreadyStartedException when calling Start() on running session
    [Fact]
    public void StartCapture_WhenAlreadyStarted_ThrowsSessionAlreadyStartedException()
    {
        // Arrange
        _notepadProcess = Process.Start("notepad.exe");
        Thread.Sleep(1000);

        var windowHandle = _notepadProcess.MainWindowHandle;
        Assert.NotEqual(IntPtr.Zero, windowHandle);

        using var session = Capture.FromWindow(windowHandle);

        session.FrameReady += (sender, args) => args.Frame.Dispose(); // Dispose bitmap to prevent memory leak

        // Act
        session.StartCapture();

        // Assert
        Assert.Throws<SessionAlreadyStartedException>(() => session.StartCapture());

        // Cleanup
        session.StopCapture();
    }

    // T089: Test CaptureFrameAsync() returns valid Bitmap
    [Fact]
    public async Task CaptureFrameAsync_ValidWindow_ReturnsValidBitmap()
    {
        // Arrange
        _notepadProcess = Process.Start("notepad.exe");
        Thread.Sleep(1000);

        var windowHandle = _notepadProcess.MainWindowHandle;
        Assert.NotEqual(IntPtr.Zero, windowHandle);

        using var session = Capture.FromWindow(windowHandle);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var frame = await session.CaptureFrameAsync(cts.Token);

        // Assert
        Assert.NotNull(frame);
        Assert.NotNull(frame.Bitmap);
        Assert.True(frame.Bitmap.Width > 0);
        Assert.True(frame.Bitmap.Height > 0);
        Assert.NotEqual(DateTime.MinValue, frame.Timestamp);
    }

    // Additional test: Verify StopCapture throws when session not started
    [Fact]
    public void StopCapture_WhenNotStarted_ThrowsSessionNotStartedException()
    {
        // Arrange
        _notepadProcess = Process.Start("notepad.exe");
        Thread.Sleep(1000);

        var windowHandle = _notepadProcess.MainWindowHandle;
        Assert.NotEqual(IntPtr.Zero, windowHandle);

        using var session = Capture.FromWindow(windowHandle);

        // Act & Assert
        Assert.Throws<SessionNotStartedException>(() => session.StopCapture());
    }

    // Additional test: Verify IsActive property reflects capture state
    [Fact]
    public void IsActive_ReflectsCaptureState()
    {
        // Arrange
        _notepadProcess = Process.Start("notepad.exe");
        Thread.Sleep(1000);

        var windowHandle = _notepadProcess.MainWindowHandle;
        Assert.NotEqual(IntPtr.Zero, windowHandle);

        using var session = Capture.FromWindow(windowHandle);

        session.FrameReady += (sender, args) => args.Frame.Dispose(); // Dispose bitmap to prevent memory leak

        // Assert - Initially not active
        Assert.False(session.IsActive);

        // Act - Start capture
        session.StartCapture();

        // Assert - Now active
        Assert.True(session.IsActive);

        // Act - Stop capture
        session.StopCapture();

        // Assert - No longer active
        Assert.False(session.IsActive);
    }

    // Additional test: Verify TotalFramesCaptured increments correctly
    [Fact]
    public void TotalFramesCaptured_IncrementsCorrectly()
    {
        // Arrange
        _notepadProcess = Process.Start("notepad.exe");
        Thread.Sleep(1000);

        var windowHandle = _notepadProcess.MainWindowHandle;
        Assert.NotEqual(IntPtr.Zero, windowHandle);

        using var session = Capture.FromWindow(windowHandle);

        session.FrameReady += (sender, args) => args.Frame.Dispose(); // Dispose bitmap to prevent memory leak

        var config = new CaptureConfiguration
        {
            MaxFramesPerSecond = 15
        };
        session.UpdateConfiguration(config);

        // Assert - Initially zero
        Assert.Equal(0UL, session.TotalFramesCaptured);

        // Act
        session.StartCapture();
        Thread.Sleep(1000);
        session.StopCapture();

        // Assert - Should have captured some frames
        Assert.True(session.TotalFramesCaptured > 0);
        Assert.InRange(session.TotalFramesCaptured, 12UL, 18UL); // ~15 frames ± 20%
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
