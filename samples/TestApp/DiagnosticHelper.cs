using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace TestApp;

public static class DiagnosticHelper
{
    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    public static void RunDiagnostics()
    {
        Console.WriteLine("\n=== System Diagnostics ===");

        // Windows Version
        var osVersion = Environment.OSVersion;
        Console.WriteLine($"OS Version: {osVersion.Version.Major}.{osVersion.Version.Minor}.{osVersion.Version.Build}");
        Console.WriteLine($"OS Platform: {osVersion.Platform}");

        // Check Windows 10 version requirement
        if (osVersion.Version.Build >= 18362)
        {
            Console.WriteLine("✓ Windows version supports Graphics Capture API (build >= 18362)");
        }
        else
        {
            Console.WriteLine($"✗ Windows version too old (build {osVersion.Version.Build}). Minimum required: 18362");
            return;
        }

        // DirectX Diagnostics
        Console.WriteLine("\n--- DirectX Diagnostics ---");

        try
        {
            // Try to create DXGI Factory
            using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
            Console.WriteLine("✓ DXGI Factory created successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ DXGI Factory creation failed: {ex.Message}");
        }

        // Try to create D3D11 Device
        Console.WriteLine("\n--- Direct3D 11 Device Creation ---");
        try
        {
            var featureLevels = new[]
            {
                FeatureLevel.Level_11_1,
                FeatureLevel.Level_11_0,
                FeatureLevel.Level_10_1,
                FeatureLevel.Level_10_0
            };

            var result = D3D11.D3D11CreateDevice(
                null,
                DriverType.Hardware,
                DeviceCreationFlags.BgraSupport,
                featureLevels,
                out ID3D11Device? device,
                out FeatureLevel selectedLevel,
                out ID3D11DeviceContext? context);

            if (result.Success && device != null)
            {
                Console.WriteLine($"✓ D3D11 Device created successfully");
                Console.WriteLine($"  Feature Level: {selectedLevel}");
                Console.WriteLine($"  Device Type: Hardware");

                // Check for threading support
                var threadingSupport = device.CheckThreadingSupport(out bool supportsConcurrent, out bool supportsCommand);
                Console.WriteLine($"  Concurrent Threading: {supportsConcurrent}");
                Console.WriteLine($"  Command List Threading: {supportsCommand}");

                device.Dispose();
                context?.Dispose();
            }
            else
            {
                Console.WriteLine($"✗ Failed to create D3D11 Device: {result}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ D3D11 Creation Error: {ex.Message}");
            Console.WriteLine($"  Inner Exception: {ex.InnerException?.Message}");
        }

        // Check WinRT Graphics Capture support
        Console.WriteLine("\n--- Windows Graphics Capture API ---");
        try
        {
            var captureType = Type.GetType("Windows.Graphics.Capture.GraphicsCaptureSession, Windows.Graphics.Capture");
            if (captureType != null)
            {
                Console.WriteLine("✓ Windows.Graphics.Capture assembly found");

                // Check if API is actually supported
                try
                {
                    var isSupportedMethod = captureType.GetMethod("IsSupported");
                    if (isSupportedMethod != null)
                    {
                        var isSupported = (bool?)isSupportedMethod.Invoke(null, null);
                        if (isSupported == true)
                        {
                            Console.WriteLine("✓ GraphicsCaptureSession.IsSupported() = true");
                        }
                        else
                        {
                            Console.WriteLine("✗ GraphicsCaptureSession.IsSupported() = false");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠ Could not check IsSupported: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("✗ Windows.Graphics.Capture assembly not found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ WinRT Error: {ex.Message}");
        }

        // Environment info
        Console.WriteLine("\n--- Environment ---");
        Console.WriteLine($"CLR Version: {Environment.Version}");
        Console.WriteLine($"64-bit Process: {Environment.Is64BitProcess}");
        Console.WriteLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
        Console.WriteLine($"Processor Count: {Environment.ProcessorCount}");

        // Check for remote desktop
        Console.WriteLine("\n--- Remote Desktop Check ---");
        var sessionName = Environment.GetEnvironmentVariable("SESSIONNAME");
        Console.WriteLine($"Session Name: {sessionName ?? "(null)"}");

        if (!string.IsNullOrEmpty(sessionName))
        {
            if (sessionName.StartsWith("RDP", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("⚠ WARNING: RDP session detected");
            }
            else if (sessionName.Equals("Console", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("✓ Local console session");
            }
        }

        Console.WriteLine("\n=== Diagnostics Complete ===\n");
    }
}
