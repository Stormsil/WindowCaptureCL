# API Contract: WindowCaptureCL Public Interface

**Phase**: 1 - Design
**Date**: 2025-10-22
**Purpose**: Define the complete public API surface for WindowCaptureCL library

## API Overview

WindowCaptureCL exposes a minimal, intuitive API through 5 public classes/interfaces:
1. `Capture` (static facade - entry point)
2. `ICaptureSession` (interface for capture operations)
3. `CaptureConfiguration` (static global settings)
4. `CapturedFrame` (immutable captured data)
5. Exception types

---

## 1. Capture Class (Static Facade)

**Namespace**: `WindowCaptureCL`

**Purpose**: Single entry point for creating capture sessions

### Methods

#### FromWindow
```csharp
public static ICaptureSession FromWindow(IntPtr windowHandle)
```

**Description**: Creates a capture session for a specific window identified by its handle

**Parameters**:
- `windowHandle` (IntPtr): Native window handle (HWND) of the target window

**Returns**: `ICaptureSession` configured to capture the specified window

**Exceptions**:
- `ArgumentException`: If windowHandle is IntPtr.Zero
- `WindowNotFoundException`: If the window handle is invalid or window does not exist
- `GraphicsCaptureNotSupportedException`: If Windows version does not support WGC API

**Example**:
```csharp
IntPtr hwnd = FindWindow(null, "Notepad");
using (var session = Capture.FromWindow(hwnd))
{
    session.Start();
    // ... handle frames
}
```

---

#### FromScreen
```csharp
public static ICaptureSession FromScreen(int monitorIndex)
```

**Description**: Creates a capture session for an entire monitor

**Parameters**:
- `monitorIndex` (int): Zero-based index of the monitor (0 = primary monitor)

**Returns**: `ICaptureSession` configured to capture the entire specified monitor

**Exceptions**:
- `ArgumentOutOfRangeException`: If monitorIndex < 0
- `InvalidMonitorException`: If monitorIndex >= number of connected monitors
- `GraphicsCaptureNotSupportedException`: If Windows version does not support WGC API

**Example**:
```csharp
// Capture primary monitor
using (var session = Capture.FromScreen(0))
{
    Bitmap screenshot = session.TakeScreenshot();
    screenshot.Save("monitor.png");
    screenshot.Dispose();
}
```

---

#### FromScreenRegion
```csharp
public static ICaptureSession FromScreenRegion(int monitorIndex, Rectangle region)
```

**Description**: Creates a capture session for a specific rectangular region on a monitor

**Parameters**:
- `monitorIndex` (int): Zero-based index of the monitor containing the region
- `region` (Rectangle): The rectangular area to capture (in screen coordinates)

**Returns**: `ICaptureSession` configured to capture the specified region

**Exceptions**:
- `ArgumentOutOfRangeException`: If monitorIndex < 0
- `InvalidMonitorException`: If monitorIndex >= number of connected monitors
- `RegionOutOfBoundsException`: If region extends beyond monitor bounds
- `ArgumentException`: If region has Width <= 0 or Height <= 0
- `GraphicsCaptureNotSupportedException`: If Windows version does not support WGC API

**Example**:
```csharp
// Capture top-left 800x600 area of primary monitor
var region = new Rectangle(0, 0, 800, 600);
using (var session = Capture.FromScreenRegion(0, region))
{
    session.TargetFPS = 30;
    session.FrameReady += (s, e) => ProcessFrame(e.Frame);
    session.Start();
    Thread.Sleep(10000);  // Capture for 10 seconds
    session.Stop();
}
```

---

## 2. ICaptureSession Interface

**Namespace**: `WindowCaptureCL`

**Purpose**: Represents an active capture session with lifecycle management

### Properties

#### IsRunning
```csharp
bool IsRunning { get; }
```

**Description**: Gets whether continuous capture is currently active

**Thread-safe**: Yes (read-only)

---

#### SourceSize
```csharp
Size SourceSize { get; }
```

**Description**: Gets the current dimensions of the capture source. May change if window is resized during capture.

**Thread-safe**: Yes (read-only)

**Note**: For region captures, this returns the region size, not the source window/monitor size

---

#### TargetFPS
```csharp
int TargetFPS { get; set; }
```

**Description**: Gets or sets the target frames per second for continuous capture. Frames will be delivered at approximately this rate.

**Default**: Value from `CaptureConfiguration.DefaultTargetFPS` (30)

**Valid Range**: 1 to 120

**Thread-safe**: Yes (uses lock)

**Exceptions (setter)**:
- `ArgumentOutOfRangeException`: If value < 1 or value > 120

**Note**: Changing this while capture is running will affect subsequent frames. Does not apply to `TakeScreenshot()` methods.

---

#### CaptureSource
```csharp
CaptureSourceInfo CaptureSource { get; }
```

**Description**: Gets information about what is being captured (type, handle/index, region)

**Thread-safe**: Yes (immutable after creation)

---

### Methods

#### Start
```csharp
void Start()
```

**Description**: Begins continuous frame capture. Frames will be delivered via the `FrameReady` event at the rate specified by `TargetFPS`.

**Exceptions**:
- `SessionAlreadyStartedException`: If capture is already running (`IsRunning` is true)
- `ObjectDisposedException`: If session has been disposed
- `CaptureTargetInvalidException`: If the capture target is no longer valid (window closed, monitor disconnected)

**Thread-safe**: Yes

**Example**:
```csharp
session.FrameReady += OnFrameReady;
session.Start();
```

---

#### Stop
```csharp
void Stop()
```

**Description**: Stops continuous frame capture. No-op if capture is not running. The `CaptureStopped` event will be fired with reason `UserRequested`.

**Thread-safe**: Yes

**Example**:
```csharp
session.Stop();
```

---

#### TakeScreenshot
```csharp
Bitmap TakeScreenshot()
```

**Description**: Captures a single frame synchronously. Can be called whether continuous capture is running or not.

**Returns**: `Bitmap` containing the captured frame. Caller is responsible for disposing.

**Exceptions**:
- `ObjectDisposedException`: If session has been disposed
- `CaptureTargetInvalidException`: If the capture target is no longer valid
- `GraphicsDeviceException`: If GPU/DirectX operation fails

**Performance**: Typically completes in <100ms for 1080p captures on typical hardware.

**Thread-safe**: Yes

**Example**:
```csharp
using (Bitmap screenshot = session.TakeScreenshot())
{
    screenshot.Save("capture.png", ImageFormat.Png);
}
```

---

#### TakeScreenshotAsync
```csharp
Task<Bitmap> TakeScreenshotAsync()
```

**Description**: Captures a single frame asynchronously. Can be called whether continuous capture is running or not.

**Returns**: `Task<Bitmap>` that completes when frame is captured. Caller is responsible for disposing the Bitmap.

**Exceptions**: Same as `TakeScreenshot()`, thrown from awaited task

**Thread-safe**: Yes

**Example**:
```csharp
Bitmap screenshot = await session.TakeScreenshotAsync();
try
{
    await SaveToFileAsync(screenshot, "capture.png");
}
finally
{
    screenshot.Dispose();
}
```

---

#### Dispose
```csharp
void Dispose()
```

**Description**: Releases all resources associated with the capture session. Stops capture if running. After disposal, all operations will throw `ObjectDisposedException`.

**Implementation**: `IDisposable` pattern

**Thread-safe**: Yes (idempotent - can be called multiple times safely)

**Example**:
```csharp
using (var session = Capture.FromWindow(hwnd))
{
    // ... use session
}  // Automatically disposed
```

---

### Events

#### FrameReady
```csharp
event EventHandler<FrameReadyEventArgs> FrameReady
```

**Description**: Fired when a new frame is available during continuous capture. Fires at approximately `TargetFPS` rate.

**Event Args**: `FrameReadyEventArgs` containing the `CapturedFrame`

**Thread**: May be fired on a background thread. If UI updates are needed, marshal to UI thread.

**Important**: Consumer must dispose the `Bitmap` in `CapturedFrame.Frame` when done processing.

**Example**:
```csharp
session.FrameReady += (sender, e) =>
{
    CapturedFrame frame = e.Frame;
    Console.WriteLine($"Frame {frame.FrameNumber} at {frame.Timestamp}");

    // Process frame.Frame (Bitmap)
    ProcessBitmap(frame.Frame);

    // Must dispose
    frame.Frame.Dispose();
};
```

---

#### CaptureStopped
```csharp
event EventHandler<CaptureStoppedEventArgs> CaptureStopped
```

**Description**: Fired when continuous capture stops, either by user request (`Stop()`) or due to error/source closure.

**Event Args**: `CaptureStoppedEventArgs` containing reason and optional exception

**Example**:
```csharp
session.CaptureStopped += (sender, e) =>
{
    switch (e.Reason)
    {
        case CaptureStopReason.UserRequested:
            Console.WriteLine("User stopped capture");
            break;
        case CaptureStopReason.SourceClosed:
            Console.WriteLine("Capture source closed");
            break;
        case CaptureStopReason.Error:
            Console.WriteLine($"Error: {e.Exception.Message}");
            break;
    }
};
```

---

## 3. CaptureConfiguration Class (Static)

**Namespace**: `WindowCaptureCL`

**Purpose**: Global configuration settings applied to all capture operations

### Properties

#### IncludeCursor
```csharp
public static bool IncludeCursor { get; set; }
```

**Description**: Gets or sets whether the mouse cursor should be included in captured frames

**Default**: `false`

**Thread-safe**: Yes

**Applies to**: All capture sessions (window, monitor, region)

**Example**:
```csharp
CaptureConfiguration.IncludeCursor = true;  // Show cursor in captures
var session = Capture.FromWindow(hwnd);
// Captures will include cursor
```

---

#### DrawBorder
```csharp
public static bool DrawBorder { get; set; }
```

**Description**: Gets or sets whether a border should be drawn around captured windows (visual indicator)

**Default**: `false`

**Thread-safe**: Yes

**Applies to**: Window captures only (ignored for monitor/region captures)

---

#### DefaultTargetFPS
```csharp
public static int DefaultTargetFPS { get; set; }
```

**Description**: Gets or sets the default target FPS for new capture sessions

**Default**: `30`

**Valid Range**: 1 to 120

**Thread-safe**: Yes

**Exceptions (setter)**:
- `ArgumentOutOfRangeException`: If value < 1 or value > 120

**Example**:
```csharp
CaptureConfiguration.DefaultTargetFPS = 60;  // New sessions default to 60 FPS
var session = Capture.FromWindow(hwnd);
Console.WriteLine(session.TargetFPS);  // Outputs: 60
```

---

#### MinimumFPS (constant)
```csharp
public const int MinimumFPS = 1;
```

**Description**: Minimum allowed FPS value

---

#### MaximumFPS (constant)
```csharp
public const int MaximumFPS = 120;
```

**Description**: Maximum allowed FPS value

---

## 4. CapturedFrame Class

**Namespace**: `WindowCaptureCL`

**Purpose**: Immutable container for a captured frame with metadata

### Properties

#### Frame
```csharp
public Bitmap Frame { get; }
```

**Description**: The captured image data

**Ownership**: Caller owns this Bitmap and must dispose it when done

---

#### Timestamp
```csharp
public DateTime Timestamp { get; }
```

**Description**: UTC timestamp when the frame was captured

---

#### SourceSize
```csharp
public Size SourceSize { get; }
```

**Description**: Dimensions of the capture source at time of capture

---

#### FrameNumber
```csharp
public long FrameNumber { get; }
```

**Description**: Sequential frame number within this capture session (starts at 0)

---

#### CaptureSource
```csharp
public CaptureSourceInfo CaptureSource { get; }
```

**Description**: Information about what was captured

---

## 5. Supporting Types

### CaptureSourceInfo
```csharp
public class CaptureSourceInfo
{
    public CaptureSourceType SourceType { get; }
    public IntPtr WindowHandle { get; }  // Valid if SourceType == Window
    public int MonitorIndex { get; }     // Valid if SourceType == Monitor or Region
    public Rectangle? Region { get; }    // Valid if SourceType == Region
}
```

### CaptureSourceType Enum
```csharp
public enum CaptureSourceType
{
    Window,
    Monitor,
    Region
}
```

### CaptureStopReason Enum
```csharp
public enum CaptureStopReason
{
    UserRequested,   // Stop() called
    SourceClosed,    // Window/monitor no longer available
    Error            // Unrecoverable error
}
```

### FrameReadyEventArgs
```csharp
public class FrameReadyEventArgs : EventArgs
{
    public CapturedFrame Frame { get; }
}
```

### CaptureStoppedEventArgs
```csharp
public class CaptureStoppedEventArgs : EventArgs
{
    public CaptureStopReason Reason { get; }
    public Exception Exception { get; }  // Non-null if Reason == Error
}
```

---

## 6. Exception Types

All exceptions inherit from base `CaptureException`:

### CaptureException (abstract)
```csharp
public abstract class CaptureException : Exception
```

Base class for all library-specific exceptions

---

### GraphicsCaptureNotSupportedException
```csharp
public class GraphicsCaptureNotSupportedException : CaptureException
```

**Thrown when**: Windows version does not support Windows Graphics Capture API (< Windows 10 1903)

---

### CaptureTargetInvalidException (abstract)
```csharp
public abstract class CaptureTargetInvalidException : CaptureException
```

Base class for exceptions related to invalid capture targets

---

### WindowNotFoundException
```csharp
public class WindowNotFoundException : CaptureTargetInvalidException
{
    public IntPtr WindowHandle { get; }
}
```

**Thrown when**: Window handle is invalid or window has closed

---

### InvalidMonitorException
```csharp
public class InvalidMonitorException : CaptureTargetInvalidException
{
    public int MonitorIndex { get; }
}
```

**Thrown when**: Monitor index is out of range or monitor disconnected

---

### RegionOutOfBoundsException
```csharp
public class RegionOutOfBoundsException : CaptureException
{
    public Rectangle AttemptedRegion { get; }
    public Size MonitorSize { get; }
}
```

**Thrown when**: Region extends beyond monitor bounds

---

### SessionAlreadyStartedException
```csharp
public class SessionAlreadyStartedException : CaptureException
```

**Thrown when**: `Start()` called on already-running session

---

### SessionNotStartedException
```csharp
public class SessionNotStartedException : CaptureException
```

**Thrown when**: Operation requires running session but not started

---

### GraphicsDeviceException
```csharp
public class GraphicsDeviceException : CaptureException
```

**Thrown when**: DirectX device creation or operation fails. `InnerException` contains COM error details.

---

## API Usage Patterns

### Pattern 1: Single Screenshot
```csharp
IntPtr hwnd = GetWindowHandle();
using (var session = Capture.FromWindow(hwnd))
{
    using (Bitmap screenshot = session.TakeScreenshot())
    {
        screenshot.Save("window.png");
    }
}
```

### Pattern 2: Continuous Capture
```csharp
var session = Capture.FromWindow(hwnd);
session.TargetFPS = 30;

session.FrameReady += (s, e) =>
{
    // Process frame
    ProcessFrame(e.Frame.Frame);

    // Dispose bitmap
    e.Frame.Frame.Dispose();
};

session.CaptureStopped += (s, e) =>
{
    if (e.Reason == CaptureStopReason.Error)
    {
        Console.WriteLine($"Error: {e.Exception.Message}");
    }
};

session.Start();

// ... do other work

session.Stop();
session.Dispose();
```

### Pattern 3: Async Screenshot
```csharp
using (var session = Capture.FromScreen(0))
{
    Bitmap screenshot = await session.TakeScreenshotAsync();
    try
    {
        await SaveAsync(screenshot);
    }
    finally
    {
        screenshot.Dispose();
    }
}
```

---

## Thread Safety Guarantees

- ✅ **All public methods**: Thread-safe
- ✅ **Property getters/setters**: Thread-safe (lock-based)
- ✅ **Event subscriptions**: Thread-safe
- ⚠️ **Event handlers**: May fire on background thread, caller must handle marshaling if needed
- ✅ **Dispose**: Idempotent and thread-safe

---

## Performance Characteristics

| Operation | Typical Latency | Notes |
|-----------|-----------------|-------|
| `FromWindow()` | <10ms | Session creation, no capture yet |
| `TakeScreenshot()` (1080p) | <100ms | Includes GPU-to-CPU transfer |
| Frame delivery (60 FPS) | ~16ms intervals | ±10% jitter expected |
| `Dispose()` | <50ms | Waits for pending frames to complete |

---

## Versioning & Compatibility

**Library Version**: 1.0.0 (initial release)

**Breaking Change Policy**: Follows semantic versioning
- Major: Breaking API changes
- Minor: New features, backward compatible
- Patch: Bug fixes

**Minimum Requirements**:
- Windows 10 version 1903 (build 18362) or Windows 11
- .NET 6.0 or later
- DirectX 11-compatible GPU
