namespace WindowCaptureCL;

public sealed partial class CaptureConfiguration
{
    /// <summary>
    /// Creates a copy of this configuration.
    /// </summary>
    /// <returns>A new <see cref="CaptureConfiguration"/> instance with the same settings.</returns>
    public CaptureConfiguration Clone()
    {
        return new CaptureConfiguration
        {
            MaxFramesPerSecond = this.MaxFramesPerSecond,
            SessionIncludeCursor = this.SessionIncludeCursor,
            SessionDrawBorder = this.SessionDrawBorder
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
