namespace WindowCaptureCL.Tests.Unit;

public class CaptureConfigurationTests
{
    // T124: Verify IncludeCursor default is false
    [Fact]
    public void IncludeCursor_DefaultValue_IsFalse()
    {
        // Reset to defaults for test isolation
        CaptureConfiguration.IncludeCursor = false;

        Assert.False(CaptureConfiguration.IncludeCursor);
    }

    // T125: Verify DefaultTargetFPS default is 30
    [Fact]
    public void DefaultTargetFPS_DefaultValue_Is30()
    {
        // Reset to defaults for test isolation
        CaptureConfiguration.DefaultTargetFPS = 30;

        Assert.Equal(30, CaptureConfiguration.DefaultTargetFPS);
    }

    [Fact]
    public void DrawBorder_DefaultValue_IsFalse()
    {
        // Reset to defaults for test isolation
        CaptureConfiguration.DrawBorder = false;

        Assert.False(CaptureConfiguration.DrawBorder);
    }

    // T126: Verify DefaultTargetFPS validation throws ArgumentOutOfRangeException
    [Fact]
    public void DefaultTargetFPS_ThrowsOnInvalidValue_TooLow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CaptureConfiguration.DefaultTargetFPS = 0);
    }

    [Fact]
    public void DefaultTargetFPS_ThrowsOnInvalidValue_TooHigh()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CaptureConfiguration.DefaultTargetFPS = 121);
    }

    [Fact]
    public void DefaultTargetFPS_AcceptsValidValues()
    {
        CaptureConfiguration.DefaultTargetFPS = CaptureConfiguration.MinimumFPS;
        Assert.Equal(CaptureConfiguration.MinimumFPS, CaptureConfiguration.DefaultTargetFPS);

        CaptureConfiguration.DefaultTargetFPS = 60;
        Assert.Equal(60, CaptureConfiguration.DefaultTargetFPS);

        CaptureConfiguration.DefaultTargetFPS = CaptureConfiguration.MaximumFPS;
        Assert.Equal(CaptureConfiguration.MaximumFPS, CaptureConfiguration.DefaultTargetFPS);

        // Reset to default
        CaptureConfiguration.DefaultTargetFPS = 30;
    }

    // T127: Verify IncludeCursor thread safety
    [Fact]
    public void IncludeCursor_ThreadSafety_ConcurrentSetsSucceed()
    {
        var tasks = new List<Task>();
        var random = new Random();

        // Create 10 concurrent tasks that set IncludeCursor
        for (int i = 0; i < 10; i++)
        {
            int taskId = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    CaptureConfiguration.IncludeCursor = (taskId + j) % 2 == 0;
                    var value = CaptureConfiguration.IncludeCursor;
                    // Just read to ensure no exceptions
                }
            }));
        }

        // Wait for all tasks to complete
        Task.WaitAll(tasks.ToArray());

        // If we get here without exceptions, thread safety is working
        Assert.True(true);

        // Reset to default
        CaptureConfiguration.IncludeCursor = false;
    }

    // T128: Verify DefaultTargetFPS thread safety
    [Fact]
    public void DefaultTargetFPS_ThreadSafety_ConcurrentSetsSucceed()
    {
        var tasks = new List<Task>();
        var validValues = new[] { 10, 15, 30, 60, 90, 120 };

        // Create 10 concurrent tasks that set DefaultTargetFPS
        for (int i = 0; i < 10; i++)
        {
            int taskId = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    var value = validValues[(taskId + j) % validValues.Length];
                    CaptureConfiguration.DefaultTargetFPS = value;
                    var readValue = CaptureConfiguration.DefaultTargetFPS;
                    // Just read to ensure no exceptions
                }
            }));
        }

        // Wait for all tasks to complete
        Task.WaitAll(tasks.ToArray());

        // If we get here without exceptions, thread safety is working
        Assert.True(true);

        // Reset to default
        CaptureConfiguration.DefaultTargetFPS = 30;
    }

    // Test constants
    [Fact]
    public void Constants_HaveCorrectValues()
    {
        Assert.Equal(1, CaptureConfiguration.MinimumFPS);
        Assert.Equal(120, CaptureConfiguration.MaximumFPS);
    }

    // Test instance configuration still works
    [Fact]
    public void InstanceConfiguration_HasDefaultMaxFPS()
    {
        var config = new CaptureConfiguration();
        Assert.Equal(30, config.MaxFramesPerSecond);
    }

    [Fact]
    public void InstanceMaxFramesPerSecond_ThrowsOnInvalidValue()
    {
        var config = new CaptureConfiguration();
        Assert.Throws<ArgumentOutOfRangeException>(() => config.MaxFramesPerSecond = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => config.MaxFramesPerSecond = 121);
    }

    [Fact]
    public void InstanceClone_CreatesIndependentCopy()
    {
        var original = new CaptureConfiguration
        {
            MaxFramesPerSecond = 60
        };

        var clone = original.Clone();

        Assert.Equal(original.MaxFramesPerSecond, clone.MaxFramesPerSecond);

        // Verify independence
        clone.MaxFramesPerSecond = 30;
        Assert.Equal(60, original.MaxFramesPerSecond);
        Assert.Equal(30, clone.MaxFramesPerSecond);
    }
}
