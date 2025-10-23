# Technical Research: WindowCaptureCL

**Phase**: 0 - Research & Technical Decisions
**Date**: 2025-10-22
**Purpose**: Resolve technical unknowns and document technology choices for WindowCaptureCL library

## Executive Summary

This research validates the feasibility of building a high-performance screen capture library using Windows Graphics Capture (WGC) API. Key findings:
1. WGC provides hardware-accelerated capture with native support for obscured windows
2. Vortice.Windows provides production-ready DirectX interop for .NET
3. FPS throttling must occur at GPU level before CPU transfer to meet performance goals
4. .NET 8 with `net8.0-windows` TFM is required for WGC access (supersedes .NET Standard 2.0 requirement)

## Research Areas

### 1. Windows Graphics Capture API Capabilities

**Decision**: Use Windows Graphics Capture (WGC) API exclusively as the capture mechanism

**Rationale**:
- **Hardware Acceleration**: WGC leverages DirectX 11 for GPU-based capture, significantly reducing CPU overhead compared to legacy GDI methods (BitBlt)
- **Obscured Window Support**: WGC can capture windows even when fully covered by other windows (critical requirement from spec FR-007)
- **Modern API**: Introduced in Windows 10 1903, actively maintained by Microsoft, designed for performance scenarios
- **Frame Pool Architecture**: Built-in frame pooling reduces memory allocation overhead for continuous capture

**Alternatives Considered**:
1. **Desktop Duplication API (DXGI)**:
   - ❌ Rejected: Cannot capture individual windows, only entire outputs (monitors)
   - ❌ Rejected: Cannot capture obscured windows
   - ✓ Use case: Could be fallback for full-screen capture, but adds complexity without clear benefit

2. **GDI BitBlt/PrintWindow**:
   - ❌ Rejected: CPU-bound, poor performance for 60 FPS scenarios
   - ❌ Rejected: Does not meet constitutional requirement for hardware-accelerated APIs (Principle II)
   - ❌ Rejected: PrintWindow cannot capture DX/Vulkan game windows reliably

3. **Win32 Magnification API**:
   - ❌ Rejected: Designed for accessibility, not programmatic capture
   - ❌ Rejected: Performance overhead, requires visible magnifier window

**Implementation Notes**:
- WGC requires `Windows.Graphics.Capture.GraphicsCaptureItem` creation via interop from HWND/HMONITOR
- Frame delivery via `Direct3D11CaptureFramePool` event-based model aligns with spec requirements
- Cursor inclusion/exclusion controlled via `GraphicsCaptureSession.IsCursorCaptureEnabled` property

**References**:
- [Microsoft Docs: Windows.Graphics.Capture Namespace](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture)
- [Screen Capture Sample (Microsoft)](https://github.com/microsoft/Windows-universal-samples/tree/main/Samples/ScreenCaptureforHWND)

---

### 2. DirectX Interop Library Selection

**Decision**: Use Vortice.Windows (specifically Vortice.Direct3D11 and Vortice.Win32) for DirectX interop

**Rationale**:
- **Modern & Maintained**: Active development, .NET 5+ focused, frequent updates
- **Complete Coverage**: Provides full Direct3D11 API surface needed for texture manipulation
- **Type-Safe**: C# wrappers with proper COM lifetime management, reduces memory leaks
- **Performance**: Zero-overhead abstractions over native COM interfaces
- **Community Adoption**: Used in production by game engines (Stride), graphics applications

**Alternatives Considered**:
1. **SharpDX**:
   - ❌ Rejected: Project archived/unmaintained since 2019
   - ❌ Rejected: Does not support .NET 5+ properly
   - Historical note: SharpDX was the standard choice, but Vortice is its spiritual successor

2. **Direct P/Invoke to d3d11.dll**:
   - ❌ Rejected: Requires manual COM lifetime management (error-prone)
   - ❌ Rejected: Significant development overhead to wrap all necessary APIs
   - ❌ Rejected: No type safety, easy to introduce memory leaks

3. **TerraFX.Interop.Windows**:
   - ⚠️ Considered: Auto-generated bindings, very complete
   - ❌ Rejected: Lower-level than Vortice, less ergonomic C# API
   - ❌ Rejected: Smaller community, less documentation

**Required NuGet Packages**:
- `Vortice.Direct3D11` (latest stable): For D3D11Device, D3D11Texture2D manipulation
- `Vortice.Win32` (latest stable): For DXGI interop with WGC frame pool

**Implementation Notes**:
- Vortice provides `ID3D11Device` wrapper for device creation
- Texture mapping for CPU read: `ID3D11DeviceContext.Map()` / `Unmap()`
- Must create staging texture (CPU-accessible) to copy GPU frame data

---

### 3. GPU-to-CPU Texture Transfer & Bitmap Conversion

**Decision**: Use staging texture pattern with explicit GPU-to-CPU copy for Bitmap conversion

**Rationale**:
- **Staging Texture Required**: WGC provides GPU textures (D3D11_USAGE_DEFAULT), must copy to CPU-accessible staging texture (D3D11_USAGE_STAGING) for Bitmap conversion
- **Explicit Copy**: Allows measurement and optimization of transfer cost
- **Bitmap Compatibility**: System.Drawing.Bitmap requires CPU memory, no GPU-backed bitmap support

**Process Flow**:
1. WGC delivers `IDirect3DSurface` (GPU texture) via frame pool event
2. Query for `ID3D11Texture2D` from surface
3. Create/reuse staging texture with `D3D11_USAGE_STAGING` and `D3D11_CPU_ACCESS_READ`
4. `ID3D11DeviceContext.CopyResource()` from GPU to staging
5. `Map()` staging texture to get CPU pointer
6. Copy pixel data to `Bitmap` (stride-aware, handle padding)
7. `Unmap()` and dispose frame

**Performance Considerations**:
- Staging texture creation is expensive: reuse texture per session (sized to match source)
- `CopyResource()` is async on GPU, but `Map()` blocks until complete
- For 1080p (1920x1080x4 bytes), transfer ~8MB per frame, ~480 MB/s at 60 FPS
- Modern PCIe 3.0 can sustain this bandwidth, but consider pooling Bitmaps to reduce GC pressure

**Alternatives Considered**:
1. **Direct GPU rendering to screen, capture via screenshot**:
   - ❌ Rejected: Adds unnecessary indirection, defeats purpose of WGC

2. **Use ImageSharp instead of System.Drawing.Bitmap**:
   - ⚠️ Considered: More modern, cross-platform, better performance
   - ❌ Rejected for MVP: Spec requires System.Drawing.Bitmap (SC-006: "standard bitmap objects")
   - ✓ Future: Could add ImageSharp overload in v2.0

**Implementation Notes**:
- Bitmap stride may differ from texture row pitch, must handle padding
- Pixel format conversion: WGC typically delivers BGRA, Bitmap expects BGRA (PixelFormat.Format32bppArgb)
- Lock Bitmap bits with `LockBits()` for efficient bulk copy, avoid `SetPixel()`

---

### 4. FPS Throttling Strategy

**Decision**: Implement GPU-side frame skipping with timestamp-based throttling in CaptureSession

**Rationale**:
- **Efficiency**: Dropping frames before GPU-to-CPU transfer avoids expensive copy operation
- **Accurate Timing**: Use `Stopwatch` for high-resolution timestamps, calculate required frame interval (1000ms / targetFPS)
- **Zero CPU Copy for Skipped Frames**: Dispose WGC frame immediately without staging texture operations

**Algorithm**:
```
OnFrameArrived(framePool, args):
  frame = framePool.TryGetNextFrame()
  if frame == null: return

  currentTime = stopwatch.ElapsedMilliseconds
  timeSinceLastFrame = currentTime - lastFrameTimestamp
  requiredInterval = 1000 / targetFPS

  if timeSinceLastFrame < requiredInterval:
    frame.Dispose()  // Skip frame, no GPU-to-CPU copy
    return

  lastFrameTimestamp = currentTime
  ProcessFrame(frame)  // Do GPU-to-CPU copy + Bitmap conversion
```

**Alternatives Considered**:
1. **CPU-side throttling after Bitmap conversion**:
   - ❌ Rejected: Wastes GPU-to-CPU bandwidth on frames that will be dropped
   - ❌ Rejected: Creates Bitmap objects that are immediately discarded (GC pressure)

2. **WGC BufferAllocationKind adjustment**:
   - ⚠️ Considered: WGC can allocate fewer buffers in frame pool
   - ❌ Rejected: Does not provide FPS control, just buffer management
   - This is orthogonal to FPS control, still useful for memory optimization

3. **Thread.Sleep() between captures**:
   - ❌ Rejected: WGC is event-driven, cannot control frame arrival rate
   - ❌ Rejected: Would block event thread, cause frame loss

**Performance Validation**:
- Target: ±10% variance from target FPS (spec SC-008)
- Test: 60 FPS target, measure actual delivery over 10 minutes
- Expected variance sources: Windows DWM compositing jitter, GC pauses

---

### 5. Event-Driven Architecture for Frame Delivery

**Decision**: Expose .NET event pattern on ICaptureSession interface for frame delivery

**Rationale**:
- **Spec Requirement**: FR-004 mandates event-based frame delivery
- **.NET Idiomatic**: Standard event pattern familiar to C# developers
- **Non-Blocking**: Consumers process frames asynchronously without blocking capture thread

**API Design**:
```csharp
public interface ICaptureSession : IDisposable
{
    event EventHandler<FrameReadyEventArgs> FrameReady;

    void Start();
    void Stop();
    Bitmap TakeScreenshot();  // Synchronous single-frame capture
    Task<Bitmap> TakeScreenshotAsync();
}

public class FrameReadyEventArgs : EventArgs
{
    public Bitmap Frame { get; }
    public DateTime Timestamp { get; }
    public Size SourceSize { get; }
}
```

**Implementation Notes**:
- WGC `FrameArrived` event fires on internal thread, must marshal to appropriate sync context
- Use `SynchronizationContext.Post()` if available, otherwise fire on thread pool
- Consumers must not block in event handler (document this requirement)
- Frame ownership: Bitmap is created for consumer, they are responsible for disposal

**Alternatives Considered**:
1. **IObservable<Bitmap> (Rx.NET)**:
   - ⚠️ Considered: More powerful composition, backpressure handling
   - ❌ Rejected: Adds dependency, more complex API for simple streaming use case
   - ✓ Future: Could expose Rx overload as extension method

2. **Callback delegate instead of event**:
   - ❌ Rejected: Events are more discoverable in IntelliSense, standard .NET pattern

3. **Async enumerable (IAsyncEnumerable<Bitmap>)**:
   - ⚠️ Considered: Modern async pattern, enables foreach await
   - ❌ Rejected: Less ergonomic for real-time frame processing, implies pull model
   - WGC is push-based, mapping to pull model adds complexity

---

### 6. Exception Hierarchy Design

**Decision**: Implement comprehensive exception hierarchy inheriting from base `CaptureException`

**Hierarchy**:
```
CaptureException (abstract)
├── GraphicsCaptureNotSupportedException  // WGC not available on system
├── CaptureTargetInvalidException          // Window/monitor doesn't exist
│   ├── WindowNotFoundException            // Specific to window captures
│   └── InvalidMonitorException            // Specific to monitor captures
├── CaptureSessionException                // Session lifecycle errors
│   ├── SessionAlreadyStartedException
│   └── SessionNotStartedException
├── GraphicsDeviceException                // DirectX/GPU failures
└── RegionOutOfBoundsException             // Region capture validation
```

**Rationale**:
- **Constitutional Requirement**: Principle III mandates clear exception hierarchy
- **Catch Granularity**: Allows consumers to catch specific exceptions vs. broad CaptureException
- **Diagnostics**: Each exception includes detailed message and InnerException for underlying failures

**Exception Properties**:
- All include: Message, InnerException, stack trace
- Context-specific: `CaptureTargetInvalidException.TargetHandle`, `RegionOutOfBoundsException.AttemptedRegion`

**Implementation Notes**:
- Throw `GraphicsCaptureNotSupportedException` on library init if WGC not available (Windows version check)
- Validate window handle via `IsWindow()` before attempting capture
- Wrap all COM exceptions in appropriate CaptureException subtype

---

### 7. Thread-Safe Configuration Management

**Decision**: Use `static` class with `lock` for thread-safe global configuration

**Rationale**:
- **Constitutional Requirement**: Principle IV mandates centralized, thread-safe configuration
- **Simple & Correct**: `lock` on private static object provides mutual exclusion
- **No Over-Engineering**: Concurrent data structures overkill for infrequent config changes

**API Design**:
```csharp
public static class CaptureConfiguration
{
    private static readonly object _lock = new object();
    private static bool _includeCursor = false;
    private static int _defaultTargetFPS = 30;

    public static bool IncludeCursor
    {
        get { lock(_lock) return _includeCursor; }
        set { lock(_lock) _includeCursor = value; }
    }

    public static int DefaultTargetFPS
    {
        get { lock(_lock) return _defaultTargetFPS; }
        set
        {
            if (value < 1 || value > 120)
                throw new ArgumentOutOfRangeException(nameof(value), "FPS must be between 1 and 120");
            lock(_lock) _defaultTargetFPS = value;
        }
    }
}
```

**Validation**:
- FPS: 1-120 range (validated on set)
- Cursor: boolean (no validation needed)

**Alternatives Considered**:
1. **Immutable configuration object**:
   - ⚠️ Considered: Truly thread-safe, no locking needed
   - ❌ Rejected: Less discoverable for global settings pattern, requires factory/builder

2. **Concurrent collections**:
   - ❌ Rejected: Overkill for 2-3 configuration properties

3. **Per-session configuration**:
   - ⚠️ Considered: More flexible, avoids global state
   - ❌ Rejected: Spec and constitution mandate global configuration (FR-008)
   - ✓ Future: Could add per-session overrides in v2.0

---

### 8. Testing Strategy

**Decision**: Three-tier testing approach: Unit, Integration, Performance

**Unit Tests (xUnit)**:
- CaptureConfiguration validation logic
- Exception hierarchy correct inheritance
- Static facade parameter validation (null checks, range checks)
- Mock-based testing of CaptureSession state machine

**Integration Tests (xUnit + Real Windows)**:
- Capture actual test window (notepad.exe launched by test)
- Verify Bitmap dimensions match window size
- Test obscured window capture (overlay test window, verify content)
- Test window closure during capture (expect WindowNotFoundException)
- Test invalid handles, out-of-range monitor indices

**Performance Tests (BenchmarkDotNet)**:
- Single-frame capture latency (target: <100ms for 1080p)
- Sustained 60 FPS capture (10-minute test, measure jitter)
- Memory overhead per active session (target: <50 MB)
- Frame skipping efficiency (measure CPU/GPU usage with varying FPS targets)

**CI/CD Considerations**:
- Integration tests require Windows with graphics stack (cannot run in containers)
- Consider dedicated Windows test runner (GitHub Actions: windows-latest)
- Performance tests run on tagged releases only (too slow for every commit)

**Alternatives Considered**:
1. **Manual testing only**:
   - ❌ Rejected: Constitutional requirement for testing (Principle III), spec has explicit SC criteria

2. **TDD (Test-First)**:
   - ⚠️ Considered: Constitution suggests TDD preference
   - ✓ Partial adoption: Write unit tests first, integration tests require implementation to validate

---

### 9. Dependency Management

**Decision**: Minimize dependencies, use only essential NuGet packages

**Required Dependencies**:
1. **Vortice.Direct3D11** (latest stable): DirectX interop
2. **Vortice.Win32** (latest stable): DXGI/Win32 interop
3. **System.Drawing.Common** (latest stable): Bitmap class (already part of .NET)

**Testing Dependencies** (test project only):
1. **xUnit** (2.5+): Test framework
2. **xUnit.runner.visualstudio** (2.5+): VS test runner
3. **BenchmarkDotNet** (0.13+): Performance benchmarking

**Rationale**:
- Constitutional directive: "minimal external dependencies"
- Each dependency increases attack surface, maintenance burden
- Vortice.Windows is essential (no alternative for DirectX interop)
- System.Drawing.Common is already in framework (no additional dependency)

**Avoided Dependencies**:
- ❌ Logging frameworks (Microsoft.Extensions.Logging): Let consumers choose their logger
- ❌ Serialization (Newtonsoft.Json): No configuration persistence needed
- ❌ Image processing (ImageSharp): Out of scope per spec

---

### 10. Platform & Version Support

**Decision**: Windows 10 version 1903 (build 18362) or later, Windows 11

**Rationale**:
- **WGC Availability**: Windows.Graphics.Capture introduced in Windows 10 1809, stabilized in 1903
- **Market Coverage**: Windows 10 1903 released May 2019, end-of-service already passed for older versions
- **Validation**: Check Windows version at library init, throw `GraphicsCaptureNotSupportedException` if too old

**Version Check Implementation**:
```csharp
private static bool IsWGCSupported()
{
    var version = Environment.OSVersion.Version;
    // Windows 10 1903 = 10.0.18362
    return version.Major >= 10 && version.Build >= 18362;
}
```

**Alternatives Considered**:
1. **Support Windows 10 1809**:
   - ❌ Rejected: WGC had bugs in early releases, 1903 more stable

2. **Support Windows 7/8.1 with fallback**:
   - ❌ Rejected: Violates constitution (modern APIs only), adds significant complexity
   - End-of-life: Windows 7 (2020), Windows 8.1 (2023)

---

## Research Conclusions

All technical decisions support the constitutional requirements and specification goals:

1. ✅ **Simplicity**: Static facade API hides WGC/DirectX complexity
2. ✅ **Performance**: Hardware-accelerated WGC meets 60 FPS @ 1080p target
3. ✅ **Robustness**: Comprehensive exception hierarchy with clear failure modes
4. ✅ **Configuration**: Thread-safe global settings with validation
5. ⚠️ **.NET Standard 2.0**: Impossible due to WGC WinRT requirements, justified in Complexity Tracking

**Ready to proceed to Phase 1: Design & Contracts**
