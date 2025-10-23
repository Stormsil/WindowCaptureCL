# QuickStart Guide: WindowCaptureCL

**Purpose**: Get started with WindowCaptureCL in under 5 minutes

## Installation

### Prerequisites
- Windows 10 version 1903 or later / Windows 11
- .NET 6.0 SDK or later
- Visual Studio 2022 or JetBrains Rider (or any .NET-compatible IDE)

### Install NuGet Package
```bash
dotnet add package WindowCaptureCL
```

Or via Package Manager Console in Visual Studio:
```powershell
Install-Package WindowCaptureCL
```

---

## Example 1: Capture a Single Window Screenshot

The simplest use case - capture one frame from a window:

```csharp
using WindowCaptureCL;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

class Program
{
    // P/Invoke to find window by title
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    static void Main()
    {
        // Find Notepad window (launch Notepad first!)
        IntPtr hwnd = FindWindow(null, "Untitled - Notepad");

        if (hwnd == IntPtr.Zero)
        {
            Console.WriteLine("Notepad window not found. Please open Notepad.");
            return;
        }

        // Create capture session and take screenshot
        using (var session = Capture.FromWindow(hwnd))
        using (Bitmap screenshot = session.TakeScreenshot())
        {
            screenshot.Save("notepad_capture.png", ImageFormat.Png);
            Console.WriteLine("Screenshot saved to notepad_capture.png");
        }
    }
}
```

**Result**: A PNG file containing the Notepad window content, even if it was obscured by other windows!

---

## Example 2: Continuous Window Monitoring (30 FPS)

Capture a stream of frames from a window in real-time:

```csharp
using WindowCaptureCL;
using System;
using System.Drawing;
using System.Threading;
using System.Runtime.InteropServices;

class Program
{
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    static void Main()
    {
        IntPtr hwnd = FindWindow(null, "Untitled - Notepad");

        if (hwnd == IntPtr.Zero)
        {
            Console.WriteLine("Notepad window not found.");
            return;
        }

        using (var session = Capture.FromWindow(hwnd))
        {
            // Set target frame rate
            session.TargetFPS = 30;

            // Subscribe to frame events
            int frameCount = 0;
            session.FrameReady += (sender, e) =>
            {
                frameCount++;
                Console.WriteLine($"Frame {frameCount}: {e.Frame.SourceSize.Width}x{e.Frame.SourceSize.Height} at {e.Frame.Timestamp:HH:mm:ss.fff}");

                // Process frame here (e.g., analyze content, save to video, etc.)
                // ...

                // IMPORTANT: Dispose the bitmap when done
                e.Frame.Frame.Dispose();
            };

            // Handle capture stoppage
            session.CaptureStopped += (sender, e) =>
            {
                Console.WriteLine($"Capture stopped: {e.Reason}");
                if (e.Reason == CaptureStopReason.Error)
                {
                    Console.WriteLine($"Error: {e.Exception.Message}");
                }
            };

            // Start capturing
            Console.WriteLine("Starting capture... Press Enter to stop.");
            session.Start();

            // Wait for user input
            Console.ReadLine();

            // Stop capture
            session.Stop();
            Console.WriteLine($"Captured {frameCount} frames total.");
        }
    }
}
```

**Result**: Console output showing frame capture at ~30 FPS with timestamps

---

## Example 3: Capture Full Monitor

Capture the entire primary monitor:

```csharp
using WindowCaptureCL;
using System;
using System.Drawing;
using System.Drawing.Imaging;

class Program
{
    static void Main()
    {
        // Capture primary monitor (index 0)
        using (var session = Capture.FromScreen(0))
        using (Bitmap screenshot = session.TakeScreenshot())
        {
            Console.WriteLine($"Captured monitor: {screenshot.Width}x{screenshot.Height}");
            screenshot.Save("monitor_capture.png", ImageFormat.Png);
            Console.WriteLine("Screenshot saved to monitor_capture.png");
        }
    }
}
```

**Multi-Monitor Setup**: Use index 1, 2, etc. for secondary monitors:
```csharp
var session = Capture.FromScreen(1);  // Second monitor
```

---

## Example 4: Capture Specific Screen Region

Capture only a portion of the screen (e.g., a status bar or specific UI area):

```csharp
using WindowCaptureCL;
using System;
using System.Drawing;
using System.Drawing.Imaging;

class Program
{
    static void Main()
    {
        // Capture top-left 800x600 region of primary monitor
        var region = new Rectangle(0, 0, 800, 600);

        using (var session = Capture.FromScreenRegion(0, region))
        using (Bitmap screenshot = session.TakeScreenshot())
        {
            screenshot.Save("region_capture.png", ImageFormat.Png);
            Console.WriteLine($"Region captured: {screenshot.Width}x{screenshot.Height}");
        }
    }
}
```

---

## Example 5: Configure Global Settings

Set global configuration that applies to all captures:

```csharp
using WindowCaptureCL;
using System;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

class Program
{
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    static void Main()
    {
        // Configure global settings BEFORE creating sessions
        CaptureConfiguration.IncludeCursor = true;   // Show mouse cursor
        CaptureConfiguration.DrawBorder = false;     // No border around windows
        CaptureConfiguration.DefaultTargetFPS = 60;  // New sessions default to 60 FPS

        IntPtr hwnd = FindWindow(null, "Untitled - Notepad");

        using (var session = Capture.FromWindow(hwnd))
        {
            // Session inherits global configuration
            Console.WriteLine($"Session FPS: {session.TargetFPS}");  // Outputs: 60

            using (var screenshot = session.TakeScreenshot())
            {
                screenshot.Save("with_cursor.png", ImageFormat.Png);
                Console.WriteLine("Screenshot with cursor saved");
            }
        }
    }
}
```

---

## Example 6: Async Screenshot

Use async/await for non-blocking capture:

```csharp
using WindowCaptureCL;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

class Program
{
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    static async Task Main()
    {
        IntPtr hwnd = FindWindow(null, "Untitled - Notepad");

        using (var session = Capture.FromWindow(hwnd))
        {
            Console.WriteLine("Capturing asynchronously...");
            Bitmap screenshot = await session.TakeScreenshotAsync();

            try
            {
                await Task.Run(() => screenshot.Save("async_capture.png", ImageFormat.Png));
                Console.WriteLine("Async screenshot saved");
            }
            finally
            {
                screenshot.Dispose();
            }
        }
    }
}
```

---

## Example 7: Error Handling

Properly handle exceptions that may occur during capture:

```csharp
using WindowCaptureCL;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

class Program
{
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    static void Main()
    {
        IntPtr hwnd = FindWindow(null, "Untitled - Notepad");

        try
        {
            using (var session = Capture.FromWindow(hwnd))
            {
                using (var screenshot = session.TakeScreenshot())
                {
                    screenshot.Save("capture.png");
                    Console.WriteLine("Success!");
                }
            }
        }
        catch (WindowNotFoundException ex)
        {
            Console.WriteLine($"Window not found: {ex.Message}");
            Console.WriteLine($"Handle: {ex.WindowHandle}");
        }
        catch (GraphicsCaptureNotSupportedException ex)
        {
            Console.WriteLine($"Windows Graphics Capture not supported: {ex.Message}");
            Console.WriteLine("Please upgrade to Windows 10 version 1903 or later.");
        }
        catch (GraphicsDeviceException ex)
        {
            Console.WriteLine($"Graphics device error: {ex.Message}");
            Console.WriteLine("Check GPU drivers and DirectX support.");
        }
        catch (CaptureException ex)
        {
            Console.WriteLine($"Capture error: {ex.Message}");
        }
    }
}
```

---

## Example 8: High-Performance 60 FPS Capture

For performance-critical scenarios (game monitoring, screen recording):

```csharp
using WindowCaptureCL;
using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

class Program
{
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    static void Main()
    {
        IntPtr hwnd = FindWindow(null, "Untitled - Notepad");

        using (var session = Capture.FromWindow(hwnd))
        {
            session.TargetFPS = 60;  // High frame rate

            var stopwatch = Stopwatch.StartNew();
            int frameCount = 0;
            long totalProcessingTime = 0;

            session.FrameReady += (sender, e) =>
            {
                var frameStart = Stopwatch.GetTimestamp();

                // YOUR FAST PROCESSING CODE HERE
                // Keep this under 16ms for 60 FPS!
                // Example: Copy frame to video encoder, analyze pixels, etc.

                var frameEnd = Stopwatch.GetTimestamp();
                long processingMicros = (frameEnd - frameStart) * 1000000 / Stopwatch.Frequency;
                totalProcessingTime += processingMicros;

                frameCount++;

                // Dispose immediately
                e.Frame.Frame.Dispose();
            };

            session.Start();

            // Run for 10 seconds
            Thread.Sleep(10000);

            session.Stop();
            stopwatch.Stop();

            double actualFPS = frameCount / stopwatch.Elapsed.TotalSeconds;
            double avgProcessingMs = totalProcessingTime / (double)frameCount / 1000.0;

            Console.WriteLine($"Captured {frameCount} frames in {stopwatch.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"Actual FPS: {actualFPS:F2}");
            Console.WriteLine($"Avg processing time per frame: {avgProcessingMs:F2}ms");
        }
    }
}
```

---

## Common Patterns & Best Practices

### ✅ DO: Always Dispose Resources
```csharp
// Use 'using' statements
using (var session = Capture.FromWindow(hwnd))
using (var screenshot = session.TakeScreenshot())
{
    // Work with screenshot
}

// Or explicit disposal in event handlers
session.FrameReady += (s, e) =>
{
    try
    {
        ProcessFrame(e.Frame.Frame);
    }
    finally
    {
        e.Frame.Frame.Dispose();
    }
};
```

### ✅ DO: Check Windows Version
```csharp
try
{
    var session = Capture.FromWindow(hwnd);
}
catch (GraphicsCaptureNotSupportedException)
{
    Console.WriteLine("Windows 10 1903+ or Windows 11 required");
}
```

### ✅ DO: Validate Window Handles
```csharp
IntPtr hwnd = GetWindowHandle();
if (hwnd == IntPtr.Zero)
{
    Console.WriteLine("Invalid window handle");
    return;
}
```

### ❌ DON'T: Block in Event Handlers
```csharp
// BAD: Blocking operation in event handler
session.FrameReady += (s, e) =>
{
    Thread.Sleep(100);  // DON'T DO THIS!
    e.Frame.Frame.Dispose();
};

// GOOD: Queue for processing on another thread
session.FrameReady += (s, e) =>
{
    var frameCopy = e.Frame.Frame.Clone() as Bitmap;
    Task.Run(() => SlowProcessing(frameCopy));
    e.Frame.Frame.Dispose();
};
```

### ❌ DON'T: Keep References to Frames
```csharp
// BAD: Storing frame reference without cloning
List<Bitmap> frames = new List<Bitmap>();
session.FrameReady += (s, e) =>
{
    frames.Add(e.Frame.Frame);  // DON'T DO THIS!
};

// GOOD: Clone if you need to keep it
List<Bitmap> frames = new List<Bitmap>();
session.FrameReady += (s, e) =>
{
    frames.Add(e.Frame.Frame.Clone() as Bitmap);
    e.Frame.Frame.Dispose();
};
```

---

## Performance Tips

1. **Lower FPS for Less Load**: Use 30 FPS for monitoring, 60 FPS only for high-speed scenarios
2. **Dispose Quickly**: Process frames and dispose bitmaps as fast as possible to avoid memory buildup
3. **Async Processing**: Offload heavy processing to background threads
4. **Region Capture**: Capture only the area you need (smaller region = less data transfer)
5. **Monitor Memory**: Watch for memory leaks if you forget to dispose bitmaps

---

## Troubleshooting

### "GraphicsCaptureNotSupportedException"
**Solution**: Upgrade to Windows 10 version 1903 or later

### "WindowNotFoundException"
**Solution**: Verify window exists before capturing, handle window closure gracefully

### "Permission denied" or "Access denied"
**Solution**: Some windows (elevated/system processes) cannot be captured without elevation

### Poor Performance (<30 FPS)
**Solution**: Check GPU drivers, reduce target FPS, use region capture instead of full window

### High Memory Usage
**Solution**: Ensure you're disposing all Bitmap objects in FrameReady event handlers

---

## Next Steps

- Read the full [API Documentation](./contracts/API.md)
- Explore [Data Model](./data-model.md) for entity details
- Review [Technical Research](./research.md) for implementation insights
- Check [Examples Repository](https://github.com/YourRepo/WindowCaptureCL-Examples) for more advanced scenarios

---

## Support

- **Issues**: Report bugs on GitHub Issues
- **Discussions**: Ask questions on GitHub Discussions
- **Documentation**: Full API reference at [docs.yoursite.com](https://docs.yoursite.com)

---

## License

MIT License - See LICENSE file for details
