<!--
Sync Impact Report:
- Version change: none → 1.0.0
- New constitution creation (initial version)
- Modified principles: N/A (initial creation)
- Added sections: All sections are new
- Removed sections: N/A
- Templates requiring updates:
  ✅ plan-template.md - reviewed, compatible with performance and testing principles
  ✅ spec-template.md - reviewed, compatible with user scenarios approach
  ✅ tasks-template.md - reviewed, compatible with phased implementation approach
- Follow-up TODOs: None
-->

# WindowCaptureCL Constitution

## Core Principles

### I. Simplicity through High-Level API

The library MUST abstract away all internal complexity. User interaction MUST occur through a clean, static facade class (the static `Capture` class) that serves as the single, intuitive entry point for all capture operations.

**Rationale**: Windows capture APIs (Desktop Duplication API, Graphics Capture API) are complex and require significant boilerplate. Users should never need to understand COM initialization, DXGI device management, or frame buffer handling. The facade pattern ensures a consistent, discoverable interface that hides all implementation details.

**Non-negotiable rules**:
- All public APIs MUST be accessible through the static `Capture` class
- Internal complexity (DirectX device management, COM, threading) MUST NOT leak into public interfaces
- Method signatures MUST be self-documenting with clear parameter names and purposes
- Users MUST be able to perform common operations (capture window, capture screen) in a single method call

### II. Performance as a Priority

The library's design MUST prioritize high performance and low system overhead. It MUST use modern, hardware-accelerated Windows APIs (Windows.Graphics.Capture, Desktop Duplication API) as its foundation. Performance-tuning mechanisms MUST be provided to users.

**Rationale**: Screen capture is a performance-critical operation, especially for real-time scenarios like streaming, recording, or monitoring. GDI-based approaches (BitBlt) are CPU-bound and obsolete. Hardware-accelerated APIs leverage GPU capabilities for significantly better performance and lower CPU usage.

**Non-negotiable rules**:
- MUST use Windows.Graphics.Capture API or Desktop Duplication API as the primary capture mechanism
- MUST NOT use legacy GDI APIs (BitBlt, GetDIBits) for primary capture path
- MUST provide FPS control mechanisms to allow users to balance performance vs. resource usage
- Memory allocation in hot paths MUST be minimized (pooling, reuse strategies required)
- All capture operations MUST be asynchronous to prevent blocking the caller's thread

### III. Robustness and Predictability

The library MUST be resilient to common failure scenarios and MUST provide a clear, custom exception hierarchy for predictable error handling.

**Rationale**: Capture operations can fail for numerous reasons: target window closes, DWM disables, graphics driver crashes, permissions issues. The library must handle these gracefully rather than crashing or exhibiting undefined behavior. A well-designed exception hierarchy allows consumers to implement precise error handling strategies.

**Non-negotiable rules**:
- MUST define a `CaptureException` base class for all library-specific exceptions
- MUST provide specialized exception types for common failures:
  - `WindowNotFoundException`: Target window no longer exists
  - `CaptureNotSupportedException`: API not available on this system
  - `GraphicsDeviceException`: GPU/driver-level failures
  - `PermissionDeniedException`: Insufficient permissions for capture
- When a target window closes during capture, MUST raise `WindowNotFoundException` rather than crashing
- All public methods MUST document their exception contracts
- Transient failures (e.g., frame loss) MUST be logged but MUST NOT terminate the capture session unless unrecoverable

### IV. Clear and Centralized Configuration

All global library settings (cursor visibility, capture quality, performance hints) MUST be managed through a centralized, type-safe configuration system.

**Rationale**: Scattered configuration makes the library difficult to use and maintain. A centralized system ensures discoverability, prevents configuration drift, and enables validation. Type safety prevents runtime errors from invalid settings.

**Non-negotiable rules**:
- MUST provide a `CaptureConfiguration` class (or similar) for all global settings
- Configuration MUST be validated at the point of setting (fail-fast for invalid values)
- Default configuration MUST be sensible and work out-of-the-box for common scenarios
- Configuration changes MUST be thread-safe if library supports concurrent capture sessions
- Settings MUST include:
  - Cursor visibility toggle
  - FPS target/limiter
  - Capture quality hints (if applicable to the chosen API)
  - Logging verbosity level

### V. .NET Standard 2.0 Compatibility

The library MUST target .NET Standard 2.0 to maximize compatibility across .NET Framework, .NET Core, and modern .NET (5+) applications.

**Rationale**: .NET Standard 2.0 provides broad compatibility while still offering modern C# language features and APIs. This ensures the library can be consumed by the widest possible range of Windows applications without requiring users to upgrade their entire project to the latest .NET version.

**Non-negotiable rules**:
- Project MUST target `netstandard2.0`
- MUST NOT use APIs unavailable in .NET Standard 2.0 without conditional compilation
- When using Windows-specific APIs, MUST use appropriate platform invocation (P/Invoke) or Windows Runtime projection
- Testing MUST verify compatibility with both .NET Framework 4.7.2+ and modern .NET

## Technical Standards

### API Design

- **Static Facade**: The primary entry point MUST be a static `Capture` class with methods like `CaptureWindow(IntPtr hWnd)`, `CaptureScreen(int screenIndex)`
- **Return Types**: Capture methods MUST return strongly-typed objects (e.g., `CapturedFrame` containing `Bitmap`, metadata, timestamp)
- **Async-First**: All I/O and capture operations MUST provide async variants (`CaptureWindowAsync`, etc.)
- **Resource Management**: All disposable resources MUST implement `IDisposable` correctly with clear lifetime documentation
- **Event-Driven Updates**: For continuous capture scenarios, MUST provide event-based notifications (e.g., `FrameCaptured` event)

### Error Handling Requirements

- **No Silent Failures**: Library MUST NOT swallow exceptions or fail silently
- **Logging**: MUST provide structured logging using standard .NET logging abstractions (Microsoft.Extensions.Logging recommended)
- **Diagnostics**: MUST log key operations (session start, frame capture, errors) at appropriate levels
- **Validation**: Input validation MUST occur immediately with `ArgumentException` or `ArgumentNullException` for invalid arguments

### Performance Standards

- **Target Metrics**:
  - 60 FPS capture for 1080p windows on typical hardware (mid-range GPU from last 5 years)
  - <50 MB memory overhead for active capture session
  - <16ms frame time budget for 60 FPS scenarios
- **Constraints**:
  - Frame encoding/saving MUST NOT block capture thread
  - GPU resource allocation MUST be lazy (initialize on first capture, not on library load)
  - MUST provide mechanisms to reduce quality/resolution if performance targets cannot be met

### Testing Requirements

- **Unit Tests**: Core logic (configuration validation, exception hierarchy) MUST have unit test coverage
- **Integration Tests**: MUST test actual capture operations against real windows (may require interactive testing or test windows)
- **Performance Tests**: MUST include benchmarks for common scenarios (capture single frame, capture 60 FPS for 10 seconds)
- **Compatibility Tests**: MUST verify operation on Windows 10 (1903+) and Windows 11

## Development Workflow

### Implementation Phases

1. **Phase 0 - Research**: Investigate Windows.Graphics.Capture API capabilities, limitations, and fallback strategies
2. **Phase 1 - Foundation**: Implement configuration system, exception hierarchy, logging infrastructure
3. **Phase 2 - Core Capture**: Implement static facade, basic window/screen capture using Graphics Capture API
4. **Phase 3 - Robustness**: Add error handling, window lifetime monitoring, resource cleanup
5. **Phase 4 - Performance**: Implement FPS control, frame pooling, async patterns
6. **Phase 5 - Polish**: Documentation, samples, performance tuning, edge case handling

### Code Review Gates

- **API Simplicity Check**: Can a new user capture a window in <5 lines of code without reading docs?
- **Performance Validation**: Does the implementation meet the 60 FPS @ 1080p target on reference hardware?
- **Error Handling Audit**: Are all failure scenarios documented and handled with appropriate exceptions?
- **Configuration Audit**: Are all magic numbers/hardcoded values extracted to configuration?

### Documentation Requirements

- **QuickStart Guide**: MUST provide a simple example showing window capture in <10 lines of code
- **API Reference**: All public types and members MUST have XML documentation comments
- **Architecture Overview**: MUST document the internal architecture for contributors
- **Performance Guide**: MUST document best practices for high-performance scenarios

## Governance

### Amendment Procedure

1. Proposed amendments MUST be documented in an issue or design document
2. Amendments affecting core principles (I-V) require explicit justification for why existing principle is insufficient
3. Changes MUST include migration plan if breaking existing guidance
4. Amendments MUST update dependent templates (plan-template.md, spec-template.md, tasks-template.md) in the same commit

### Versioning Policy

- **MAJOR version**: Backward incompatible governance changes (e.g., removing a core principle, changing testing requirements fundamentally)
- **MINOR version**: New principles added or existing principles materially expanded
- **PATCH version**: Clarifications, wording improvements, non-semantic refinements

### Compliance Review

- All feature specifications MUST reference relevant constitutional principles in their "Constitution Check" section
- Implementation plans MUST verify adherence to performance standards and API design rules
- PRs violating core principles MUST be rejected unless accompanied by constitutional amendment proposal
- Complexity violations (e.g., exposing internal DirectX details in public API) MUST be justified in the "Complexity Tracking" section of plan.md

### Enforcement

This constitution supersedes all other practices and preferences. When in conflict, constitution wins.

**Version**: 1.0.0 | **Ratified**: 2025-10-22 | **Last Amended**: 2025-10-22
