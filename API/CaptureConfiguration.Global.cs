namespace WindowCaptureCL;

public sealed partial class CaptureConfiguration
{
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
}
