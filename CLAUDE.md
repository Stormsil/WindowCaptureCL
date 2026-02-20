# CLAUDE.md - WindowCaptureCL Developer Guide

> This file helps AI agents (Claude Code) understand the WindowCaptureCL codebase architecture, conventions, and development workflow.

## Quick Reference

**Project Type**: .NET 8.0 Class Library for Windows
**Target Platform**: Windows 10 version 1803+ (Build 17134+)
**Primary Dependencies**: DirectX 11, Windows Graphics Capture API

For API usage and examples, see `Docs/README.md` and `Docs/API_REFERENCE.md`.

---

## Build and Development

### Building the Library

**Using Visual Studio:**
- Open `WindowCaptureCL.csproj` in Visual Studio 2022+
- Build solution: `Ctrl+Shift+B` or Build > Build Solution
- Output: `bin/Debug/net8.0-windows10.0.19041.0/WindowCaptureCL.dll`

**Using .NET CLI:**
```bash
# Build debug
dotnet build

# Build release
dotnet build -c Release

# Clean build
dotnet clean && dotnet build
```

### Running and Testing

This is a **class library** with no executable entry point. To test:
1. Create a console or Windows Forms test application
2. Reference the library or DLL
3. Use the `Capture` API to create capture sessions

**Note**: Currently no unit test project exists in the repository.

### Dependencies

Key NuGet packages:
- `Vortice.Direct3D11` (3.6.2) - DirectX 11 bindings
- `Vortice.Win32` (2.3.0) - Win32 API interop
- `System.Drawing.Common` (9.0.10) - Bitmap handling
- Framework reference: `Microsoft.WindowsDesktop.App.WindowsForms`

---

## Architecture Overview

### Layered Architecture

The codebase follows a **three-layer architecture** with clear separation of concerns:

```
API Layer (Public Interface)
    ↓
Core Layer (Business Logic)
    ↓
Infrastructure Layer (Platform/Hardware Integration)
```

#### 1. API Layer (`/API/`)

**Purpose**: Public-facing interface and contracts

**Key Components:**
- `Capture.cs` - Static entry point with factory methods (`FromWindow`, `FromScreen`, `FromScreenRegion`)
- `ICaptureSession.cs` - Main interface for capture operations
- `CapturedFrame.cs` - Represents a captured frame with bitmap and metadata
- `CaptureConfiguration.cs` - Configuration settings (FPS, cursor, border)
- Event args: `FrameReadyEventArgs`, `CaptureErrorEventArgs`, `CaptureStoppedEventArgs`
- Enums: `CaptureSourceType`, `CaptureStopReason`
- `Exceptions.cs` - Custom exception types

**Design**: Clean, simple API that hides platform complexity from consumers.

#### 2. Core Layer (`/Core/`)

**Purpose**: Business logic and state management

**Key Components:**
- `CaptureSession.cs` - Implements `ICaptureSession`, orchestrates capture logic
  - Manages capture state (idle, active)
  - Coordinates frame capture timing and FPS throttling
  - Handles event dispatching
  - Ensures thread safety with locks
  - Manages resource lifecycle

**Responsibilities:**
- Session lifecycle management
- Frame rate control and timing
- Event coordination
- Thread synchronization
- Resource disposal pattern

#### 3. Infrastructure Layer (`/Infrastructure/`)

**Purpose**: Platform-specific implementation details

**Subdirectories:**

**`/Infrastructure/WGC/`** - Windows Graphics Capture integration
- `GraphicsCaptureHelper.cs` - WGC API wrapper
- `GraphicsCaptureItemHelper.cs` - Creates capture items from sources
- `WindowEnumerator.cs` - Enumerates windows
- `MonitorEnumerator.cs` - Enumerates monitors/displays
- `WgcInterop.cs` - COM interop definitions
- `Direct3D11Helper.cs` - D3D11/WGC integration

**`/Infrastructure/DirectX/`** - DirectX 11 integration
- `DirectXDeviceManager.cs` - D3D11 device initialization and management
- `FrameProcessor.cs` - Converts DirectX surfaces to .NET Bitmaps

**Design**: Isolates platform-specific code, could theoretically support other capture backends.

### Capture Flow

**Single Frame Capture:**
1. User calls `session.CaptureFrame()` or `CaptureFrameAsync()`
2. `CaptureSession` requests frame from WGC subsystem
3. `GraphicsCaptureHelper` captures DirectX surface
4. `FrameProcessor` converts surface to Bitmap
5. Returns `CapturedFrame` with bitmap and timestamp

**Continuous Capture:**
1. User subscribes to `FrameReady` event
2. User calls `session.StartCapture()`
3. `CaptureSession` starts background capture loop
4. Loop captures frames at configured FPS
5. Each frame triggers `FrameReady` event on background thread
6. User calls `session.StopCapture()` to end

---

## Key Design Patterns

### Thread Safety

**Pattern**: Lock-based synchronization
- `CaptureSession` uses `lock` statements to protect shared state
- Critical sections: state changes, event subscriptions, configuration updates
- Events may fire on background threads - consumers must handle synchronization

### Resource Management

**Pattern**: IDisposable with deterministic cleanup
- `ICaptureSession` implements `IDisposable`
- `CapturedFrame` implements `IDisposable`
- Disposes DirectX resources, bitmaps, and COM objects
- Always use `using` statements or explicit `Dispose()` calls

**Critical**: Frames in continuous capture must be disposed promptly to avoid memory leaks, especially at high FPS.

### Task Reference System

**Convention**: Code contains `T###` comments (e.g., `// T001`, `// T042`)
- These reference implementation tasks from planning/specification
- Used for tracking which code implements which requirements
- Do not remove these comments when editing code

---

## Technical Constraints

### Platform Requirements

- **Windows Version**: 10 version 1803+ (Build 17134, April 2018 Update)
  - Windows Graphics Capture API introduced in 1803
  - Runtime check: throws `GraphicsCaptureNotSupportedException` on older systems
- **DirectX**: DirectX 11 compatible GPU required
- **.NET**: .NET 8.0 Windows SDK 10.0.19041.0+

### FPS Limits

- Minimum: 1 FPS
- Maximum: 120 FPS
- Actual FPS may be lower due to system load or source refresh rate
- Configuration validation enforces these limits

### Capture Capabilities

**Supported:**
- Capturing minimized or obscured windows (WGC capability)
- Multi-monitor setups
- Arbitrary screen regions
- Hardware cursor capture (configurable)

**Not Supported:**
- Capturing protected content (DRM, some fullscreen games)
- Cross-platform capture (Windows-only API)

---

## Code Style and Conventions

### General Style

See `.editorconfig` for complete rules. Key points:

**Formatting:**
- Indentation: 4 spaces (not tabs)
- Line endings: CRLF (Windows style)
- Opening braces: New line for all constructs (`csharp_new_line_before_open_brace = all`)
- UTF-8 encoding

**Naming:**
- Interfaces: `I` prefix + PascalCase (e.g., `ICaptureSession`)
- Classes/Structs/Enums: PascalCase
- Methods/Properties: PascalCase
- Parameters/Locals: camelCase
- Private fields: camelCase with no prefix

**Language Features:**
- Prefer `var` for built-in types and when type is apparent
- Use object and collection initializers
- Use null-coalescing (`??`) and null-propagation (`?.`)
- Expression-bodied members for properties/indexers/accessors
- Pattern matching over `is` with cast and `as` with null check

### Documentation

- XML documentation enabled (`GenerateDocumentationFile = true`)
- Document all public APIs
- Include `<summary>`, `<param>`, `<returns>`, `<exception>` tags

### Unsafe Code

- Allowed (`AllowUnsafeBlocks = true`) for DirectX interop
- Used sparingly in `FrameProcessor` for bitmap data copying

---

## Common Development Scenarios

### Adding a New Capture Mode

1. Extend `CaptureSourceType` enum if needed
2. Add factory method to `Capture` class
3. Implement source validation in appropriate Infrastructure helper
4. Update `CaptureSession` if new behavior required
5. Add exception types if needed
6. Update documentation in `Docs/`

### Adding Configuration Options

1. Add property to `CaptureConfiguration` class
2. Add validation in property setter
3. Update `Clone()` method
4. Apply setting in `CaptureSession` or Infrastructure layer
5. Update API documentation

### Modifying Exception Handling

- All custom exceptions inherit from `CaptureException` base class
- Defined in `API/Exceptions.cs`
- Include relevant context properties (e.g., `WindowHandle`, `MonitorIndex`)
- Document in API reference

### Performance Optimization

**Profile first**: Continuous capture at high FPS is demanding
- DirectX operations are on critical path
- Bitmap conversion is CPU-intensive
- Frame disposal impacts GC pressure
- Lock contention affects throughput

**Focus areas**: `FrameProcessor`, `DirectXDeviceManager`, `CaptureSession` timing loop

---

## Important Technical Notes

### DirectX Integration

- Uses **Vortice.Direct3D11** for managed DirectX 11 bindings
- Device created with `D3D11_CREATE_DEVICE_BGRA_SUPPORT` flag for WGC compatibility
- Texture format: `DXGI_FORMAT_B8G8R8A8_UNORM` (32-bit BGRA)
- Staging textures used for GPU-to-CPU transfer

### Windows Graphics Capture (WGC)

- COM-based API accessed via C#/WinRT projections
- `GraphicsCaptureItem` represents capture source
- `Direct3D11CaptureFramePool` manages frame buffers
- `GraphicsCaptureSession` drives the capture

### Bitmap Output

- Frames returned as `System.Drawing.Bitmap` (GDI+)
- Format: 32-bit ARGB
- Memory: Managed wrapper over unmanaged bitmap data
- Dispose to release unmanaged memory promptly

---

## Troubleshooting

### Common Issues

**"GraphicsCaptureNotSupportedException"**
- Windows version < 1803
- Missing Windows.Graphics.Capture namespace (SDK version mismatch)

**"WindowNotFoundException"**
- Invalid window handle (window closed or `IntPtr.Zero`)
- Verify handle with `IsWindow()` Win32 API

**"InvalidMonitorException"**
- Monitor index out of range
- Check monitor count before calling

**Low frame rates in continuous capture**
- System under load
- Source refresh rate lower than requested FPS
- Check actual FPS with timestamps in `FrameReadyEventArgs`

**Memory growth**
- Not disposing `CapturedFrame` objects
- Verify `using` statements or explicit `Dispose()` calls
- Particularly critical in `FrameReady` event handlers

---

## Project Structure Reference

```
WindowCaptureCL/
├── API/                      # Public interface layer
│   ├── Capture.cs           # Static entry point
│   ├── ICaptureSession.cs   # Main interface
│   ├── CapturedFrame.cs     # Frame data
│   ├── CaptureConfiguration.cs
│   ├── CaptureSourceInfo.cs
│   ├── Exceptions.cs
│   └── [Event args and enums]
│
├── Core/                     # Business logic layer
│   └── CaptureSession.cs    # Session implementation
│
├── Infrastructure/           # Platform integration layer
│   ├── WGC/                 # Windows Graphics Capture
│   │   ├── GraphicsCaptureHelper.cs
│   │   ├── GraphicsCaptureItemHelper.cs
│   │   ├── WindowEnumerator.cs
│   │   ├── MonitorEnumerator.cs
│   │   ├── Direct3D11Helper.cs
│   │   └── WgcInterop.cs
│   │
│   └── DirectX/             # DirectX 11 integration
│       ├── DirectXDeviceManager.cs
│       └── FrameProcessor.cs
│
├── Docs/                    # Documentation
│   ├── README.md           # Quick start guide
│   └── API_REFERENCE.md    # Complete API docs
│
├── .editorconfig           # Code style rules
├── WindowCaptureCL.csproj  # Project file
└── CLAUDE.md              # This file

```

---

## Getting Help

- **API Usage**: See `Docs/README.md` for quick start and common patterns
- **API Details**: See `Docs/API_REFERENCE.md` for complete reference
- **Architecture Questions**: This file (CLAUDE.md)
- **Code Style**: See `.editorconfig`
