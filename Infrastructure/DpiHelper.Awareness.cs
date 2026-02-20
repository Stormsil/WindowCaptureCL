namespace WindowCaptureCL.Infrastructure;

public static partial class DpiHelper
{
    public static bool SetPerMonitorDpiAwareness()
    {
        lock (_lock)
        {
            if (_dpiAwarenessSet)
                return true;

            try
            {
                if (SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2))
                {
                    _dpiAwarenessSet = true;
                    return true;
                }

                if (SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE))
                {
                    _dpiAwarenessSet = true;
                    return true;
                }
            }
            catch
            {
                // Function not available on older Windows versions.
            }

            try
            {
                var result = SetProcessDpiAwareness(PROCESS_PER_MONITOR_DPI_AWARE);
                if (result == 0)
                {
                    _dpiAwarenessSet = true;
                    return true;
                }
            }
            catch
            {
                // Function not available.
            }

            try
            {
                if (SetProcessDPIAware())
                {
                    _dpiAwarenessSet = true;
                    return true;
                }
            }
            catch
            {
                // Function not available.
            }

            return false;
        }
    }

    public static bool SetSystemDpiAwareness()
    {
        lock (_lock)
        {
            if (_dpiAwarenessSet)
                return true;

            try
            {
                if (SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_SYSTEM_AWARE))
                {
                    _dpiAwarenessSet = true;
                    return true;
                }
            }
            catch
            {
                // Function not available.
            }

            try
            {
                var result = SetProcessDpiAwareness(PROCESS_SYSTEM_DPI_AWARE);
                if (result == 0)
                {
                    _dpiAwarenessSet = true;
                    return true;
                }
            }
            catch
            {
                // Function not available.
            }

            try
            {
                if (SetProcessDPIAware())
                {
                    _dpiAwarenessSet = true;
                    return true;
                }
            }
            catch
            {
                // Function not available.
            }

            return false;
        }
    }
}
