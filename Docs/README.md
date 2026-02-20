# WindowCaptureCL - Quick Reference for AI Agents

High-performance .NET library for capturing screenshots using Windows Graphics Capture API.

## Quick Start

### Add to Project

```xml
<ItemGroup>
  <ProjectReference Include="path\to\WindowCaptureCL.csproj" />
</ItemGroup>
```

Or copy the library DLL and reference it directly.

### Requirements

- .NET 8.0 for Windows
- Windows 10 version 1803+ (April 2018 Update or later)
- DirectX 11 compatible graphics hardware

## Core Concepts

### 3 Capture Types

1. **Window Capture** - Capture a specific window by handle
2. **Monitor Capture** - Capture entire monitor by index (0 = primary)
3. **Region Capture** - Capture a rectangular region on a monitor

### 2 Capture Modes

1. **Single Frame** - One-time capture (sync or async)
2. **Continuous** - Event-driven stream at specified FPS (1-120)

## Essential API

### Creating Sessions

```csharp
using WindowCaptureCL;

// Window (requires IntPtr window handle)
var session = Capture.FromWindow(windowHandle);

// Monitor (0 = primary monitor)
var session = Capture.FromScreen(0);

// Region on monitor
var region = new Rectangle(x: 100, y: 100, width: 800, height: 600);
var session = Capture.FromScreenRegion(monitorIndex: 0, region);
```

### Single Frame Capture

```csharp
// Synchronous
using var frame = session.CaptureFrame();
frame.Save("screenshot.png");

// Asynchronous
using var frame = await session.CaptureFrameAsync();
frame.Save("screenshot.png");

// Frame properties: Bitmap, Timestamp, Width, Height
```

### Continuous Capture

```csharp
// Set FPS (default: 30)
var config = new CaptureConfiguration { MaxFramesPerSecond = 60 };
session.UpdateConfiguration(config);

// Subscribe to events BEFORE starting
session.FrameReady += (sender, e) => {
    // e.Frame (Bitmap), e.Timestamp, e.FrameNumber
    e.Frame.Save($"frame_{e.FrameNumber}.png");
};

session.CaptureError += (sender, e) => {
    // e.Exception, e.CanContinue
};

session.CaptureStopped += (sender, e) => {
    // e.Reason (UserRequested/SourceClosed/Error)
};

// Start/Stop
session.StartCapture();
// ... capture runs in background ...
session.StopCapture();
```

### Resource Management

```csharp
// ALWAYS dispose both sessions and frames
using var session = Capture.FromWindow(hwnd);
using var frame = session.CaptureFrame();

// Or manually
session.Dispose();
frame.Dispose();
```

## Common Patterns

### Pattern 1: Quick Screenshot

```csharp
using var session = Capture.FromScreen(0);
using var frame = session.CaptureFrame();
frame.Save("screenshot.png");
```

### Pattern 2: Window Capture with Error Handling

```csharp
try
{
    using var session = Capture.FromWindow(hwnd);
    using var frame = session.CaptureFrame();
    frame.Save("output.png");
}
catch (WindowNotFoundException)
{
    // Window not found or closed
}
catch (GraphicsCaptureNotSupportedException)
{
    // Windows version too old (need 1803+)
}
catch (FrameCaptureException ex)
{
    // Capture failed
}
```

### Pattern 3: Record Video Frames

```csharp
var session = Capture.FromScreen(0);
var config = new CaptureConfiguration { MaxFramesPerSecond = 30 };
session.UpdateConfiguration(config);

session.FrameReady += (s, e) => {
    e.Frame.Save($"frames/frame_{e.FrameNumber:D6}.png");
};

session.StartCapture();
Thread.Sleep(10000); // Record for 10 seconds
session.StopCapture();
session.Dispose();
```

### Pattern 4: Async Batch Capture

```csharp
using var session = Capture.FromWindow(hwnd);

for (int i = 0; i < 10; i++)
{
    using var frame = await session.CaptureFrameAsync();
    await Task.Run(() => frame.Save($"screenshot_{i}.png"));
    await Task.Delay(1000);
}
```

## Key Types Reference

### ICaptureSession

**Properties:**
- `IsActive` (bool) - Is continuous capture running?
- `Configuration` (CaptureConfiguration) - Current config
- `SourceInfo` (CaptureSourceInfo) - Source details
- `TotalFramesCaptured` (ulong) - Total frames captured

**Methods:**
- `CaptureFrame()` � CapturedFrame
- `CaptureFrameAsync()` � Task\<CapturedFrame\>
- `StartCapture()` - Begin continuous capture
- `StopCapture()` - End continuous capture
- `UpdateConfiguration(config)` - Change settings (even during capture)
- `Dispose()` - Cleanup

**Events:**
- `FrameReady` - New frame available (continuous mode)
- `CaptureError` - Error occurred
- `CaptureStopped` - Capture stopped

### CapturedFrame

**Properties:**
- `Bitmap` (Bitmap) - The image data
- `Timestamp` (DateTime) - Capture time
- `Width`, `Height` (int) - Dimensions

**Methods:**
- `Save(filePath)` - Save to file (auto-detect format)
- `Save(filePath, ImageFormat)` - Save with specific format
- `Dispose()` - Release bitmap

### CaptureConfiguration

**Instance:**
- `MaxFramesPerSecond` (int, 1-120, default: 30)

**Static (Global Defaults):**
- `IncludeCursor` (bool, default: false)
- `DrawBorder` (bool, default: false)
- `DefaultTargetFPS` (int, 1-120, default: 30)

## Common Exceptions

- `GraphicsCaptureNotSupportedException` - Windows version < 1803
- `WindowNotFoundException` - Invalid/closed window handle
- `InvalidMonitorException` - Invalid monitor index
- `RegionOutOfBoundsException` - Region extends beyond monitor
- `InvalidCaptureStateException` - Wrong state (e.g., StartCapture when already active)
- `FrameCaptureException` - Capture operation failed

## Important Notes for AI Agents

1. **Window Handles**: Use `WindowEnumerator` utility class (`WindowCaptureCL.Infrastructure.WGC` namespace) to find windows by title, process ID, or partial title. Alternatively, use Win32 APIs or `Process.MainWindowHandle`.

2. **Monitor Indexing**: Starts at 0. Primary monitor is always 0. Use `MonitorEnumerator` utility class (`WindowCaptureCL.Infrastructure.WGC` namespace) to enumerate all monitors and get detailed information.

3. **Regions**: Coordinates are relative to monitor's top-left corner (0,0).

4. **Thread Safety**: `FrameReady` event fires on background thread - use synchronization for UI updates.

5. **Memory**: Dispose frames promptly, especially in continuous capture at high FPS.

6. **FPS Reality**: Actual FPS may be lower than `MaxFramesPerSecond` depending on system load.

7. **Configuration Changes**: Can update config during active capture - changes apply immediately.

8. **Session State**: Can't start if already active, can't stop if not active.

9. **Bitmap Format**: Frames are 32-bit ARGB bitmaps.

10. **Platform Check**: Always handle `GraphicsCaptureNotSupportedException` for compatibility.

## Finding Windows and Monitors

WindowCaptureCL provides built-in utilities to discover windows and monitors before capturing.

### Finding Windows

```csharp
using WindowCaptureCL.Infrastructure.WGC;

// Method 1: By window title (exact match)
var windowInfo = WindowEnumerator.FindWindowByTitle("Calculator");
if (windowInfo != null)
{
    Console.WriteLine($"Found: {windowInfo.Title} ({windowInfo.Width}x{windowInfo.Height})");
    using var session = Capture.FromWindow(windowInfo.Handle);
    using var frame = session.CaptureFrame();
    frame.Save("calculator.png");
}

// Method 2: By partial title (case-insensitive)
var windows = WindowEnumerator.FindWindowsByPartialTitle("Visual Studio");
foreach (var window in windows)
{
    Console.WriteLine($"Found: {window.Title}");
}

// Method 3: By process ID
var process = Process.GetProcessesByName("notepad").FirstOrDefault();
if (process != null)
{
    var windowInfo = WindowEnumerator.FindWindowByProcessId(process.Id);
    if (windowInfo != null)
    {
        using var session = Capture.FromWindow(windowInfo.Handle);
        // Capture notepad window
    }
}

// Method 4: Validate existing window handle
IntPtr hwnd = /* your handle */;
var windowInfo = WindowEnumerator.FindWindow(hwnd);
if (windowInfo != null)
{
    // Window is valid and capturable
}
```

**WindowInfo Properties:**
- `Handle` (IntPtr) - Window handle for use with `Capture.FromWindow()`
- `Title` (string) - Window title
- `Width`, `Height` (int) - Window dimensions

### Finding Monitors

```csharp
using WindowCaptureCL.Infrastructure.WGC;

// Method 1: Get all monitors
var monitors = MonitorEnumerator.GetAllMonitors();
Console.WriteLine($"Found {monitors.Count} monitor(s)");
foreach (var monitor in monitors)
{
    Console.WriteLine($"{monitor.DeviceName}: {monitor.Width}x{monitor.Height} (Primary: {monitor.IsPrimary})");
}

// Method 2: Get primary monitor
var primaryMonitor = MonitorEnumerator.GetPrimaryMonitor();
if (primaryMonitor != null)
{
    Console.WriteLine($"Primary: {primaryMonitor.Width}x{primaryMonitor.Height}");
    using var session = Capture.FromScreen(0); // Primary is always index 0
}

// Method 3: Get specific monitor by index
var monitor = MonitorEnumerator.GetMonitorByIndex(1);
if (monitor != null)
{
    Console.WriteLine($"Monitor 1: {monitor.DeviceName}");
    using var session = Capture.FromScreen(1);
}

// Method 4: Get monitor by device name
var monitor = MonitorEnumerator.GetMonitorByDeviceName("\\\\.\\DISPLAY2");
if (monitor != null)
{
    // Found specific monitor
}
```

**MonitorInfo Properties:**
- `Handle` (IntPtr) - Monitor handle (HMONITOR)
- `DeviceName` (string) - Device name like "\\\\.\\DISPLAY1"
- `Width`, `Height` (int) - Monitor dimensions
- `IsPrimary` (bool) - Whether this is the primary monitor

### Alternative Methods (Without Built-in Utilities)

```csharp
// Getting window handles from Process
var process = Process.GetProcessesByName("notepad")[0];
IntPtr hwnd = process.MainWindowHandle;

// Using Win32 API directly
[DllImport("user32.dll")]
static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
IntPtr hwnd = FindWindow(null, "Untitled - Notepad");

// Using System.Windows.Forms.Screen for monitors (requires WindowsForms reference)
var screens = Screen.AllScreens;
Console.WriteLine($"Monitor count: {screens.Length}");
// Note: WindowCaptureCL uses index: 0 = primary, 1, 2, 3... = additional
```

## Complete Example

```csharp
using WindowCaptureCL;
using System;
using System.Drawing;

class Program
{
    static void Main()
    {
        try
        {
            // Capture primary monitor
            using var session = Capture.FromScreen(0);

            Console.WriteLine($"Capturing: {session.SourceInfo.DisplayName}");
            Console.WriteLine($"Size: {session.SourceInfo.Width}x{session.SourceInfo.Height}");

            // Single frame
            using var frame = session.CaptureFrame();
            frame.Save("monitor_capture.png");
            Console.WriteLine($"Saved {frame.Width}x{frame.Height} frame");

            // Continuous capture at 30 FPS for 5 seconds
            int frameCount = 0;
            session.FrameReady += (s, e) => {
                Console.WriteLine($"Frame {e.FrameNumber} at {e.Timestamp:HH:mm:ss.fff}");
                frameCount++;
            };

            session.StartCapture();
            Thread.Sleep(5000);
            session.StopCapture();

            Console.WriteLine($"Captured {frameCount} frames (expected ~150 at 30fps)");
        }
        catch (GraphicsCaptureNotSupportedException)
        {
            Console.WriteLine("ERROR: Requires Windows 10 version 1803 or later");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }
    }
}
```

## See Also

For complete API documentation with all methods, properties, exceptions, and detailed examples, see **API_REFERENCE.md**.
