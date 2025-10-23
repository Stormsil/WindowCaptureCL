# Tasks: WindowCaptureCL - High-Performance Screen Capture Library

**Input**: Design documents from `/specs/001-window-capture-library/`
**Prerequisites**: plan.md, spec.md, data-model.md, contracts/API.md, research.md, quickstart.md

**Organization**: Tasks are grouped by architectural layer (Infrastructure → Core → API) within each user story phase to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Single project**: `src/WindowCaptureCL/`, `tests/WindowCaptureCL.Tests/` at repository root
- Paths shown below use the single library project structure per plan.md

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create solution file WindowCaptureCL.sln at repository root
- [X] T002 Create .NET 8 class library project at src/WindowCaptureCL/WindowCaptureCL.csproj targeting net8.0-windows
- [X] T003 [P] Create xUnit test project at tests/WindowCaptureCL.Tests/WindowCaptureCL.Tests.csproj
- [X] T004 [P] Add NuGet package Vortice.Direct3D11 (latest stable) to src/WindowCaptureCL/WindowCaptureCL.csproj
- [X] T005 [P] Add NuGet package Vortice.Win32 (latest stable) to src/WindowCaptureCL/WindowCaptureCL.csproj
- [X] T006 [P] Add NuGet package xUnit (2.5+) to tests/WindowCaptureCL.Tests/WindowCaptureCL.Tests.csproj
- [X] T007 [P] Add NuGet package xUnit.runner.visualstudio to tests/WindowCaptureCL.Tests/WindowCaptureCL.Tests.csproj
- [X] T008 [P] Add NuGet package BenchmarkDotNet (0.13+) to tests/WindowCaptureCL.Tests/WindowCaptureCL.Tests.csproj
- [X] T009 [P] Create directory structure: src/WindowCaptureCL/API/, src/WindowCaptureCL/Core/, src/WindowCaptureCL/Infrastructure/
- [X] T010 [P] Create directory structure: tests/WindowCaptureCL.Tests/Unit/, tests/WindowCaptureCL.Tests/Integration/, tests/WindowCaptureCL.Tests/Performance/
- [X] T011 [P] Create directory structure: samples/BasicCapture/, samples/ContinuousMonitoring/
- [X] T012 [P] Add project reference from tests/WindowCaptureCL.Tests to src/WindowCaptureCL
- [X] T013 [P] Configure assembly properties in src/WindowCaptureCL/WindowCaptureCL.csproj (AssemblyName, RootNamespace, Version 1.0.0)
- [X] T014 [P] Add .gitignore file with standard .NET patterns (bin/, obj/, *.user, etc.)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Infrastructure Layer - Exception Hierarchy

- [ ] T015 [P] Create abstract base class CaptureException in src/WindowCaptureCL/API/Exceptions.cs with constructors for message and inner exception
- [ ] T016 [P] Create GraphicsCaptureNotSupportedException inheriting from CaptureException in src/WindowCaptureCL/API/Exceptions.cs
- [ ] T017 [P] Create abstract CaptureTargetInvalidException with Target property inheriting from CaptureException in src/WindowCaptureCL/API/Exceptions.cs
- [ ] T018 [P] Create WindowNotFoundException with WindowHandle property inheriting from CaptureTargetInvalidException in src/WindowCaptureCL/API/Exceptions.cs
- [ ] T019 [P] Create InvalidMonitorException with MonitorIndex property inheriting from CaptureTargetInvalidException in src/WindowCaptureCL/API/Exceptions.cs
- [ ] T020 [P] Create RegionOutOfBoundsException with AttemptedRegion and MonitorSize properties in src/WindowCaptureCL/API/Exceptions.cs
- [ ] T021 [P] Create SessionAlreadyStartedException inheriting from CaptureException in src/WindowCaptureCL/API/Exceptions.cs
- [ ] T022 [P] Create SessionNotStartedException inheriting from CaptureException in src/WindowCaptureCL/API/Exceptions.cs
- [ ] T023 [P] Create GraphicsDeviceException inheriting from CaptureException in src/WindowCaptureCL/API/Exceptions.cs

### Infrastructure Layer - Supporting Types

- [ ] T024 [P] Create CaptureSourceType enum (Window, Monitor, Region) in src/WindowCaptureCL/API/CaptureSourceInfo.cs
- [ ] T025 [P] Create CaptureSourceInfo class with SourceType, WindowHandle, MonitorIndex, Region properties in src/WindowCaptureCL/API/CaptureSourceInfo.cs
- [ ] T026 [P] Create CaptureStopReason enum (UserRequested, SourceClosed, Error) in src/WindowCaptureCL/API/CaptureStoppedEventArgs.cs
- [ ] T027 [P] Create CaptureStoppedEventArgs class with Reason and Exception properties in src/WindowCaptureCL/API/CaptureStoppedEventArgs.cs
- [ ] T028 [P] Create CapturedFrame class with Frame (Bitmap), Timestamp, SourceSize, FrameNumber, CaptureSource properties in src/WindowCaptureCL/API/CapturedFrame.cs (immutable, read-only)
- [ ] T029 [P] Create FrameReadyEventArgs class with Frame property (CapturedFrame) in src/WindowCaptureCL/API/FrameReadyEventArgs.cs

### Infrastructure Layer - DirectX and WGC Interop

- [ ] T030 Create DirectXManager class with singleton pattern in src/WindowCaptureCL/Infrastructure/DirectXManager.cs for D3D11Device creation and management
- [ ] T031 Add CreateDevice() method to DirectXManager using Vortice.Direct3D11 to create ID3D11Device with D3D11_CREATE_DEVICE_BGRA_SUPPORT flag
- [ ] T032 Add GetDeviceContext() method to DirectXManager returning ID3D11DeviceContext for the shared device
- [ ] T033 Add CreateStagingTexture() method to DirectXManager for CPU-accessible texture creation with D3D11_USAGE_STAGING
- [ ] T034 Add Dispose() method to DirectXManager to release D3D11 resources (implement IDisposable)
- [ ] T035 Create WgcInterop static class in src/WindowCaptureCL/Infrastructure/WgcInterop.cs for Windows Graphics Capture API bindings
- [ ] T036 Add CreateCaptureItemForWindow() method to WgcInterop using Windows.Graphics.Capture.GraphicsCaptureItem.CreateFromWindowId
- [ ] T037 Add CreateCaptureItemForMonitor() method to WgcInterop using GraphicsCaptureItem.CreateFromDisplayId
- [ ] T038 Add IsWGCSupported() method to WgcInterop checking Windows version (>= 10.0.18362) and API availability
- [ ] T039 Add GetDirect3DSurfaceFromFrame() helper method to WgcInterop for extracting ID3D11Texture2D from IDirect3DSurface

### API Layer - Interfaces

- [ ] T040 Create ICaptureSession interface in src/WindowCaptureCL/API/ICaptureSession.cs with IsRunning, SourceSize, TargetFPS, CaptureSource properties
- [ ] T041 Add Start(), Stop(), TakeScreenshot(), TakeScreenshotAsync(), Dispose() methods to ICaptureSession interface
- [ ] T042 Add FrameReady event (EventHandler<FrameReadyEventArgs>) to ICaptureSession interface
- [ ] T043 Add CaptureStopped event (EventHandler<CaptureStoppedEventArgs>) to ICaptureSession interface

### Unit Tests - Foundation

- [ ] T044 [P] Create ConfigurationTests.cs in tests/WindowCaptureCL.Tests/Unit/ (placeholder for Phase 5)
- [ ] T045 [P] Create ExceptionTests.cs in tests/WindowCaptureCL.Tests/Unit/ with tests verifying exception hierarchy, properties, and messages

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Single Window Screenshot for Automation (Priority: P1) 🎯 MVP

**Goal**: Enable developers to capture a single screenshot of a window by handle, even if obscured

**Independent Test**: Launch Notepad, get its window handle, call capture method, verify returned Bitmap contains window content

### Infrastructure for US1

- [ ] T046 [US1] Add GetWindowRect() P/Invoke method to WgcInterop for window size validation
- [ ] T047 [US1] Add IsWindow() P/Invoke method to WgcInterop for window handle validation

### Core Layer for US1

- [ ] T048 [US1] Create CaptureSession class implementing ICaptureSession in src/WindowCaptureCL/Core/CaptureSession.cs with constructor accepting CaptureSourceInfo
- [ ] T049 [US1] Implement IsRunning property in CaptureSession with private backing field and thread-safe access
- [ ] T050 [US1] Implement SourceSize property in CaptureSession returning current capture source dimensions
- [ ] T051 [US1] Implement TargetFPS property in CaptureSession with validation (1-120 range) and lock-based thread safety
- [ ] T052 [US1] Implement CaptureSource property in CaptureSession returning immutable CaptureSourceInfo
- [ ] T053 [US1] Add private field _graphicsCaptureItem for GraphicsCaptureItem in CaptureSession
- [ ] T054 [US1] Add private field _framePool for Direct3D11CaptureFramePool in CaptureSession
- [ ] T055 [US1] Add private field _captureSession for GraphicsCaptureSession in CaptureSession
- [ ] T056 [US1] Add private method InitializeWGCSession() to CaptureSession creating GraphicsCaptureItem from source (window only for US1)
- [ ] T057 [US1] Add private method CreateFramePool() to CaptureSession using DirectXManager device and FrameArrived event subscription
- [ ] T058 [US1] Implement TakeScreenshot() method in CaptureSession: initialize WGC session, wait for single frame, perform GPU-to-CPU copy, convert to Bitmap, cleanup
- [ ] T059 [US1] Add private method CopyTextureToBitmap() to CaptureSession: create staging texture, CopyResource, Map, copy pixels to Bitmap with stride handling, Unmap
- [ ] T060 [US1] Implement Dispose() method in CaptureSession releasing WGC session, frame pool, and DirectX resources
- [ ] T061 [US1] Add ObjectDisposedException guards to all public methods in CaptureSession

### API Layer for US1

- [ ] T062 [US1] Create static Capture class in src/WindowCaptureCL/API/Capture.cs
- [ ] T063 [US1] Implement Capture.FromWindow(IntPtr windowHandle) method validating handle with IsWindow(), creating CaptureSourceInfo with Window type, returning new CaptureSession
- [ ] T064 [US1] Add input validation to Capture.FromWindow() throwing ArgumentException for IntPtr.Zero, WindowNotFoundException for invalid handle

### Integration Tests for US1

- [ ] T065 [P] [US1] Create WindowCaptureTests.cs in tests/WindowCaptureCL.Tests/Integration/ with test launching Notepad and capturing valid window
- [ ] T066 [P] [US1] Add test to WindowCaptureTests.cs verifying captured Bitmap dimensions match window size
- [ ] T067 [P] [US1] Add test to WindowCaptureTests.cs verifying WindowNotFoundException for invalid handle (IntPtr with value 99999)
- [ ] T068 [P] [US1] Add test to WindowCaptureTests.cs verifying obscured window capture (launch second overlapping window, verify content)

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently - developers can capture window screenshots

---

## Phase 4: User Story 2 - Continuous Window Monitoring Stream (Priority: P2)

**Goal**: Enable continuous frame capture at controlled FPS with event-based delivery

**Independent Test**: Start capture session at 5 FPS for 10 seconds, verify ~50 frames delivered via FrameReady event with correct timing

### Core Layer for US2

- [ ] T069 [US2] Add private field _stopwatch (Stopwatch) to CaptureSession for FPS timing
- [ ] T070 [US2] Add private field _lastFrameTimestamp (long) to CaptureSession tracking last frame delivery time
- [ ] T071 [US2] Add private field _frameNumber (long) to CaptureSession for sequential frame numbering
- [ ] T072 [US2] Implement Start() method in CaptureSession: validate not already started, initialize WGC session and frame pool, start GraphicsCaptureSession, set IsRunning = true
- [ ] T073 [US2] Implement Stop() method in CaptureSession: set IsRunning = false, stop GraphicsCaptureSession, fire CaptureStopped event with UserRequested reason
- [ ] T074 [US2] Implement FrameReady event backing field and raising logic in CaptureSession
- [ ] T075 [US2] Implement CaptureStopped event backing field and raising logic in CaptureSession
- [ ] T076 [US2] Implement OnFrameArrived event handler in CaptureSession: check FPS throttle, if time elapsed >= target interval, process frame and fire FrameReady
- [ ] T077 [US2] Add FPS throttling logic to OnFrameArrived: calculate elapsed time since last frame, if < required interval (1000ms / TargetFPS), dispose frame immediately without GPU copy
- [ ] T078 [US2] In OnFrameArrived when frame passes throttle: perform GPU-to-CPU copy, create CapturedFrame with metadata, fire FrameReady event, update _lastFrameTimestamp and _frameNumber
- [ ] T079 [US2] Add window validation to OnFrameArrived: if window handle invalid (IsWindow returns false), fire CaptureStopped with SourceClosed reason and stop session
- [ ] T080 [US2] Add error handling to OnFrameArrived: catch exceptions, fire CaptureStopped with Error reason and exception, stop session gracefully
- [ ] T081 [US2] Implement TakeScreenshotAsync() method in CaptureSession as async Task<Bitmap> wrapper around TakeScreenshot()

### Integration Tests for US2

- [ ] T082 [P] [US2] Create ContinuousCaptureTests.cs in tests/WindowCaptureCL.Tests/Integration/ with test starting session at 10 FPS for 2 seconds
- [ ] T083 [P] [US2] Add test to ContinuousCaptureTests.cs verifying frame count (~20 frames in 2 seconds at 10 FPS with ±10% tolerance)
- [ ] T084 [P] [US2] Add test to ContinuousCaptureTests.cs verifying frame timing accuracy by analyzing timestamps
- [ ] T085 [P] [US2] Add test to ContinuousCaptureTests.cs verifying FrameReady event delivers non-null Bitmap with metadata
- [ ] T086 [P] [US2] Add test to ContinuousCaptureTests.cs closing window mid-capture and verifying CaptureStopped event fires with SourceClosed reason
- [ ] T087 [P] [US2] Add test to ContinuousCaptureTests.cs verifying FPS throttling: session at 30 FPS should skip frames if source updates at 60 FPS
- [ ] T088 [P] [US2] Add test to ContinuousCaptureTests.cs verifying SessionAlreadyStartedException when calling Start() on running session
- [ ] T089 [P] [US2] Add test to ContinuousCaptureTests.cs verifying TakeScreenshotAsync() returns valid Bitmap

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently - single capture and continuous streaming

---

## Phase 5: User Story 3 - Full Monitor Capture for Multi-Display Scenarios (Priority: P3)

**Goal**: Enable capture of entire monitor by index

**Independent Test**: Call Capture.FromScreen(0), verify returned Bitmap matches primary monitor resolution

### Infrastructure for US3

- [ ] T090 [US3] Add GetMonitorCount() method to WgcInterop using Screen.AllScreens.Length
- [ ] T091 [US3] Add GetMonitorBounds() method to WgcInterop accepting monitor index and returning Rectangle with screen bounds
- [ ] T092 [US3] Add GetMonitorHandle() method to WgcInterop converting monitor index to HMONITOR for WGC API

### Core Layer for US3

- [ ] T093 [US3] Update InitializeWGCSession() in CaptureSession to handle Monitor source type using CreateCaptureItemForMonitor()
- [ ] T094 [US3] Add monitor index validation to InitializeWGCSession(): check index >= 0 and < monitor count, throw InvalidMonitorException if out of range

### API Layer for US3

- [ ] T095 [US3] Implement Capture.FromScreen(int monitorIndex) method in src/WindowCaptureCL/API/Capture.cs
- [ ] T096 [US3] Add input validation to Capture.FromScreen(): ArgumentOutOfRangeException for monitorIndex < 0, InvalidMonitorException if >= monitor count
- [ ] T097 [US3] In Capture.FromScreen() create CaptureSourceInfo with Monitor type, set MonitorIndex, return new CaptureSession

### Integration Tests for US3

- [ ] T098 [P] [US3] Create MonitorCaptureTests.cs in tests/WindowCaptureCL.Tests/Integration/ with test capturing primary monitor (index 0)
- [ ] T099 [P] [US3] Add test to MonitorCaptureTests.cs verifying captured Bitmap dimensions match Screen.AllScreens[0] resolution
- [ ] T100 [P] [US3] Add test to MonitorCaptureTests.cs for InvalidMonitorException when index out of range (e.g., 99)
- [ ] T101 [P] [US3] Add test to MonitorCaptureTests.cs for continuous monitor capture at 15 FPS for 3 seconds

**Checkpoint**: All three capture modes work (window, monitor) - User Stories 1, 2, 3 complete

---

## Phase 6: User Story 4 - Precise Region Capture for Targeted Monitoring (Priority: P4)

**Goal**: Enable capture of specific rectangular region on a monitor

**Independent Test**: Define Rectangle(0, 0, 800, 600), capture region, verify Bitmap is exactly 800x600

### Infrastructure for US4

- [ ] T102 [US4] Add ValidateRegion() method to WgcInterop accepting monitor index and Rectangle, throwing RegionOutOfBoundsException if region extends beyond monitor bounds

### Core Layer for US4

- [ ] T103 [US4] Update InitializeWGCSession() in CaptureSession to handle Region source type: create GraphicsCaptureItem for monitor, configure frame pool with region bounds
- [ ] T104 [US4] Update CopyTextureToBitmap() in CaptureSession to handle region cropping: only copy pixels within region bounds from staging texture
- [ ] T105 [US4] Add region validation to InitializeWGCSession(): call ValidateRegion(), throw RegionOutOfBoundsException if invalid

### API Layer for US4

- [ ] T106 [US4] Implement Capture.FromScreenRegion(int monitorIndex, Rectangle region) method in src/WindowCaptureCL/API/Capture.cs
- [ ] T107 [US4] Add input validation to Capture.FromScreenRegion(): ArgumentOutOfRangeException for monitorIndex < 0, ArgumentException for region with Width/Height <= 0
- [ ] T108 [US4] In Capture.FromScreenRegion() create CaptureSourceInfo with Region type, set MonitorIndex and Region, return new CaptureSession

### Integration Tests for US4

- [ ] T109 [P] [US4] Create RegionCaptureTests.cs in tests/WindowCaptureCL.Tests/Integration/ with test capturing Rectangle(0, 0, 800, 600) on monitor 0
- [ ] T110 [P] [US4] Add test to RegionCaptureTests.cs verifying captured Bitmap is exactly 800x600 pixels
- [ ] T111 [P] [US4] Add test to RegionCaptureTests.cs for RegionOutOfBoundsException when region extends beyond monitor (e.g., x: 10000, y: 10000)
- [ ] T112 [P] [US4] Add test to RegionCaptureTests.cs for continuous region capture at 20 FPS for 2 seconds

**Checkpoint**: All four capture modes work (window, monitor, region) - User Stories 1-4 complete

---

## Phase 7: User Story 5 - Global Capture Configuration for Consistent Behavior (Priority: P5)

**Goal**: Enable global configuration of cursor visibility, FPS defaults, and other settings

**Independent Test**: Set CaptureConfiguration.IncludeCursor = false, capture window, verify no cursor in Bitmap

### API Layer for US5

- [ ] T113 [US5] Create CaptureConfiguration static class in src/WindowCaptureCL/API/CaptureConfiguration.cs
- [ ] T114 [US5] Add private static _lock object to CaptureConfiguration for thread synchronization
- [ ] T115 [US5] Add IncludeCursor static property to CaptureConfiguration with private backing field, getter/setter using lock
- [ ] T116 [US5] Add DrawBorder static property to CaptureConfiguration with private backing field, getter/setter using lock (default: false)
- [ ] T117 [US5] Add DefaultTargetFPS static property to CaptureConfiguration with validation (1-120 range) in setter using lock (default: 30)
- [ ] T118 [US5] Add MinimumFPS const (value: 1) to CaptureConfiguration
- [ ] T119 [US5] Add MaximumFPS const (value: 120) to CaptureConfiguration
- [ ] T120 [US5] Add static constructor to CaptureConfiguration initializing defaults: IncludeCursor = false, DrawBorder = false, DefaultTargetFPS = 30

### Core Layer for US5

- [ ] T121 [US5] Update CaptureSession constructor to read CaptureConfiguration.DefaultTargetFPS and set TargetFPS property
- [ ] T122 [US5] Update InitializeWGCSession() in CaptureSession to apply CaptureConfiguration.IncludeCursor to GraphicsCaptureSession.IsCursorCaptureEnabled
- [ ] T123 [US5] Update InitializeWGCSession() in CaptureSession to apply CaptureConfiguration.DrawBorder (if SourceType == Window) to GraphicsCaptureSession border settings

### Unit Tests for US5

- [ ] T124 [P] [US5] Update ConfigurationTests.cs in tests/WindowCaptureCL.Tests/Unit/ with test verifying IncludeCursor default is false
- [ ] T125 [P] [US5] Add test to ConfigurationTests.cs verifying DefaultTargetFPS default is 30
- [ ] T126 [P] [US5] Add test to ConfigurationTests.cs verifying DefaultTargetFPS setter validation throws ArgumentOutOfRangeException for value < 1 or > 120
- [ ] T127 [P] [US5] Add test to ConfigurationTests.cs verifying IncludeCursor setter thread safety (concurrent sets from multiple threads)
- [ ] T128 [P] [US5] Add test to ConfigurationTests.cs verifying DefaultTargetFPS setter thread safety

### Integration Tests for US5

- [ ] T129 [P] [US5] Create ConfigurationIntegrationTests.cs in tests/WindowCaptureCL.Tests/Integration/ with test setting IncludeCursor = false and verifying cursor absent in capture
- [ ] T130 [P] [US5] Add test to ConfigurationIntegrationTests.cs setting IncludeCursor = true and verifying cursor present in capture
- [ ] T131 [P] [US5] Add test to ConfigurationIntegrationTests.cs verifying new session uses DefaultTargetFPS value

**Checkpoint**: All user stories should now be independently functional - complete library feature set

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories and production readiness

### Documentation

- [ ] T132 [P] Add XML documentation comments to all public types and members in src/WindowCaptureCL/API/ directory
- [ ] T133 [P] Create README.md at repository root with library description, installation instructions, and link to quickstart
- [ ] T134 [P] Create LICENSE file at repository root (MIT License)
- [ ] T135 [P] Create .editorconfig file at repository root with C# coding style rules

### Sample Applications

- [ ] T136 Create BasicCapture console application in samples/BasicCapture/ demonstrating single window capture
- [ ] T137 Add Program.cs to BasicCapture with P/Invoke for FindWindow(), capture Notepad window, save to PNG
- [ ] T138 Create ContinuousMonitoring console application in samples/ContinuousMonitoring/ demonstrating streaming capture
- [ ] T139 Add Program.cs to ContinuousMonitoring with continuous capture at 30 FPS, frame count display, graceful shutdown

### Performance Testing

- [ ] T140 [P] Create CaptureBenchmarks.cs in tests/WindowCaptureCL.Tests/Performance/ using BenchmarkDotNet
- [ ] T141 [P] Add benchmark to CaptureBenchmarks.cs measuring single-frame capture latency (target: <100ms for 1080p)
- [ ] T142 [P] Add benchmark to CaptureBenchmarks.cs measuring sustained 60 FPS capture with frame timing variance
- [ ] T143 [P] Add benchmark to CaptureBenchmarks.cs measuring memory overhead per active session

### Code Quality

- [ ] T144 Run all integration tests and verify 100% pass rate
- [ ] T145 Run performance benchmarks and validate against success criteria from spec.md
- [ ] T146 [P] Review code for TODO comments and address or document as future work
- [ ] T147 [P] Run static analysis tool (e.g., Roslyn analyzers) and fix critical warnings
- [ ] T148 [P] Verify all IDisposable implementations follow correct pattern (finalizer if needed, dispose flag, etc.)

### NuGet Package Preparation

- [ ] T149 Update WindowCaptureCL.csproj with NuGet metadata: PackageId, Authors, Description, PackageTags, RepositoryUrl
- [ ] T150 Add package icon file to repository and reference in WindowCaptureCL.csproj
- [ ] T151 Generate NuGet package using dotnet pack and verify package contents
- [ ] T152 Test NuGet package installation in separate test project to validate dependencies and references

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-7)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3 → P4 → P5)
- **Polish (Phase 8)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Depends on User Story 1 (builds on CaptureSession from US1)
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - No dependencies on US1 or US2 (separate capture mode)
- **User Story 4 (P4)**: Depends on User Story 3 (builds on monitor capture infrastructure)
- **User Story 5 (P5)**: Can start after Foundational (Phase 2) - Affects all stories but independent implementation

### Within Each User Story

- Infrastructure tasks before Core tasks (interop before session implementation)
- Core tasks before API tasks (implementation before facade)
- API tasks before Integration tests (need public API to test)
- Tasks marked [P] can run in parallel within their layer

### Parallel Opportunities

- **Setup (Phase 1)**: Tasks T003-T014 can all run in parallel after T001-T002
- **Foundational (Phase 2)**: Tasks T015-T029 (exception hierarchy and types) can all run in parallel
- **Within User Story 1**: T046-T047 (interop), T065-T068 (tests) can run in parallel
- **Within User Story 2**: T082-T089 (tests) can run in parallel
- **Within User Story 3**: T090-T092 (interop), T098-T101 (tests) can run in parallel
- **Within User Story 4**: T109-T112 (tests) can run in parallel
- **Within User Story 5**: T124-T128 (unit tests), T129-T131 (integration tests) can run in parallel
- **Polish (Phase 8)**: T132-T135 (documentation), T140-T143 (benchmarks), T146-T148 (quality) can run in parallel

---

## Parallel Example: User Story 1 Infrastructure

```bash
# Launch all interop tasks for User Story 1 together:
Task: "Add GetWindowRect() P/Invoke method to WgcInterop" (T046)
Task: "Add IsWindow() P/Invoke method to WgcInterop" (T047)

# Both can be worked on simultaneously as they modify the same file with different methods
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T014)
2. Complete Phase 2: Foundational (T015-T045) - CRITICAL prerequisite
3. Complete Phase 3: User Story 1 (T046-T068)
4. **STOP and VALIDATE**: Test User Story 1 independently
5. Deploy/demo if ready (single window capture works)

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready (T001-T045)
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!) (T046-T068)
3. Add User Story 2 → Test independently → Deploy/Demo (continuous streaming) (T069-T089)
4. Add User Story 3 → Test independently → Deploy/Demo (monitor capture) (T090-T101)
5. Add User Story 4 → Test independently → Deploy/Demo (region capture) (T102-T112)
6. Add User Story 5 → Test independently → Deploy/Demo (configuration) (T113-T131)
7. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together (T001-T045)
2. Once Foundational is done:
   - Developer A: User Story 1 (T046-T068)
   - Developer B: User Story 3 (T090-T101) - independent of US1/US2
   - Developer C: User Story 5 (T113-T131) - independent of US1-US4
3. After US1 completes:
   - Developer A moves to User Story 2 (T069-T089) - depends on US1
4. After US3 completes:
   - Developer B moves to User Story 4 (T102-T112) - depends on US3
5. Stories complete and integrate independently

---

## Task Count Summary

- **Phase 1 (Setup)**: 14 tasks
- **Phase 2 (Foundational)**: 31 tasks
- **Phase 3 (User Story 1)**: 23 tasks
- **Phase 4 (User Story 2)**: 21 tasks
- **Phase 5 (User Story 3)**: 12 tasks
- **Phase 6 (User Story 4)**: 11 tasks
- **Phase 7 (User Story 5)**: 19 tasks
- **Phase 8 (Polish)**: 21 tasks

**Total**: 152 tasks

**Parallel Tasks**: 89 tasks marked [P] (58.6% parallelizable)

---

## Notes

- [P] tasks = different files or independent methods, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group of related tasks
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
- All file paths are absolute from repository root
- Tasks are ordered logically: Infrastructure → Core → API within each user story phase
