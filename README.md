# WindowCaptureCL

A high-performance .NET 8 library for capturing screenshots from windows, monitors, and screen regions using Windows Graphics Capture API.

## Features

- **Hardware-Accelerated Capture**: Leverages DirectX 11 and Windows Graphics Capture (WGC) API for efficient GPU-based screen capture
- **Multiple Capture Modes**:
  - Window capture (including obscured/minimized windows)
  - Full monitor capture
  - Rectangular region capture
- **Single-Frame and Continuous Capture**: Capture a single frame or stream frames continuously
- **FPS Control**: Configure frame rate from 1-120 FPS with built-in throttling
- **Event-Driven Architecture**: React to frame captures, errors, and session state changes via events
- **Thread-Safe**: All public APIs are thread-safe and can be used from multiple threads
- **Global Configuration**: Set default capture behavior globally or per-session

## Requirements

- **Platform**: Windows 10 version 1903 (19H1, build 18362) or later
- **Framework**: .NET 8.0
- **GPU**: DirectX 11 compatible graphics device

## Installation

```bash
dotnet add package WindowCaptureCL
```

## Quick Start

### Single Window Capture

```csharp
using WindowCaptureCL;

// Find a window handle (example using Process)
var notepad = Process.GetProcessesByName("notepad").FirstOrDefault();
if (notepad != null)
{
    using var session = Capture.FromWindow(notepad.MainWindowHandle);
    using var frame = session.CaptureFrame();

    // Save the captured bitmap
    frame.Bitmap.Save("screenshot.png", ImageFormat.Png);
}
```

### Continuous Capture with FPS Control

```csharp
using WindowCaptureCL;

var process = Process.GetProcessesByName("notepad").FirstOrDefault();
if (process != null)
{
    using var session = Capture.FromWindow(process.MainWindowHandle);

    // Configure FPS
    var config = new CaptureConfiguration { MaxFramesPerSecond = 30 };
    session.UpdateConfiguration(config);

    // Subscribe to frame events
    session.FrameReady += (sender, args) =>
    {
        Console.WriteLine($"Frame {args.FrameNumber} captured");
        // Process args.Frame (Bitmap)
        args.Frame.Dispose(); // Don't forget to dispose!
    };

    session.CaptureStopped += (sender, args) =>
    {
        Console.WriteLine($"Capture stopped: {args.Reason}");
    };

    // Start capturing
    session.StartCapture();

    // Let it run...
    Console.ReadLine();

    // Stop when done
    session.StopCapture();
}
```

### Monitor Capture

```csharp
using WindowCaptureCL;

// Capture primary monitor (index 0)
using var session = Capture.FromScreen(0);
using var frame = session.CaptureFrame();

frame.Bitmap.Save("monitor_capture.png", ImageFormat.Png);
```

### Region Capture

```csharp
using WindowCaptureCL;
using System.Drawing;

// Capture a 800x600 region from top-left of primary monitor
var region = new Rectangle(0, 0, 800, 600);
using var session = Capture.FromScreenRegion(0, region);
using var frame = session.CaptureFrame();

frame.Bitmap.Save("region_capture.png", ImageFormat.Png);
```

## Global Configuration

Set default behavior for all new capture sessions:

```csharp
// Set global defaults
CaptureConfiguration.DefaultTargetFPS = 60;
CaptureConfiguration.IncludeCursor = false;

// All new sessions will use these defaults
using var session = Capture.FromWindow(windowHandle);
// Session starts with 60 FPS and no cursor
```

## Architecture

WindowCaptureCL uses a three-layer architecture:

- **API Layer**: Public interfaces (`Capture`, `ICaptureSession`, event args)
- **Core Layer**: Implementation logic (`CaptureSession`)
- **Infrastructure Layer**: DirectX and WGC interop

## Performance

- Single frame capture latency: < 16ms (typically 5-10ms on modern hardware)
- Sustained 60 FPS capture with minimal CPU overhead
- Memory efficient: frame data is GPU-resident until copied to CPU

## Exception Handling

The library uses a comprehensive exception hierarchy:

- `CaptureException`: Base for all library exceptions
- `WindowNotFoundException`: Window handle is invalid or window closed
- `InvalidMonitorException`: Monitor index out of range
- `RegionOutOfBoundsException`: Capture region extends beyond screen
- `FrameCaptureException`: Frame capture operation failed
- `GraphicsDeviceException`: DirectX device initialization failed

## Thread Safety

All public APIs are thread-safe. You can:
- Call `CaptureFrame()` from multiple threads
- Start/stop capture from different threads
- Update configuration concurrently

## Limitations

- **Windows Only**: Uses Windows Graphics Capture API
- **Windows 10 1903+**: Requires recent Windows version (minimum build 18362)
- **DirectX 11**: Requires DX11-compatible GPU with hardware acceleration
- **Cursor Capture**: IsCursorCaptureEnabled requires Windows 10 version 2004 (19041) or later
- **Remote Desktop**: May not work in RDP, VNC, NoMachine, or other remote desktop sessions due to GPU limitations. The library requires direct GPU access and hardware acceleration.
- **Virtual Machines**: Requires GPU passthrough or proper virtual GPU support

## License

MIT License - see LICENSE file for details

## Contributing

Contributions are welcome! Please open an issue or pull request.

## Acknowledgments

Built with:
- [Vortice.Windows](https://github.com/amerkoleci/Vortice.Windows) - DirectX interop
- Windows Graphics Capture API - Hardware-accelerated screen capture

---

**Note**: This library captures screen content. Ensure you comply with applicable laws and regulations regarding screen recording and user privacy.
