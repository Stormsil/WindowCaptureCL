namespace WindowCaptureCL;

public sealed partial class CaptureConfiguration
{
    /// <summary>
    /// Gets or sets the maximum frames per second for continuous capture.
    /// Default is 30 FPS.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is less than 1 or greater than 120.</exception>
    public int MaxFramesPerSecond
    {
        get => _maxFramesPerSecond;
        set
        {
            if (value < 1 || value > 120)
                throw new ArgumentOutOfRangeException(nameof(MaxFramesPerSecond), "MaxFramesPerSecond must be between 1 and 120.");
            _maxFramesPerSecond = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the cursor should be included in captures for this session.
    /// Default is the global <see cref="CaptureConfiguration.IncludeCursor"/> value.
    /// </summary>
    public bool SessionIncludeCursor
    {
        get => _includeCursor;
        set => _includeCursor = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to draw a border around captured windows for this session.
    /// Default is the global <see cref="CaptureConfiguration.DrawBorder"/> value.
    /// </summary>
    public bool SessionDrawBorder
    {
        get => _drawBorder;
        set => _drawBorder = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureConfiguration"/> class with default settings.
    /// Global defaults are applied for IncludeCursor and DrawBorder.
    /// </summary>
    public CaptureConfiguration()
    {
        lock (_staticLock)
        {
            _includeCursor = _globalIncludeCursor;
            _drawBorder = _globalDrawBorder;
            _maxFramesPerSecond = _globalDefaultTargetFPS;
        }
    }
}
