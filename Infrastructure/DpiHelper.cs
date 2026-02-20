namespace WindowCaptureCL.Infrastructure;

/// <summary>
/// Helper class for DPI awareness and scaling operations.
/// Provides methods to handle high-DPI displays correctly.
/// </summary>
public static partial class DpiHelper
{
    private const int DPI_AWARENESS_CONTEXT_UNAWARE = -1;
    private const int DPI_AWARENESS_CONTEXT_SYSTEM_AWARE = -2;
    private const int DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE = -3;
    private const int DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;

    private const int PROCESS_DPI_UNAWARE = 0;
    private const int PROCESS_SYSTEM_DPI_AWARE = 1;
    private const int PROCESS_PER_MONITOR_DPI_AWARE = 2;

    /// <summary>
    /// The default DPI value (96 DPI corresponds to 100% scaling).
    /// </summary>
    public const int DefaultDpi = 96;

    private static bool _dpiAwarenessSet;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets a value indicating whether DPI awareness has been set for the process.
    /// </summary>
    public static bool IsDpiAwarenessSet => _dpiAwarenessSet;
}
