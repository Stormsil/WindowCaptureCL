# WindowCaptureCL API Reference

Complete API documentation for the WindowCaptureCL library.

## Overview

WindowCaptureCL is a high-performance .NET library for capturing screenshots from windows, monitors, and screen regions using the Windows Graphics Capture API. It provides both single-frame and continuous capture capabilities with hardware acceleration via DirectX 11.

**Namespace**: `WindowCaptureCL`
**Target Framework**: .NET 8.0-windows10.0.19041.0
**Platform Requirements**: Windows 10 version 1803 or later

---

## Static Entry Point: Capture Class

The `Capture` static class provides the main entry points for creating capture sessions.

### Methods

#### `Capture.FromWindow(IntPtr windowHandle)`

Creates a capture session for the specified window.

**Parameters:**
- `windowHandle` (IntPtr): The handle to the window to capture

**Returns:** `ICaptureSession` - A capture session for the window

**Exceptions:**
- `ArgumentException`: When `windowHandle` is `IntPtr.Zero`
- `WindowNotFoundException`: When the window handle is invalid or the window no longer exists
- `GraphicsCaptureNotSupportedException`: When Windows Graphics Capture is not supported on the system

**Example:**
```csharp
IntPtr hwnd = /* get window handle */;
using var session = Capture.FromWindow(hwnd);
var frame = session.CaptureFrame();
frame.Save("screenshot.png");
```

---

#### `Capture.FromScreen(int monitorIndex)`

Creates a capture session for a monitor.

**Parameters:**
- `monitorIndex` (int): Zero-based index of the monitor (0 = primary monitor)

**Returns:** `ICaptureSession` - A capture session for the monitor

**Exceptions:**
- `ArgumentOutOfRangeException`: When `monitorIndex` is negative
- `InvalidMonitorException`: When the monitor index is out of range
- `GraphicsCaptureNotSupportedException`: When Windows Graphics Capture is not supported

**Example:**
```csharp
// Capture primary monitor
using var session = Capture.FromScreen(0);
var frame = session.CaptureFrame();
frame.Save("monitor.png");
```

---

#### `Capture.FromScreenRegion(int monitorIndex, Rectangle region)`

Creates a capture session for a specific rectangular region on a monitor.

**Parameters:**
- `monitorIndex` (int): Zero-based index of the monitor
- `region` (Rectangle): The rectangular region to capture, relative to the monitor's top-left corner

**Returns:** `ICaptureSession` - A capture session for the region

**Exceptions:**
- `ArgumentOutOfRangeException`: When `monitorIndex` is negative
- `ArgumentException`: When `region` has invalid dimensions (width or height <= 0)
- `InvalidMonitorException`: When the monitor index is out of range
- `RegionOutOfBoundsException`: When the region extends beyond monitor bounds
- `GraphicsCaptureNotSupportedException`: When Windows Graphics Capture is not supported

**Example:**
```csharp
var region = new Rectangle(100, 100, 800, 600);
using var session = Capture.FromScreenRegion(0, region);
var frame = session.CaptureFrame();
frame.Save("region.png");
```

---

## Interface: ICaptureSession

Represents an active capture session for a window, monitor, or screen region. Implements `IDisposable`.

### Properties

#### `SourceInfo`
```csharp
CaptureSourceInfo SourceInfo { get; }
```
Gets information about the capture source (type, dimensions, handle/index).

---

#### `IsActive`
```csharp
bool IsActive { get; }
```
Gets whether the capture session is currently active (continuous capture running).

---

#### `Configuration`
```csharp
CaptureConfiguration Configuration { get; }
```
Gets the current configuration used by this capture session.

---

#### `TotalFramesCaptured`
```csharp
ulong TotalFramesCaptured { get; }
```
Gets the total number of frames captured since the session started.

---

### Events

#### `FrameReady`
```csharp
event EventHandler<FrameReadyEventArgs>? FrameReady
```
Raised when a new frame is ready in continuous capture mode. Subscribe to this event before calling `StartCapture()`.

**Event Args:** `FrameReadyEventArgs` with properties:
- `Frame` (Bitmap): The captured frame
- `Timestamp` (DateTime): When the frame was captured
- `FrameNumber` (ulong): Frame sequence number

**Example:**
```csharp
session.FrameReady += (sender, e) => {
    Console.WriteLine($"Frame {e.FrameNumber} captured at {e.Timestamp}");
    e.Frame.Save($"frame_{e.FrameNumber}.png");
};
```

---

#### `CaptureError`
```csharp
event EventHandler<CaptureErrorEventArgs>? CaptureError
```
Raised when an error occurs during capture.

**Event Args:** `CaptureErrorEventArgs` with properties:
- `Exception` (Exception): The exception that occurred
- `Timestamp` (DateTime): When the error occurred
- `CanContinue` (bool): Whether the capture session can continue

---

#### `CaptureStopped`
```csharp
event EventHandler<CaptureStoppedEventArgs>? CaptureStopped
```
Raised when the capture session stops.

**Event Args:** `CaptureStoppedEventArgs` with properties:
- `Reason` (CaptureStopReason): Why the capture stopped (UserRequested, SourceClosed, Error)
- `Exception` (Exception?): The exception if Reason is Error

---

### Methods

#### `StartCapture()`
```csharp
void StartCapture()
```
Starts continuous frame capture. Frames will be delivered via the `FrameReady` event at the rate specified by `Configuration.MaxFramesPerSecond`.

**Exceptions:**
- `InvalidCaptureStateException`: When the session is already active
- `ObjectDisposedException`: When the session has been disposed

**Example:**
```csharp
session.FrameReady += OnFrameReady;
session.StartCapture();
// Frames will be delivered to OnFrameReady event handler
```

---

#### `StopCapture()`
```csharp
void StopCapture()
```
Stops continuous frame capture.

**Exceptions:**
- `InvalidCaptureStateException`: When the session is not active
- `ObjectDisposedException`: When the session has been disposed

---

#### `CaptureFrame()`
```csharp
CapturedFrame CaptureFrame()
```
Captures a single frame immediately (synchronous).

**Returns:** `CapturedFrame` - The captured frame with bitmap and metadata

**Exceptions:**
- `FrameCaptureException`: When frame capture fails
- `ObjectDisposedException`: When the session has been disposed

**Example:**
```csharp
using var frame = session.CaptureFrame();
Console.WriteLine($"Captured {frame.Width}x{frame.Height} at {frame.Timestamp}");
frame.Save("screenshot.png");
```

---

#### `CaptureFrameAsync(CancellationToken cancellationToken = default)`
```csharp
Task<CapturedFrame> CaptureFrameAsync(CancellationToken cancellationToken = default)
```
Captures a single frame asynchronously.

**Parameters:**
- `cancellationToken` (CancellationToken): Optional cancellation token

**Returns:** `Task<CapturedFrame>` - Task containing the captured frame

**Exceptions:**
- `FrameCaptureException`: When frame capture fails
- `ObjectDisposedException`: When the session has been disposed
- `OperationCanceledException`: When the operation is canceled

**Example:**
```csharp
using var frame = await session.CaptureFrameAsync();
await Task.Run(() => frame.Save("screenshot.png"));
```

---

#### `UpdateConfiguration(CaptureConfiguration configuration)`
```csharp
void UpdateConfiguration(CaptureConfiguration configuration)
```
Updates the configuration for this capture session. Changes take effect immediately, even during active capture.

**Parameters:**
- `configuration` (CaptureConfiguration): The new configuration settings

**Exceptions:**
- `ArgumentNullException`: When `configuration` is null
- `InvalidConfigurationException`: When the configuration is invalid
- `ObjectDisposedException`: When the session has been disposed

**Example:**
```csharp
var config = new CaptureConfiguration { MaxFramesPerSecond = 60 };
session.UpdateConfiguration(config);
```

---

#### `Dispose()`
```csharp
void Dispose()
```
Releases all resources used by the capture session. Stops capture if active.

---

## Class: CaptureConfiguration

Configuration settings for a capture session. Provides both global default settings and per-session configuration.

### Static Properties (Global Defaults)

#### `IncludeCursor`
```csharp
static bool IncludeCursor { get; set; }
```
Global default for cursor inclusion in captures. Default: `false`.
Thread-safe.

---

#### `DrawBorder`
```csharp
static bool DrawBorder { get; set; }
```
Global default for drawing borders around captured windows. Default: `false`.
Thread-safe.

---

#### `DefaultTargetFPS`
```csharp
static int DefaultTargetFPS { get; set; }
```
Global default target FPS for new capture sessions. Must be between 1 and 120. Default: `30`.
Thread-safe.

**Exceptions:**
- `ArgumentOutOfRangeException`: When value is less than 1 or greater than 120

---

### Constants

#### `MinimumFPS`
```csharp
const int MinimumFPS = 1
```
The minimum allowed frames per second value.

---

#### `MaximumFPS`
```csharp
const int MaximumFPS = 120
```
The maximum allowed frames per second value.

---

### Instance Properties

#### `MaxFramesPerSecond`
```csharp
int MaxFramesPerSecond { get; set; }
```
Maximum frames per second for continuous capture. Default: `30`.

**Exceptions:**
- `ArgumentOutOfRangeException`: When value is less than 1 or greater than 120

---

### Methods

#### Constructor
```csharp
CaptureConfiguration()
```
Initializes a new instance with default settings.

---

#### `Clone()`
```csharp
CaptureConfiguration Clone()
```
Creates a copy of this configuration.

**Returns:** New `CaptureConfiguration` instance with the same settings

**Example:**
```csharp
var config1 = new CaptureConfiguration { MaxFramesPerSecond = 60 };
var config2 = config1.Clone();
```

---

## Class: CapturedFrame

Represents a single captured frame with associated metadata. Implements `IDisposable`.

### Properties

#### `Bitmap`
```csharp
Bitmap Bitmap { get; }
```
Gets the bitmap data of the captured frame.

---

#### `Timestamp`
```csharp
DateTime Timestamp { get; }
```
Gets the timestamp when the frame was captured.

---

#### `Width`
```csharp
int Width { get; }
```
Gets the width of the frame in pixels.

---

#### `Height`
```csharp
int Height { get; }
```
Gets the height of the frame in pixels.

---

### Methods

#### Constructor
```csharp
CapturedFrame(Bitmap bitmap, DateTime timestamp)
```
Creates a new captured frame.

**Parameters:**
- `bitmap` (Bitmap): The bitmap data
- `timestamp` (DateTime): When captured

**Exceptions:**
- `ArgumentNullException`: When `bitmap` is null

---

#### `Save(string filePath)`
```csharp
void Save(string filePath)
```
Saves the captured frame to a file. Format is determined by file extension.

**Parameters:**
- `filePath` (string): Path where the frame should be saved

**Exceptions:**
- `ArgumentNullException`: When `filePath` is null
- `ObjectDisposedException`: When the frame has been disposed

**Example:**
```csharp
frame.Save("screenshot.png");
frame.Save("screenshot.jpg");
```

---

#### `Save(string filePath, ImageFormat format)`
```csharp
void Save(string filePath, ImageFormat format)
```
Saves the captured frame with the specified format.

**Parameters:**
- `filePath` (string): Path where the frame should be saved
- `format` (ImageFormat): The image format (from System.Drawing.Imaging)

**Exceptions:**
- `ArgumentNullException`: When `filePath` is null
- `ObjectDisposedException`: When the frame has been disposed

**Example:**
```csharp
frame.Save("screenshot.bmp", System.Drawing.Imaging.ImageFormat.Bmp);
```

---

#### `Dispose()`
```csharp
void Dispose()
```
Releases all resources used by the frame (disposes the bitmap).

---

## Class: CaptureSourceInfo

Provides information about a capture source (window, monitor, or region).

### Properties

#### `Id`
```csharp
object Id { get; }
```
Unique identifier for the capture source. Type depends on source type:
- Window: `IntPtr` (window handle)
- Monitor: `int` (monitor index)
- Region: `(int monitorIndex, Rectangle region)` tuple

---

#### `SourceType`
```csharp
CaptureSourceType SourceType { get; }
```
The type of the capture source (Window, Monitor, or Region).

---

#### `DisplayName`
```csharp
string DisplayName { get; }
```
Display name of the capture source (e.g., "Window", "Monitor 0", "Region on Monitor 0").

---

#### `Width`
```csharp
int Width { get; }
```
Width of the capture source in pixels.

---

#### `Height`
```csharp
int Height { get; }
```
Height of the capture source in pixels.

---

#### `WindowHandle`
```csharp
IntPtr WindowHandle { get; }
```
Window handle if the source type is Window, otherwise `IntPtr.Zero`.

---

#### `MonitorIndex`
```csharp
int MonitorIndex { get; }
```
Monitor index if the source type is Monitor or Region, otherwise `-1`.

---

#### `Region`
```csharp
Rectangle Region { get; }
```
Capture region if the source type is Region, otherwise `Rectangle.Empty`.

---

## Enumerations

### CaptureSourceType

Specifies the type of capture source.

```csharp
public enum CaptureSourceType
{
    Window = 0,   // Capture source is a window
    Monitor = 1,  // Capture source is a full monitor/screen
    Region = 2    // Capture source is a region of a monitor/screen
}
```

---

### CaptureStopReason

Specifies why a capture session stopped.

```csharp
public enum CaptureStopReason
{
    UserRequested = 0,  // Stopped by calling Stop()
    SourceClosed = 1,   // Source (window/monitor) was closed or became invalid
    Error = 2           // Stopped due to an error
}
```

---

## Event Arguments

### FrameReadyEventArgs

Provides data for the `ICaptureSession.FrameReady` event.

**Properties:**
- `Frame` (Bitmap): The captured frame
- `Timestamp` (DateTime): When the frame was captured
- `FrameNumber` (ulong): Frame sequence number since session started

---

### CaptureErrorEventArgs

Provides data for the `ICaptureSession.CaptureError` event.

**Properties:**
- `Exception` (Exception): The exception that caused the error
- `Timestamp` (DateTime): When the error occurred
- `CanContinue` (bool): Whether the capture session can continue after this error

---

### CaptureStoppedEventArgs

Provides data for capture session stopped events.

**Properties:**
- `Reason` (CaptureStopReason): Why the capture stopped
- `Exception` (Exception?): The exception if Reason is Error, otherwise null

---

## Exceptions

All exceptions inherit from the base `CaptureException` class.

### GraphicsCaptureNotSupportedException

Thrown when Windows Graphics Capture API is not supported on the current system.

**Requirements:** Windows 10 version 1803 or later

---

### WindowNotFoundException

Thrown when a specified window handle is not found or is invalid.

**Properties:**
- `WindowHandle` (IntPtr): The window handle that was not found

---

### InvalidMonitorException

Thrown when a specified monitor index is invalid or out of range.

**Properties:**
- `MonitorIndex` (int): The invalid monitor index

---

### RegionOutOfBoundsException

Thrown when a specified capture region extends beyond monitor bounds.

**Properties:**
- `AttemptedRegion` (Rectangle): The region that was attempted
- `MonitorSize` (Size): The size of the monitor

---

### InvalidCaptureStateException

Thrown when the capture session is not in a valid state for the requested operation.

**Properties:**
- `CurrentState` (string?): The current state when the exception was thrown

**Common Scenarios:**
- Calling `StartCapture()` when already active
- Calling `StopCapture()` when not active

---

### FrameCaptureException

Thrown when frame capture operations fail.

**Common Causes:**
- DirectX device errors
- Graphics capture item closed
- Resource allocation failures

---

### InvalidConfigurationException

Thrown when configuration validation fails.

**Properties:**
- `PropertyName` (string?): The invalid property name

---

### DirectXException

Thrown when DirectX device initialization or operations fail.

**Properties:**
- `HResult` (int?): The HRESULT error code from the DirectX operation

---

### Other Exceptions

- `CaptureSourceNotFoundException`: Requested capture source could not be found
- `ResourceAllocationException`: Resource allocation or management failed
- `UnsupportedOperationException`: Operation not supported on current platform
- `GraphicsDeviceException`: Graphics device operation failed

---

## Usage Examples

### Example 1: Single Frame Capture (Window)

```csharp
using WindowCaptureCL;

IntPtr hwnd = /* get window handle */;

using var session = Capture.FromWindow(hwnd);
using var frame = session.CaptureFrame();

Console.WriteLine($"Captured {frame.Width}x{frame.Height} at {frame.Timestamp}");
frame.Save("screenshot.png");
```

---

### Example 2: Single Frame Capture (Monitor)

```csharp
using WindowCaptureCL;

// Capture primary monitor
using var session = Capture.FromScreen(0);
using var frame = session.CaptureFrame();
frame.Save("monitor.png");
```

---

### Example 3: Single Frame Capture (Region)

```csharp
using WindowCaptureCL;
using System.Drawing;

var region = new Rectangle(100, 100, 800, 600);
using var session = Capture.FromScreenRegion(0, region);
using var frame = session.CaptureFrame();
frame.Save("region.png");
```

---

### Example 4: Continuous Capture with Events

```csharp
using WindowCaptureCL;

IntPtr hwnd = /* get window handle */;
var session = Capture.FromWindow(hwnd);

// Configure FPS
var config = new CaptureConfiguration { MaxFramesPerSecond = 60 };
session.UpdateConfiguration(config);

// Subscribe to events
session.FrameReady += (sender, e) => {
    Console.WriteLine($"Frame {e.FrameNumber} captured");
    e.Frame.Save($"frames/frame_{e.FrameNumber:D6}.png");
};

session.CaptureError += (sender, e) => {
    Console.WriteLine($"Error: {e.Exception.Message}");
};

session.CaptureStopped += (sender, e) => {
    Console.WriteLine($"Capture stopped: {e.Reason}");
};

// Start continuous capture
session.StartCapture();

// Let it run...
Console.WriteLine("Press Enter to stop...");
Console.ReadLine();

// Stop and cleanup
session.StopCapture();
session.Dispose();
```

---

### Example 5: Async Frame Capture

```csharp
using WindowCaptureCL;

IntPtr hwnd = /* get window handle */;

using var session = Capture.FromWindow(hwnd);
using var frame = await session.CaptureFrameAsync();

await Task.Run(() => frame.Save("screenshot.png"));
Console.WriteLine("Frame saved asynchronously");
```

---

### Example 6: Error Handling

```csharp
using WindowCaptureCL;

try
{
    IntPtr hwnd = /* get window handle */;
    using var session = Capture.FromWindow(hwnd);
    using var frame = session.CaptureFrame();
    frame.Save("screenshot.png");
}
catch (GraphicsCaptureNotSupportedException)
{
    Console.WriteLine("Windows Graphics Capture not supported (requires Windows 10 1803+)");
}
catch (WindowNotFoundException ex)
{
    Console.WriteLine($"Window not found: {ex.WindowHandle}");
}
catch (FrameCaptureException ex)
{
    Console.WriteLine($"Failed to capture frame: {ex.Message}");
}
catch (CaptureException ex)
{
    Console.WriteLine($"Capture error: {ex.Message}");
}
```

---

### Example 7: Global Configuration

```csharp
using WindowCaptureCL;

// Set global defaults that apply to all new sessions
CaptureConfiguration.IncludeCursor = true;
CaptureConfiguration.DrawBorder = true;
CaptureConfiguration.DefaultTargetFPS = 60;

// Now all new sessions will use these defaults
using var session = Capture.FromScreen(0);
// This session will capture at 60 FPS with cursor and border
```

---

## Best Practices

1. **Always dispose resources**: Use `using` statements or call `Dispose()` on `ICaptureSession` and `CapturedFrame` objects.

2. **Handle exceptions**: Wrap capture operations in try-catch blocks, especially for `GraphicsCaptureNotSupportedException`, `WindowNotFoundException`, and `FrameCaptureException`.

3. **Monitor frame rate**: In continuous capture, the actual frame rate may be lower than `MaxFramesPerSecond` depending on system performance.

4. **Stop before dispose**: Call `StopCapture()` before disposing the session for clean shutdown.

5. **Event subscriptions**: Subscribe to `FrameReady` before calling `StartCapture()`.

6. **Thread safety**: The `FrameReady` event may be raised on a background thread. Use appropriate synchronization when updating UI.

7. **Memory management**: Dispose frames promptly in continuous capture to avoid memory buildup, especially at high frame rates.

8. **Configuration updates**: You can update configuration during active capture, but frequent changes may impact performance.

---

## Performance Considerations

- **Hardware acceleration**: The library uses DirectX 11 for hardware-accelerated capture.
- **FPS limits**: Respect the 1-120 FPS range for optimal performance.
- **Frame disposal**: Dispose frames promptly to release GPU memory.
- **Bitmap handling**: The `Bitmap` object in `CapturedFrame` is a managed wrapper around unmanaged resources.

---

## Platform Requirements

- **OS**: Windows 10 version 1803 (April 2018 Update) or later
- **Framework**: .NET 8.0-windows10.0.19041.0
- **Minimum Platform**: Windows 10 version 17763
- **Graphics**: DirectX 11 compatible graphics hardware
