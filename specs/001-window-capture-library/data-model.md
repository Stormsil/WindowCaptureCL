# Data Model: WindowCaptureCL

**Phase**: 1 - Design
**Date**: 2025-10-22
**Purpose**: Define key entities, their relationships, and validation rules for WindowCaptureCL library

## Overview

WindowCaptureCL uses a small, focused data model with 5 primary entities that represent capture operations, their results, and configuration. The model is designed for immutability where appropriate (CapturedFrame) and clear ownership semantics for resource management.

## Entity Definitions

### 1. CapturedFrame

**Purpose**: Represents a single captured image frame with associated metadata

**Properties**:

| Property | Type | Description | Validation |
|----------|------|-------------|------------|
| `Frame` | `System.Drawing.Bitmap` | The captured image data | NOT NULL, consumer owns and must dispose |
| `Timestamp` | `DateTime` | When the frame was captured (UTC) | Must be in the past or present |
| `SourceSize` | `System.Drawing.Size` | Original dimensions of capture source | Width > 0, Height > 0 |
| `FrameNumber` | `long` | Sequential frame number within session | >= 0, monotonically increasing |
| `CaptureSource` | `CaptureSourceInfo` | Information about what was captured | NOT NULL |

**Immutability**: CapturedFrame should be immutable after creation (read-only properties)

**Lifetime**: Created by CaptureSession, passed to consumer via event. Consumer responsible for disposing Bitmap when done.

**Usage Example**:
```csharp
void OnFrameReady(object sender, FrameReadyEventArgs e)
{
    CapturedFrame frame = e.Frame;
    Console.WriteLine($"Frame {frame.FrameNumber} captured at {frame.Timestamp}");
    Console.WriteLine($"Source: {frame.CaptureSource.SourceType}, Size: {frame.SourceSize}");

    // Process frame.Frame (Bitmap)
    // ...

    // Consumer must dispose
    frame.Frame.Dispose();
}
```

---

### 2. CaptureSourceInfo

**Purpose**: Describes the source of a capture operation (window, monitor, or region)

**Properties**:

| Property | Type | Description | Validation |
|----------|------|-------------|------------|
| `SourceType` | `CaptureSourceType` (enum) | Type of capture source | Must be valid enum value |
| `WindowHandle` | `IntPtr` | Window handle (if SourceType = Window) | NOT IntPtr.Zero if Window type |
| `MonitorIndex` | `int` | Monitor index (if SourceType = Monitor or Region) | >= 0 if Monitor/Region type |
| `Region` | `Rectangle?` | Region bounds (if SourceType = Region) | NOT NULL if Region type, must be valid rectangle |

**CaptureSourceType Enum**:
```csharp
public enum CaptureSourceType
{
    Window,
    Monitor,
    Region
}
```

**Validation Rules**:
- Window type: WindowHandle must be valid, MonitorIndex ignored, Region null
- Monitor type: MonitorIndex >= 0, WindowHandle ignored, Region null
- Region type: MonitorIndex >= 0, Region NOT NULL with Width > 0 and Height > 0, WindowHandle ignored

**Immutability**: Immutable after creation

---

### 3. ICaptureSession

**Purpose**: Public interface representing an active capture session with lifecycle management

**Properties**:

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `IsRunning` | `bool` | Read-only | Whether continuous capture is currently active |
| `SourceSize` | `Size` | Read-only | Dimensions of the capture source (can change if window resizes) |
| `TargetFPS` | `int` | Read/Write | Target frames per second for continuous capture |
| `CaptureSource` | `CaptureSourceInfo` | Read-only | Information about what is being captured |

**Methods**:

| Method | Return Type | Description | Exceptions |
|--------|-------------|-------------|------------|
| `Start()` | `void` | Begin continuous frame capture | `SessionAlreadyStartedException` if already running |
| `Stop()` | `void` | Stop continuous frame capture | No exception if not running |
| `TakeScreenshot()` | `Bitmap` | Capture single frame synchronously | `CaptureTargetInvalidException` if source no longer valid |
| `TakeScreenshotAsync()` | `Task<Bitmap>` | Capture single frame asynchronously | `CaptureTargetInvalidException` if source no longer valid |
| `Dispose()` | `void` | Release all resources, stop capture if running | (IDisposable pattern) |

**Events**:

| Event | Type | Description |
|-------|------|-------------|
| `FrameReady` | `EventHandler<FrameReadyEventArgs>` | Fired when a new frame is available during continuous capture |
| `CaptureStopped` | `EventHandler<CaptureStoppedEventArgs>` | Fired when capture stops (user Stop() or error) |

**State Transitions**:
```
Created → [Start()] → Running → [Stop()] → Stopped → [Start()] → Running
                   ↓                      ↓
              [Dispose()]            [Dispose()]
                   ↓                      ↓
               Disposed              Disposed
```

**Validation Rules**:
- `TargetFPS` setter: Must be between 1 and 120 (throw `ArgumentOutOfRangeException`)
- `Start()` when already running: Throw `SessionAlreadyStartedException`
- Any operation on disposed session: Throw `ObjectDisposedException`

---

### 4. FrameReadyEventArgs

**Purpose**: Event arguments for FrameReady event, carrying captured frame data

**Properties**:

| Property | Type | Description |
|----------|------|-------------|
| `Frame` | `CapturedFrame` | The captured frame with metadata |

**Inheritance**: Derives from `System.EventArgs`

**Usage**:
```csharp
public event EventHandler<FrameReadyEventArgs> FrameReady;

protected virtual void OnFrameReady(CapturedFrame frame)
{
    FrameReady?.Invoke(this, new FrameReadyEventArgs { Frame = frame });
}
```

---

### 5. CaptureStoppedEventArgs

**Purpose**: Event arguments for CaptureStopped event, indicating reason for stoppage

**Properties**:

| Property | Type | Description |
|----------|------|-------------|
| `Reason` | `CaptureStopReason` (enum) | Why capture stopped |
| `Exception` | `Exception?` | Exception that caused stop (if Reason = Error) |

**CaptureStopReason Enum**:
```csharp
public enum CaptureStopReason
{
    UserRequested,    // Stop() called by user
    SourceClosed,     // Window closed or monitor disconnected
    Error             // Unrecoverable error (see Exception property)
}
```

**Validation Rules**:
- If Reason = Error, Exception must NOT be null
- If Reason != Error, Exception should be null

---

### 6. CaptureConfiguration (Static Class)

**Purpose**: Global configuration settings for all capture operations

**Properties** (all static):

| Property | Type | Default | Description | Validation |
|----------|------|---------|-------------|------------|
| `IncludeCursor` | `bool` | `false` | Whether to include mouse cursor in captures | None (boolean) |
| `DrawBorder` | `bool` | `false` | Whether to draw border around captured window | None (boolean) |
| `DefaultTargetFPS` | `int` | `30` | Default FPS for new capture sessions | 1-120 range |
| `MinimumFPS` | `int` (const) | `1` | Minimum allowed FPS | Read-only constant |
| `MaximumFPS` | `int` (const) | `120` | Maximum allowed FPS | Read-only constant |

**Thread Safety**: All property getters/setters are thread-safe (lock-based)

**Validation**:
- `DefaultTargetFPS` setter: Throw `ArgumentOutOfRangeException` if < 1 or > 120

**Usage**:
```csharp
// At application startup
CaptureConfiguration.IncludeCursor = false;
CaptureConfiguration.DefaultTargetFPS = 60;

// Create session (uses current configuration)
var session = Capture.FromWindow(hwnd);
session.TargetFPS = CaptureConfiguration.DefaultTargetFPS;  // Explicitly set if needed
```

---

## Exception Hierarchy

**Base Exception**:
```csharp
public abstract class CaptureException : Exception
{
    protected CaptureException(string message) : base(message) { }
    protected CaptureException(string message, Exception innerException)
        : base(message, innerException) { }
}
```

**Derived Exceptions**:

| Exception | When Thrown | Properties |
|-----------|-------------|------------|
| `GraphicsCaptureNotSupportedException` | Windows version < 10.0.18362 or WGC not available | None |
| `CaptureTargetInvalidException` (abstract base) | Target window/monitor no longer exists | `object Target` (HWND or monitor index) |
| `WindowNotFoundException` : `CaptureTargetInvalidException` | Window handle invalid or closed | `IntPtr WindowHandle` |
| `InvalidMonitorException` : `CaptureTargetInvalidException` | Monitor index out of range or disconnected | `int MonitorIndex` |
| `RegionOutOfBoundsException` | Region extends beyond monitor bounds | `Rectangle AttemptedRegion`, `Size MonitorSize` |
| `SessionAlreadyStartedException` | Start() called on running session | None |
| `SessionNotStartedException` | Operation requires running session but not started | None |
| `GraphicsDeviceException` | DirectX device creation/operation failure | None (InnerException has COM error) |

**Validation Rules**:
- All exceptions must include meaningful message describing the error
- Exceptions wrapping COM errors must preserve InnerException
- Target-specific exceptions must include relevant identifying information (handle, index)

---

## Relationships & Ownership

```
┌─────────────────────┐
│  Capture (static)   │ Creates
│  ───────────────    │──────────┐
│ FromWindow()        │          │
│ FromScreen()        │          ▼
│ FromScreenRegion()  │    ┌──────────────────┐
└─────────────────────┘    │ ICaptureSession  │
                           │ ─────────────    │
                           │ IsRunning        │
                           │ TargetFPS        │──────Creates──────┐
                           │ Start()          │                   │
                           │ Stop()           │                   │
                           │ TakeScreenshot() │                   ▼
                           └──────────────────┘          ┌──────────────────┐
                                   │                     │ CapturedFrame    │
                                   │ Fires               │ ────────────     │
                                   ▼                     │ Frame (Bitmap)   │
                       ┌──────────────────────┐          │ Timestamp        │
                       │ FrameReadyEventArgs  │ Contains │ SourceSize       │
                       │ ──────────────────── │─────────►│ FrameNumber      │
                       │ Frame                │          │ CaptureSource    │
                       └──────────────────────┘          └──────────────────┘
                                                                   │
                                                                   │ References
                                                                   ▼
                                                          ┌──────────────────┐
                                                          │ CaptureSourceInfo│
                                                          │ ──────────────── │
                                                          │ SourceType       │
                                                          │ WindowHandle     │
                                                          │ MonitorIndex     │
                                                          │ Region           │
                                                          └──────────────────┘
```

**Ownership Semantics**:
1. **Capture (static)** creates `ICaptureSession` instances → Consumer disposes
2. **ICaptureSession** creates `CapturedFrame` instances → Consumer disposes `Bitmap` inside
3. **CapturedFrame** references `CaptureSourceInfo` → Immutable, no disposal needed
4. **Bitmap** in `CapturedFrame` → Consumer owns, must call `Dispose()`

---

## Validation Summary

**At Session Creation** (in Capture static methods):
- Window handle: `IsWindow(handle)` returns true
- Monitor index: >= 0 and < `Screen.AllScreens.Length`
- Region: Within monitor bounds (x, y, width, height validated)

**At Configuration Change**:
- TargetFPS: 1 <= value <= 120

**At Runtime**:
- Before each frame: Verify source still valid (window exists, monitor connected)
- If invalid: Fire `CaptureStopped` event with `SourceClosed` reason

**Resource Cleanup**:
- All sessions must be disposed (implements `IDisposable`)
- All captured Bitmaps must be disposed by consumer
- Dispose pattern: Stop capture → Release DirectX resources → Set disposed flag
