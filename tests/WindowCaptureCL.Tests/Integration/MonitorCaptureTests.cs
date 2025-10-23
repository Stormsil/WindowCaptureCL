using System.Drawing;
using System.Windows.Forms;

namespace WindowCaptureCL.Tests.Integration;

public class MonitorCaptureTests
{
    // T098: Test capturing primary monitor (index 0)
    [Fact]
    public void FromScreen_PrimaryMonitor_ReturnsCaptureSession()
    {
        // Arrange & Act
        using var session = Capture.FromScreen(0);

        // Assert
        Assert.NotNull(session);
        Assert.NotNull(session.SourceInfo);
        Assert.Equal(CaptureSourceType.Monitor, session.SourceInfo.SourceType);
        Assert.Equal(0, session.SourceInfo.MonitorIndex);
    }

    // T099: Test captured dimensions match monitor resolution
    [Fact]
    public void CaptureFrame_PrimaryMonitor_MatchesMonitorResolution()
    {
        // Arrange
        var primaryScreen = Screen.AllScreens[0];
        var expectedWidth = primaryScreen.Bounds.Width;
        var expectedHeight = primaryScreen.Bounds.Height;

        using var session = Capture.FromScreen(0);

        // Act
        using var frame = session.CaptureFrame();

        // Assert
        Assert.NotNull(frame);
        Assert.NotNull(frame.Bitmap);
        Assert.Equal(expectedWidth, frame.Bitmap.Width);
        Assert.Equal(expectedHeight, frame.Bitmap.Height);
    }

    // T100: Test InvalidMonitorException when index out of range
    [Fact]
    public void FromScreen_InvalidMonitorIndex_ThrowsInvalidMonitorException()
    {
        // Arrange
        int invalidIndex = 99;

        // Act & Assert
        Assert.Throws<InvalidMonitorException>(() => Capture.FromScreen(invalidIndex));
    }

    [Fact]
    public void FromScreen_NegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        int negativeIndex = -1;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Capture.FromScreen(negativeIndex));
    }

    // T101: Test continuous monitor capture at 15 FPS for 3 seconds
    [Fact]
    public void StartCapture_Monitor_At15FPSFor3Seconds_CapturesExpectedFrames()
    {
        // Arrange
        using var session = Capture.FromScreen(0);

        var frameCount = 0;
        var frameTimestamps = new List<DateTime>();

        session.FrameReady += (sender, args) =>
        {
            Interlocked.Increment(ref frameCount);
            lock (frameTimestamps)
            {
                frameTimestamps.Add(args.Timestamp);
            }
            args.Frame.Dispose(); // Dispose bitmap to prevent memory leak
        };

        // Configure to 15 FPS
        var config = new CaptureConfiguration
        {
            MaxFramesPerSecond = 15
        };
        session.UpdateConfiguration(config);

        // Act
        session.StartCapture();
        Thread.Sleep(3000); // Capture for 3 seconds
        session.StopCapture();

        // Assert - Frame count should be ~45 frames (15 FPS * 3 seconds) with ±15% tolerance
        Assert.InRange(frameCount, 38, 52); // 45 ± 15%

        // Verify timing
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
                var expectedInterval = 1000.0 / 15.0; // ~66.67ms for 15 FPS

                // Average interval should be close to expected (±25% tolerance for timing variations)
                Assert.InRange(averageInterval, expectedInterval * 0.75, expectedInterval * 1.25);
            }
        }
    }

    // Additional test: Verify monitor source info properties
    [Fact]
    public void FromScreen_SourceInfo_HasCorrectProperties()
    {
        // Arrange & Act
        using var session = Capture.FromScreen(0);
        var sourceInfo = session.SourceInfo;

        // Assert
        Assert.NotNull(sourceInfo);
        Assert.Equal(CaptureSourceType.Monitor, sourceInfo.SourceType);
        Assert.Equal(0, sourceInfo.MonitorIndex);
        Assert.Contains("Monitor", sourceInfo.DisplayName);
        Assert.True(sourceInfo.Width > 0);
        Assert.True(sourceInfo.Height > 0);
    }

    // Additional test: Verify IsActive reflects capture state for monitor
    [Fact]
    public void IsActive_MonitorCapture_ReflectsCaptureState()
    {
        // Arrange
        using var session = Capture.FromScreen(0);

        session.FrameReady += (sender, args) => args.Frame.Dispose();

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
}
