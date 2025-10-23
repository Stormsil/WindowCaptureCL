using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using WindowCaptureCL;

Console.WriteLine("===========================================");
Console.WriteLine("   WindowCaptureCL - Test Application");
Console.WriteLine("===========================================");
Console.WriteLine();

// Check for remote desktop session
var sessionName = Environment.GetEnvironmentVariable("SESSIONNAME");
if (!string.IsNullOrEmpty(sessionName) && sessionName.StartsWith("RDP", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("⚠ WARNING: Remote Desktop session detected!");
    Console.WriteLine("  Windows Graphics Capture may not work in RDP/VNC/NoMachine sessions.");
    Console.WriteLine("  For best results, run on a physical machine with direct GPU access.");
    Console.WriteLine();
}

// Check if Windows Graphics Capture is supported
try
{
    // This will throw if not supported
    var testProcess = Process.GetProcessesByName("explorer").FirstOrDefault();
    if (testProcess != null)
    {
        Console.WriteLine("✓ Windows Graphics Capture API: Supported");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Windows Graphics Capture API: Not Supported");
    Console.WriteLine($"  Error: {ex.Message}");
    Console.WriteLine("\nPress any key to exit...");
    Console.ReadKey();
    return;
}

while (true)
{
    Console.WriteLine();
    Console.WriteLine("=== Main Menu ===");
    Console.WriteLine("1. Test Single Window Capture");
    Console.WriteLine("2. Test Continuous Window Capture (5 seconds)");
    Console.WriteLine("3. Test Monitor Capture");
    Console.WriteLine("4. Test Region Capture");
    Console.WriteLine("5. Test Global Configuration");
    Console.WriteLine("6. List Available Windows");
    Console.WriteLine("7. Run System Diagnostics");
    Console.WriteLine("0. Exit");
    Console.WriteLine();
    Console.Write("Select option: ");

    var choice = Console.ReadLine();

    try
    {
        switch (choice)
        {
            case "1":
                TestSingleWindowCapture();
                break;
            case "2":
                TestContinuousCapture();
                break;
            case "3":
                TestMonitorCapture();
                break;
            case "4":
                TestRegionCapture();
                break;
            case "5":
                TestGlobalConfiguration();
                break;
            case "6":
                ListAvailableWindows();
                break;
            case "7":
                TestApp.DiagnosticHelper.RunDiagnostics();
                break;
            case "0":
                Console.WriteLine("Exiting...");
                return;
            default:
                Console.WriteLine("Invalid option. Try again.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n✗ Error: {ex.GetType().Name}");
        Console.WriteLine($"  Message: {ex.Message}");

        // Show inner exception details
        if (ex.InnerException != null)
        {
            Console.WriteLine($"\n  Inner Exception: {ex.InnerException.GetType().Name}");
            Console.WriteLine($"  Inner Message: {ex.InnerException.Message}");

            if (ex.InnerException.InnerException != null)
            {
                Console.WriteLine($"\n  Inner Inner Exception: {ex.InnerException.InnerException.GetType().Name}");
                Console.WriteLine($"  Inner Inner Message: {ex.InnerException.InnerException.Message}");
            }
        }

        // Show stack trace for debugging
        Console.WriteLine($"\n  Stack Trace (first 3 lines):");
        var stackLines = ex.StackTrace?.Split('\n').Take(3);
        if (stackLines != null)
        {
            foreach (var line in stackLines)
            {
                Console.WriteLine($"    {line.Trim()}");
            }
        }

        if (ex is GraphicsDeviceException)
        {
            Console.WriteLine("\n⚠ Note: This error often occurs when:");
            Console.WriteLine("  - Running in Remote Desktop (RDP, VNC, NoMachine, etc.)");
            Console.WriteLine("  - Running in a VM without GPU passthrough");
            Console.WriteLine("  - GPU hardware acceleration is not available");
            Console.WriteLine("  - DirectX 11 is not properly initialized");
            Console.WriteLine("\n  Solution: Run this application on a physical machine with direct GPU access.");
        }
    }
}

void TestSingleWindowCapture()
{
    Console.WriteLine("\n=== Test Single Window Capture ===");
    Console.Write("Enter process name (e.g., 'notepad'): ");
    var processName = Console.ReadLine()?.Trim();

    if (string.IsNullOrEmpty(processName))
    {
        Console.WriteLine("Invalid process name.");
        return;
    }

    var process = Process.GetProcessesByName(processName).FirstOrDefault();
    if (process == null)
    {
        Console.WriteLine($"✗ Process '{processName}' not found. Make sure the application is running.");
        return;
    }

    Console.WriteLine($"✓ Found process: {process.ProcessName} (PID: {process.Id})");
    Console.WriteLine($"  Window Handle: 0x{process.MainWindowHandle:X}");
    Console.WriteLine($"  Window Title: {process.MainWindowTitle}");

    using var session = Capture.FromWindow(process.MainWindowHandle);

    Console.WriteLine($"  Source Type: {session.SourceInfo.SourceType}");
    Console.WriteLine($"  Display Name: {session.SourceInfo.DisplayName}");
    Console.WriteLine($"  Size: {session.SourceInfo.Width}x{session.SourceInfo.Height}");

    Console.WriteLine("\nCapturing frame...");
    var sw = Stopwatch.StartNew();
    using var frame = session.CaptureFrame();
    sw.Stop();

    Console.WriteLine($"✓ Frame captured in {sw.ElapsedMilliseconds}ms");
    Console.WriteLine($"  Bitmap Size: {frame.Bitmap.Width}x{frame.Bitmap.Height}");
    Console.WriteLine($"  Pixel Format: {frame.Bitmap.PixelFormat}");
    Console.WriteLine($"  Timestamp: {frame.Timestamp:HH:mm:ss.fff}");

    var filename = $"capture_{processName}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
    frame.Bitmap.Save(filename, ImageFormat.Png);
    Console.WriteLine($"✓ Saved to: {Path.GetFullPath(filename)}");
}

void TestContinuousCapture()
{
    Console.WriteLine("\n=== Test Continuous Capture ===");
    Console.Write("Enter process name (e.g., 'notepad'): ");
    var processName = Console.ReadLine()?.Trim();

    if (string.IsNullOrEmpty(processName))
    {
        Console.WriteLine("Invalid process name.");
        return;
    }

    var process = Process.GetProcessesByName(processName).FirstOrDefault();
    if (process == null)
    {
        Console.WriteLine($"✗ Process '{processName}' not found.");
        return;
    }

    Console.Write("Enter target FPS (1-120, default 30): ");
    var fpsInput = Console.ReadLine()?.Trim();
    int targetFPS = 30;
    if (!string.IsNullOrEmpty(fpsInput) && int.TryParse(fpsInput, out var fps))
    {
        targetFPS = Math.Clamp(fps, 1, 120);
    }

    using var session = Capture.FromWindow(process.MainWindowHandle);

    // Configure FPS
    var config = new CaptureConfiguration { MaxFramesPerSecond = targetFPS };
    session.UpdateConfiguration(config);

    int frameCount = 0;
    var captureStopwatch = Stopwatch.StartNew();
    var frameTimestamps = new List<long>();

    session.FrameReady += (sender, args) =>
    {
        frameCount++;
        frameTimestamps.Add(captureStopwatch.ElapsedMilliseconds);

        // Show progress every 10 frames
        if (frameCount % 10 == 0)
        {
            Console.Write($"\r  Captured {frameCount} frames... ({args.FrameNumber})");
        }

        args.Frame.Dispose(); // Don't forget to dispose!
    };

    session.CaptureError += (sender, args) =>
    {
        Console.WriteLine($"\n✗ Capture Error: {args.Exception.Message}");
    };

    session.CaptureStopped += (sender, args) =>
    {
        Console.WriteLine($"\n  Capture stopped: {args.Reason}");
        if (args.Exception != null)
        {
            Console.WriteLine($"  Exception: {args.Exception.Message}");
        }
    };

    Console.WriteLine($"\nStarting continuous capture at {targetFPS} FPS for 5 seconds...");
    Console.WriteLine("Press Ctrl+C to stop early (or wait 5 seconds)");

    session.StartCapture();
    Console.WriteLine($"✓ Capture started (IsActive: {session.IsActive})");

    Thread.Sleep(5000);

    session.StopCapture();
    captureStopwatch.Stop();

    Console.WriteLine($"\n\n✓ Capture Statistics:");
    Console.WriteLine($"  Total Frames: {frameCount}");
    Console.WriteLine($"  Total Frames Captured: {session.TotalFramesCaptured}");
    Console.WriteLine($"  Duration: {captureStopwatch.ElapsedMilliseconds}ms");
    Console.WriteLine($"  Average FPS: {frameCount / (captureStopwatch.ElapsedMilliseconds / 1000.0):F2}");
    Console.WriteLine($"  Target FPS: {targetFPS}");

    if (frameTimestamps.Count > 1)
    {
        var intervals = new List<double>();
        for (int i = 1; i < frameTimestamps.Count; i++)
        {
            intervals.Add(frameTimestamps[i] - frameTimestamps[i - 1]);
        }
        Console.WriteLine($"  Avg Frame Interval: {intervals.Average():F2}ms");
        Console.WriteLine($"  Min Frame Interval: {intervals.Min():F2}ms");
        Console.WriteLine($"  Max Frame Interval: {intervals.Max():F2}ms");
    }
}

void TestMonitorCapture()
{
    Console.WriteLine("\n=== Test Monitor Capture ===");

    // Show available monitors
    var monitorCount = System.Windows.Forms.Screen.AllScreens.Length;
    Console.WriteLine($"Available monitors: {monitorCount}");

    for (int i = 0; i < monitorCount; i++)
    {
        var screen = System.Windows.Forms.Screen.AllScreens[i];
        Console.WriteLine($"  [{i}] {screen.Bounds.Width}x{screen.Bounds.Height} " +
                         $"at ({screen.Bounds.X}, {screen.Bounds.Y}) " +
                         $"{(screen.Primary ? "(Primary)" : "")}");
    }

    Console.Write("\nEnter monitor index (0-based, default 0): ");
    var indexInput = Console.ReadLine()?.Trim();
    int monitorIndex = 0;
    if (!string.IsNullOrEmpty(indexInput) && int.TryParse(indexInput, out var idx))
    {
        monitorIndex = idx;
    }

    Console.WriteLine($"\nCapturing monitor {monitorIndex}...");

    using var session = Capture.FromScreen(monitorIndex);

    Console.WriteLine($"  Source Type: {session.SourceInfo.SourceType}");
    Console.WriteLine($"  Monitor Index: {session.SourceInfo.MonitorIndex}");
    Console.WriteLine($"  Size: {session.SourceInfo.Width}x{session.SourceInfo.Height}");

    var sw = Stopwatch.StartNew();
    using var frame = session.CaptureFrame();
    sw.Stop();

    Console.WriteLine($"✓ Frame captured in {sw.ElapsedMilliseconds}ms");
    Console.WriteLine($"  Bitmap Size: {frame.Bitmap.Width}x{frame.Bitmap.Height}");

    var filename = $"capture_monitor{monitorIndex}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
    frame.Bitmap.Save(filename, ImageFormat.Png);
    Console.WriteLine($"✓ Saved to: {Path.GetFullPath(filename)}");
}

void TestRegionCapture()
{
    Console.WriteLine("\n=== Test Region Capture ===");

    Console.Write("Enter monitor index (default 0): ");
    var indexInput = Console.ReadLine()?.Trim();
    int monitorIndex = 0;
    if (!string.IsNullOrEmpty(indexInput) && int.TryParse(indexInput, out var idx))
    {
        monitorIndex = idx;
    }

    Console.Write("Enter X coordinate (default 0): ");
    var xInput = Console.ReadLine()?.Trim();
    int x = 0;
    if (!string.IsNullOrEmpty(xInput) && int.TryParse(xInput, out var xVal))
    {
        x = xVal;
    }

    Console.Write("Enter Y coordinate (default 0): ");
    var yInput = Console.ReadLine()?.Trim();
    int y = 0;
    if (!string.IsNullOrEmpty(yInput) && int.TryParse(yInput, out var yVal))
    {
        y = yVal;
    }

    Console.Write("Enter Width (default 800): ");
    var widthInput = Console.ReadLine()?.Trim();
    int width = 800;
    if (!string.IsNullOrEmpty(widthInput) && int.TryParse(widthInput, out var wVal))
    {
        width = wVal;
    }

    Console.Write("Enter Height (default 600): ");
    var heightInput = Console.ReadLine()?.Trim();
    int height = 600;
    if (!string.IsNullOrEmpty(heightInput) && int.TryParse(heightInput, out var hVal))
    {
        height = hVal;
    }

    var region = new Rectangle(x, y, width, height);
    Console.WriteLine($"\nCapturing region: {region}");

    using var session = Capture.FromScreenRegion(monitorIndex, region);

    Console.WriteLine($"  Source Type: {session.SourceInfo.SourceType}");
    Console.WriteLine($"  Monitor Index: {session.SourceInfo.MonitorIndex}");
    Console.WriteLine($"  Region: {session.SourceInfo.Region}");

    var sw = Stopwatch.StartNew();
    using var frame = session.CaptureFrame();
    sw.Stop();

    Console.WriteLine($"✓ Frame captured in {sw.ElapsedMilliseconds}ms");
    Console.WriteLine($"  Bitmap Size: {frame.Bitmap.Width}x{frame.Bitmap.Height}");

    var filename = $"capture_region_{x}_{y}_{width}x{height}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
    frame.Bitmap.Save(filename, ImageFormat.Png);
    Console.WriteLine($"✓ Saved to: {Path.GetFullPath(filename)}");
}

void TestGlobalConfiguration()
{
    Console.WriteLine("\n=== Test Global Configuration ===");

    Console.WriteLine("\nCurrent Global Settings:");
    Console.WriteLine($"  DefaultTargetFPS: {CaptureConfiguration.DefaultTargetFPS}");
    Console.WriteLine($"  IncludeCursor: {CaptureConfiguration.IncludeCursor}");
    Console.WriteLine($"  DrawBorder: {CaptureConfiguration.DrawBorder}");
    Console.WriteLine($"  MinimumFPS: {CaptureConfiguration.MinimumFPS}");
    Console.WriteLine($"  MaximumFPS: {CaptureConfiguration.MaximumFPS}");

    Console.WriteLine("\n1. Change DefaultTargetFPS");
    Console.WriteLine("2. Toggle IncludeCursor");
    Console.WriteLine("3. Toggle DrawBorder");
    Console.WriteLine("0. Back to main menu");
    Console.Write("\nSelect option: ");

    var choice = Console.ReadLine();

    switch (choice)
    {
        case "1":
            Console.Write($"Enter new DefaultTargetFPS ({CaptureConfiguration.MinimumFPS}-{CaptureConfiguration.MaximumFPS}): ");
            if (int.TryParse(Console.ReadLine(), out var fps))
            {
                try
                {
                    CaptureConfiguration.DefaultTargetFPS = fps;
                    Console.WriteLine($"✓ DefaultTargetFPS set to {fps}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error: {ex.Message}");
                }
            }
            break;

        case "2":
            CaptureConfiguration.IncludeCursor = !CaptureConfiguration.IncludeCursor;
            Console.WriteLine($"✓ IncludeCursor set to {CaptureConfiguration.IncludeCursor}");
            break;

        case "3":
            CaptureConfiguration.DrawBorder = !CaptureConfiguration.DrawBorder;
            Console.WriteLine($"✓ DrawBorder set to {CaptureConfiguration.DrawBorder}");
            break;
    }
}

void ListAvailableWindows()
{
    Console.WriteLine("\n=== Available Windows ===");

    var processes = Process.GetProcesses()
        .Where(p => p.MainWindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(p.MainWindowTitle))
        .OrderBy(p => p.ProcessName)
        .ToList();

    Console.WriteLine($"Found {processes.Count} windows:\n");

    int count = 0;
    foreach (var process in processes)
    {
        count++;
        Console.WriteLine($"{count,3}. {process.ProcessName,-25} PID: {process.Id,6} Handle: 0x{process.MainWindowHandle:X8}");
        Console.WriteLine($"     Title: {process.MainWindowTitle}");

        if (count % 5 == 0 && count < processes.Count)
        {
            Console.Write("\nPress Enter to see more (or 'q' to quit)...");
            if (Console.ReadLine()?.ToLower() == "q")
                break;
        }
    }
}
