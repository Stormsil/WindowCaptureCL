namespace WindowCaptureCL;

/// <summary>
/// Configuration settings for a capture session.
/// Provides both global default settings and per-session configuration.
/// </summary>
public sealed class CaptureConfiguration
{
    // T114: Private static lock for thread synchronization
    private static readonly object _staticLock = new();

    // T115-T117: Static backing fields for global defaults
    private static bool _globalIncludeCursor = false;
    private static bool _globalDrawBorder = false;
    private static int _globalDefaultTargetFPS = 30;

    // T118-T119: Constants for FPS validation
    /// <summary>
    /// The minimum allowed frames per second value.
    /// </summary>
    public const int MinimumFPS = 1;

    /// <summary>
    /// The maximum allowed frames per second value.
    /// </summary>
    public const int MaximumFPS = 120;

    // T120: Static constructor to initialize defaults
    static CaptureConfiguration()
    {
        _globalIncludeCursor = false;
        _globalDrawBorder = false;
        _globalDefaultTargetFPS = 30;
    }

    // T115: Global IncludeCursor property
    /// <summary>
    /// Gets or sets the global default value for cursor inclusion in captures.
    /// This setting is applied to all new capture sessions unless overridden.
    /// Default is false.
    /// </summary>
    public static bool IncludeCursor
    {
        get
        {
            lock (_staticLock)
            {
                return _globalIncludeCursor;
            }
        }
        set
        {
            lock (_staticLock)
            {
                _globalIncludeCursor = value;
            }
        }
    }

    // T116: Global DrawBorder property
    /// <summary>
    /// Gets or sets the global default value for drawing borders around captured windows.
    /// This setting is applied to all new capture sessions unless overridden.
    /// Default is false.
    /// </summary>
    public static bool DrawBorder
    {
        get
        {
            lock (_staticLock)
            {
                return _globalDrawBorder;
            }
        }
        set
        {
            lock (_staticLock)
            {
                _globalDrawBorder = value;
            }
        }
    }

    // T117: Global DefaultTargetFPS property with validation
    /// <summary>
    /// Gets or sets the global default target FPS for new capture sessions.
    /// Must be between <see cref="MinimumFPS"/> and <see cref="MaximumFPS"/>.
    /// Default is 30.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is less than 1 or greater than 120.</exception>
    public static int DefaultTargetFPS
    {
        get
        {
            lock (_staticLock)
            {
                return _globalDefaultTargetFPS;
            }
        }
        set
        {
            if (value < MinimumFPS || value > MaximumFPS)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(DefaultTargetFPS),
                    $"DefaultTargetFPS must be between {MinimumFPS} and {MaximumFPS}.");
            }

            lock (_staticLock)
            {
                _globalDefaultTargetFPS = value;
            }
        }
    }

    // Instance fields for per-session configuration
    private int _maxFramesPerSecond = 30;

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
    /// Initializes a new instance of the <see cref="CaptureConfiguration"/> class with default settings.
    /// </summary>
    public CaptureConfiguration()
    {
    }

    /// <summary>
    /// Creates a copy of this configuration.
    /// </summary>
    /// <returns>A new <see cref="CaptureConfiguration"/> instance with the same settings.</returns>
    public CaptureConfiguration Clone()
    {
        return new CaptureConfiguration
        {
            MaxFramesPerSecond = this.MaxFramesPerSecond
        };
    }

    /// <summary>
    /// Validates the configuration settings.
    /// </summary>
    /// <exception cref="InvalidConfigurationException">Thrown when the configuration is invalid.</exception>
    internal void Validate()
    {
        if (_maxFramesPerSecond < 1 || _maxFramesPerSecond > 120)
            throw new InvalidConfigurationException("MaxFramesPerSecond must be between 1 and 120.", nameof(MaxFramesPerSecond));
    }
}
