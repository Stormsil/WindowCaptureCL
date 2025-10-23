# Implementation Plan: WindowCaptureCL - High-Performance Screen Capture Library

**Branch**: `001-window-capture-library` | **Date**: 2025-10-22 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-window-capture-library/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

WindowCaptureCL is a modern .NET class library designed for high-performance, hardware-accelerated screen and window capture for automation systems. The library provides three capture modes (window, monitor, region) with both single-shot and continuous streaming capabilities, all accessible through a simple static facade API. Built exclusively on Windows Graphics Capture (WGC) API for hardware acceleration, the library delivers captured frames as standard Bitmap objects via an event-driven architecture with configurable FPS control.

## Technical Context

**Language/Version**: C# / .NET 8 (targeting `net8.0-windows`)
**Primary Dependencies**: Vortice.Direct3D11, Vortice.Win32 (for DirectX interop), System.Drawing.Common (for Bitmap output)
**Storage**: N/A (in-memory frame processing only, no persistence)
**Testing**: xUnit (for unit/integration tests), BenchmarkDotNet (for performance validation)
**Target Platform**: Windows 10 version 1903+ / Windows 11 (requires Windows Graphics Capture API support)
**Project Type**: Single library project (class library with test project)
**Performance Goals**: 60 FPS sustained capture for 1080p windows, <100ms single-frame capture latency, <50MB memory overhead per active session
**Constraints**: <16ms frame time budget for 60 FPS scenarios, GPU-based frame skipping (no CPU copy for dropped frames), thread-safe configuration management
**Scale/Scope**: Single library (~15-20 classes), 5 public API surface classes, 3 capture modes, comprehensive exception hierarchy

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principle I: Simplicity through High-Level API
- ✅ **PASS**: Design specifies static `Capture` facade class as sole entry point (API/Capture.cs)
- ✅ **PASS**: Internal complexity (DirectX, WGC, COM) isolated in Infrastructure/ and Core/ directories
- ✅ **PASS**: Public interface (ICaptureSession) provides intuitive methods without exposing implementation details
- ✅ **PASS**: Single-method capture operations (FromWindow, FromScreen, FromScreenRegion)

### Principle II: Performance as a Priority
- ✅ **PASS**: Exclusive use of Windows Graphics Capture API (hardware-accelerated, modern)
- ✅ **PASS**: No legacy GDI APIs (BitBlt, GetDIBits) in design
- ✅ **PASS**: FPS control mechanism specified (CaptureSession.TargetFPS property)
- ✅ **PASS**: GPU-based frame skipping (no CPU copy for dropped frames)
- ✅ **PASS**: Async operations planned (async variants of capture methods)

### Principle III: Robustness and Predictability
- ✅ **PASS**: Custom exception hierarchy defined (CaptureException base class in Exceptions.cs)
- ✅ **PASS**: Specialized exceptions specified (GraphicsCaptureNotSupportedException, CaptureTargetInvalidException)
- ✅ **PASS**: Graceful handling of window closure during capture (specified in requirements)
- ✅ **PASS**: Exception documentation planned for all public methods
- ✅ **PASS**: Transient failure handling (frame loss logged, session continues unless unrecoverable)

### Principle IV: Clear and Centralized Configuration
- ✅ **PASS**: CaptureConfiguration static class specified (API/CaptureConfiguration.cs)
- ✅ **PASS**: Global settings defined (IncludeCursor, DrawBorder, DefaultTargetFPS)
- ✅ **PASS**: Validation at point-of-setting implied by static class design
- ✅ **PASS**: Sensible defaults specified (cursor excluded, logging enabled)
- ⚠️ **NEEDS VERIFICATION**: Thread-safety for configuration changes (to be validated in Phase 1 design)

### Principle V: .NET Standard 2.0 Compatibility
- ❌ **VIOLATION**: Project targets `net8.0-windows` instead of `netstandard2.0`
- **JUSTIFICATION REQUIRED**: Constitution mandates .NET Standard 2.0 for broad compatibility

**Constitution Check Status**: ❌ **GATE FAILURE - Requires Justification**

The design violates Principle V by targeting .NET 8 instead of .NET Standard 2.0. This requires explicit justification in the Complexity Tracking section.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
WindowCaptureCL/                      # Root solution directory
├── src/
│   └── WindowCaptureCL/              # Main library project
│       ├── API/                      # Public interface layer
│       │   ├── Capture.cs            # Static facade (entry point)
│       │   ├── ICaptureSession.cs    # Session interface
│       │   ├── CaptureConfiguration.cs  # Global settings
│       │   └── Exceptions.cs         # Exception hierarchy
│       ├── Core/                     # Internal implementation
│       │   └── CaptureSession.cs     # ICaptureSession implementation
│       └── Infrastructure/           # Low-level platform code
│           ├── DirectXManager.cs     # D3D11 device management
│           └── WgcInterop.cs         # Windows Graphics Capture interop
├── tests/
│   └── WindowCaptureCL.Tests/        # Test project
│       ├── Unit/                     # Unit tests
│       │   ├── ConfigurationTests.cs
│       │   └── ExceptionTests.cs
│       ├── Integration/              # Integration tests
│       │   ├── WindowCaptureTests.cs
│       │   ├── MonitorCaptureTests.cs
│       │   └── RegionCaptureTests.cs
│       └── Performance/              # Performance benchmarks
│           └── CaptureBenchmarks.cs
├── samples/                          # Example usage projects
│   ├── BasicCapture/                 # Simple capture example
│   └── ContinuousMonitoring/         # Streaming example
└── WindowCaptureCL.sln               # Solution file
```

**Structure Decision**: Single library project structure selected. This is a standalone class library with no web/mobile components. The structure separates concerns into three layers:
1. **API/**: Public-facing classes that users interact with (5 classes total)
2. **Core/**: Internal implementation of capture logic (hidden from consumers)
3. **Infrastructure/**: Low-level DirectX and Windows API interop (hidden from consumers)

Tests are organized by type (unit, integration, performance) to support different testing scenarios and CI/CD stages.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| **Targeting .NET 8 (`net8.0-windows`) instead of .NET Standard 2.0** | Windows Graphics Capture API requires Windows Runtime (WinRT) APIs that are only accessible via .NET 5+ with Windows-specific TFMs. The constitution's Principle V was written before understanding WGC's strict requirements. | **.NET Standard 2.0**: Cannot access Windows.Graphics.Capture namespace (WinRT). Would require COM interop which is significantly more complex, error-prone, and defeats the purpose of using modern APIs. <br>**.NET Framework 4.7.2+**: Does not support WinRT projections without complex workarounds. <br>**Compromise**: Target `net8.0-windows` for library, but ensure consuming applications can be on .NET 6+ (which has broad adoption). This still provides wide compatibility while enabling access to required modern Windows APIs. |

**Justification Summary**: The constitutional requirement for .NET Standard 2.0 is incompatible with the equally important constitutional requirement for hardware-accelerated, modern Windows APIs (Principle II). Windows Graphics Capture API is only accessible via .NET 5+ with Windows-specific target frameworks. This is a necessary architectural trade-off where performance and modernity (Principle II) take precedence over maximum backward compatibility (Principle V). The library will still support .NET 6+ consumers, which covers the vast majority of modern .NET applications.

---

## Phase Summary

### Phase 0: Research (✅ Complete)

**Artifacts Generated**:
- [research.md](./research.md) - Technical decisions and rationale

**Key Decisions**:
1. **Capture Technology**: Windows Graphics Capture (WGC) API exclusively - provides hardware acceleration and obscured window capture
2. **DirectX Interop**: Vortice.Windows library for type-safe DirectX bindings
3. **GPU-to-CPU Transfer**: Staging texture pattern for Bitmap conversion
4. **FPS Throttling**: GPU-side frame skipping before CPU copy (timestamp-based)
5. **Event Architecture**: .NET event pattern for frame delivery (FrameReady event)
6. **Exception Design**: Comprehensive hierarchy with CaptureException base class
7. **Configuration**: Static class with lock-based thread safety
8. **Testing**: Three-tier approach (unit, integration, performance)
9. **Dependencies**: Minimal - only Vortice.Direct3D11, Vortice.Win32, System.Drawing.Common
10. **Platform Support**: Windows 10 1903+ / Windows 11 only

**Research Validation**: All constitutional requirements validated except Principle V (.NET Standard 2.0), which has been explicitly justified in Complexity Tracking section.

---

### Phase 1: Design & Contracts (✅ Complete)

**Artifacts Generated**:
- [data-model.md](./data-model.md) - Entity definitions, relationships, validation rules
- [contracts/API.md](./contracts/API.md) - Complete public API surface specification
- [quickstart.md](./quickstart.md) - Developer onboarding guide with examples
- CLAUDE.md (agent context) - Project metadata for AI assistants

**Key Design Elements**:

**Data Model (6 Entities)**:
1. `CapturedFrame` - Immutable frame data with metadata
2. `CaptureSourceInfo` - Source identification (window/monitor/region)
3. `ICaptureSession` - Public session interface with lifecycle
4. `FrameReadyEventArgs` - Event arguments for frame delivery
5. `CaptureStoppedEventArgs` - Event arguments for stoppage
6. `CaptureConfiguration` - Static global settings

**API Contracts**:
- **Static Facade**: `Capture.FromWindow()`, `Capture.FromScreen()`, `Capture.FromScreenRegion()`
- **Session Interface**: 4 properties, 5 methods, 2 events
- **Configuration**: 3 configurable properties, 2 constants
- **Exception Hierarchy**: 8 exception types inheriting from `CaptureException`

**Thread Safety**: All public APIs guaranteed thread-safe via locks

**Re-evaluated Constitution Check**:
- ✅ **Principle IV Thread-Safety**: Validated in data-model.md - all configuration properties use lock-based synchronization
- ❌ **Principle V (.NET Standard 2.0)**: Still violated, justification stands (WGC requires .NET 5+ with `net8.0-windows` TFM)

---

### Phase 2: Task Generation (⏸️ Not Started)

**Next Command**: `/speckit.tasks`

**Expected Output**: `tasks.md` with dependency-ordered implementation tasks organized by user story priority

**Ready for Implementation**: All design artifacts complete, ready to proceed to task breakdown

---

## Implementation Roadmap

### Immediate Next Steps (Phase 2)

1. Run `/speckit.tasks` to generate tasks.md
2. Review task breakdown and dependencies
3. Begin implementation with Phase 1 (Setup) tasks

### Recommended Implementation Order

**MVP (User Story 1 - Single Window Screenshot)**:
1. Project setup (solution, projects, NuGet packages)
2. Exception hierarchy implementation
3. Static Capture facade with FromWindow() method
4. ICaptureSession interface definition
5. CaptureSession implementation (window capture only)
6. DirectXManager for D3D11 device management
7. WgcInterop for Windows Graphics Capture bindings
8. GPU-to-CPU texture transfer and Bitmap conversion
9. TakeScreenshot() synchronous method
10. Unit tests for configuration and exceptions
11. Integration tests for window capture

**Iteration 2 (User Story 2 - Continuous Streaming)**:
1. Event infrastructure (FrameReady, CaptureStopped)
2. FPS throttling logic in CaptureSession
3. Start() / Stop() lifecycle management
4. TakeScreenshotAsync() implementation
5. Integration tests for continuous capture

**Iteration 3 (User Story 3-4 - Monitor & Region Capture)**:
1. FromScreen() implementation
2. FromScreenRegion() implementation
3. Monitor enumeration and validation
4. Region bounds validation
5. Integration tests for monitor and region captures

**Iteration 4 (User Story 5 - Configuration)**:
1. CaptureConfiguration static class
2. Thread-safe property implementation
3. Cursor visibility integration with WGC
4. Unit tests for configuration

**Iteration 5 (Polish & Performance)**:
1. Performance benchmarks
2. Memory leak detection tests
3. Sample applications (BasicCapture, ContinuousMonitoring)
4. Documentation finalization
5. NuGet package preparation

---

## Success Criteria Validation Plan

| Success Criterion | Validation Method | Target |
|-------------------|-------------------|--------|
| **SC-001**: 5 lines of code for capture | Code review of quickstart examples | ≤5 lines |
| **SC-002**: <100ms single-frame latency | Performance benchmark (BenchmarkDotNet) | <100ms p95 |
| **SC-003**: 60 FPS sustained capture | 10-minute stress test with frame timing | 60 FPS ±5% |
| **SC-004**: <50MB memory overhead | Memory profiler during 1-hour capture | <50MB |
| **SC-005**: Obscured window capture | Integration test with overlapping windows | 95% success |
| **SC-006**: Exception handling | Unit tests for all invalid inputs | 100% coverage |
| **SC-007**: Windows 10/11 compatibility | Manual testing on both OS versions | No errors |
| **SC-008**: ±10% FPS timing accuracy | Statistical analysis of frame timestamps | ±10% variance |
| **SC-009**: Graceful window closure | Integration test closing window mid-capture | 100% graceful |
| **SC-010**: 10 lines for streaming | Code review of quickstart streaming example | ≤10 lines |

---

## Risk Assessment

### Technical Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| WGC API instability on older Windows 10 builds | Medium | High | Target Windows 10 1903+ only, validate version at init |
| GPU driver compatibility issues | Medium | Medium | Comprehensive error handling, clear diagnostics |
| Performance not meeting 60 FPS target | Low | Medium | GPU-based frame skipping, staging texture pooling |
| Memory leaks in COM interop | Medium | High | Use Vortice.Windows for automatic COM lifetime management |
| Thread-safety bugs in configuration | Low | Medium | Lock-based synchronization, comprehensive unit tests |

### Development Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Vortice.Windows API changes | Low | Medium | Pin specific version, monitor for breaking changes |
| Integration testing requires physical Windows machine | High | Low | Use GitHub Actions windows-latest runner |
| Complex DirectX debugging | Medium | Medium | Extensive logging, use PIX debugger for GPU issues |

---

## Open Questions

1. **System.Drawing.Common deprecation**: .NET 6+ deprecated System.Drawing.Common on non-Windows platforms. Since this library is Windows-only, should we add warning suppression or switch to ImageSharp in future version?
   - **Current Decision**: Use System.Drawing.Bitmap per spec requirement, add suppression warning with comment
   - **Future**: Consider ImageSharp in v2.0 with breaking change

2. **Cursor rendering fidelity**: WGC may not capture animated cursors correctly. Should we document this limitation?
   - **Current Decision**: Document in API that cursor capture is "best effort" and may not render animated cursors

3. **DPI scaling**: How should captures handle high-DPI displays with scaling?
   - **Current Decision**: WGC handles DPI automatically, captured bitmap matches logical window size (scaled), not physical pixels

---

## Completion Status

**Phase 0 (Research)**: ✅ **COMPLETE**
**Phase 1 (Design)**: ✅ **COMPLETE**
**Phase 2 (Tasks)**: ⏸️ **AWAITING `/speckit.tasks` COMMAND**

**Ready to Proceed**: Yes - All design artifacts validated and constitutional violations justified
