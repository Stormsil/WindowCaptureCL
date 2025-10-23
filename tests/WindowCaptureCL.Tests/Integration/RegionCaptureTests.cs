using System.Drawing;

namespace WindowCaptureCL.Tests.Integration;

public class RegionCaptureTests
{
    // T109-T110: Test capturing Rectangle(0, 0, 800, 600) and verify dimensions
    [Fact]
    public void FromScreenRegion_800x600Region_ReturnsExactDimensions()
    {
        // Arrange
        var region = new Rectangle(0, 0, 800, 600);

        // Act
        using var session = Capture.FromScreenRegion(0, region);
        using var frame = session.CaptureFrame();

        // Assert
        Assert.NotNull(frame);
        Assert.NotNull(frame.Bitmap);
        Assert.Equal(800, frame.Bitmap.Width);
        Assert.Equal(600, frame.Bitmap.Height);
    }

    // T111: Test RegionOutOfBoundsException when region extends beyond monitor
    [Fact]
    public void FromScreenRegion_RegionOutOfBounds_ThrowsRegionOutOfBoundsException()
    {
        // Arrange - Region with coordinates far beyond any reasonable monitor
        var region = new Rectangle(10000, 10000, 800, 600);

        // Act & Assert
        Assert.Throws<RegionOutOfBoundsException>(() => Capture.FromScreenRegion(0, region));
    }

    [Fact]
    public void FromScreenRegion_RegionTooWide_ThrowsRegionOutOfBoundsException()
    {
        // Arrange - Region that's wider than monitor
        var region = new Rectangle(0, 0, 50000, 100);

        // Act & Assert
        Assert.Throws<RegionOutOfBoundsException>(() => Capture.FromScreenRegion(0, region));
    }

    [Fact]
    public void FromScreenRegion_NegativeDimensions_ThrowsArgumentException()
    {
        // Arrange
        var region = new Rectangle(0, 0, -100, 100);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Capture.FromScreenRegion(0, region));
    }

    [Fact]
    public void FromScreenRegion_ZeroWidthOrHeight_ThrowsArgumentException()
    {
        // Arrange
        var region1 = new Rectangle(0, 0, 0, 600);
        var region2 = new Rectangle(0, 0, 800, 0);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Capture.FromScreenRegion(0, region1));
        Assert.Throws<ArgumentException>(() => Capture.FromScreenRegion(0, region2));
    }

    // T112: Test continuous region capture at 20 FPS for 2 seconds
    [Fact]
    public void StartCapture_Region_At20FPSFor2Seconds_CapturesExpectedFrames()
    {
        // Arrange
        var region = new Rectangle(0, 0, 640, 480);
        using var session = Capture.FromScreenRegion(0, region);

        var frameCount = 0;
        var frameTimestamps = new List<DateTime>();

        session.FrameReady += (sender, args) =>
        {
            Interlocked.Increment(ref frameCount);
            lock (frameTimestamps)
            {
                frameTimestamps.Add(args.Timestamp);
            }

            // Verify frame dimensions
            Assert.Equal(640, args.Frame.Width);
            Assert.Equal(480, args.Frame.Height);

            args.Frame.Dispose(); // Dispose bitmap to prevent memory leak
        };

        // Configure to 20 FPS
        var config = new CaptureConfiguration
        {
            MaxFramesPerSecond = 20
        };
        session.UpdateConfiguration(config);

        // Act
        session.StartCapture();
        Thread.Sleep(2000); // Capture for 2 seconds
        session.StopCapture();

        // Assert - Frame count should be ~40 frames (20 FPS * 2 seconds) with ±15% tolerance
        Assert.InRange(frameCount, 34, 46); // 40 ± 15%

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
                var expectedInterval = 1000.0 / 20.0; // 50ms for 20 FPS

                // Average interval should be close to expected (±25% tolerance for timing variations)
                Assert.InRange(averageInterval, expectedInterval * 0.75, expectedInterval * 1.25);
            }
        }
    }

    // Additional test: Verify region source info properties
    [Fact]
    public void FromScreenRegion_SourceInfo_HasCorrectProperties()
    {
        // Arrange
        var region = new Rectangle(100, 100, 400, 300);

        // Act
        using var session = Capture.FromScreenRegion(0, region);
        var sourceInfo = session.SourceInfo;

        // Assert
        Assert.NotNull(sourceInfo);
        Assert.Equal(CaptureSourceType.Region, sourceInfo.SourceType);
        Assert.Equal(0, sourceInfo.MonitorIndex);
        Assert.Equal(region, sourceInfo.Region);
        Assert.Equal(400, sourceInfo.Width);
        Assert.Equal(300, sourceInfo.Height);
        Assert.Contains("Region", sourceInfo.DisplayName);
    }

    // Additional test: Verify different region positions
    [Fact]
    public void FromScreenRegion_DifferentPositions_CapturesCorrectly()
    {
        // Arrange - Region at non-zero position
        var region = new Rectangle(200, 150, 320, 240);

        // Act
        using var session = Capture.FromScreenRegion(0, region);
        using var frame = session.CaptureFrame();

        // Assert
        Assert.NotNull(frame);
        Assert.NotNull(frame.Bitmap);
        Assert.Equal(320, frame.Bitmap.Width);
        Assert.Equal(240, frame.Bitmap.Height);
    }

    // Additional test: Verify negative monitor index throws
    [Fact]
    public void FromScreenRegion_NegativeMonitorIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var region = new Rectangle(0, 0, 800, 600);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Capture.FromScreenRegion(-1, region));
    }

    // Additional test: Verify invalid monitor index throws
    [Fact]
    public void FromScreenRegion_InvalidMonitorIndex_ThrowsInvalidMonitorException()
    {
        // Arrange
        var region = new Rectangle(0, 0, 800, 600);
        int invalidIndex = 99;

        // Act & Assert
        Assert.Throws<InvalidMonitorException>(() => Capture.FromScreenRegion(invalidIndex, region));
    }
}
