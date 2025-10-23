# Testing WindowCaptureCL

## Requirements for Testing

WindowCaptureCL requires direct access to a DirectX 11-compatible GPU with hardware acceleration. This document explains how to properly test the library.

## ⚠ Remote Desktop Limitations

**The library will NOT work in the following environments:**

- Remote Desktop Protocol (RDP)
- VNC connections
- NoMachine or other remote desktop software
- Virtual machines without GPU passthrough
- Windows Server without Desktop Experience

**Error you'll see:**
```
GraphicsDeviceException: Failed to create Direct3D11 capture frame pool.
```

## ✓ Supported Testing Environments

1. **Physical Windows machine** with GPU
   - Best option for testing
   - Full hardware acceleration available

2. **Virtual machine with GPU passthrough**
   - Requires proper virtualization setup
   - VMware/Hyper-V with GPU support
   - Check VM documentation for GPU passthrough configuration

3. **Local Windows installation**
   - Not accessed via remote desktop
   - Direct monitor connection

## How to Test

### Option 1: Run TestApp Locally

If you have direct access to a Windows machine:

```bash
cd samples/TestApp
dotnet run
```

Or run the compiled executable:
```bash
samples\TestApp\bin\Release\net8.0-windows10.0.19041.0\TestApp.exe
```

### Option 2: Run Unit Tests

Unit tests validate functionality but will also fail without GPU:

```bash
dotnet test
```

### Option 3: Verify Build Only

If you cannot run the library due to remote desktop:

```bash
dotnet build --configuration Release
```

This confirms the code compiles correctly even if you can't test runtime functionality.

## TestApp Features

The TestApp console application provides interactive testing for:

1. **Single Window Capture** - Capture one frame from a window
2. **Continuous Capture** - Capture multiple frames with FPS control
3. **Monitor Capture** - Capture entire screen/monitor
4. **Region Capture** - Capture specific screen region
5. **Global Configuration** - Test configuration settings
6. **List Windows** - View available capture targets

## Troubleshooting

### "GraphicsDeviceException" Error

**Cause:** No GPU access or running in remote desktop

**Solutions:**
- Run on physical machine with direct GPU access
- Exit remote desktop session and run locally
- Configure VM GPU passthrough
- Test on different hardware

### "Windows Graphics Capture API: Not Supported"

**Cause:** Windows version too old

**Solution:**
- Update Windows to version 1903 (build 18362) or later
- Install latest Windows updates

### "Monitor index X is out of range"

**Cause:** Invalid monitor index

**Solution:**
- Use "List Available Windows" option first
- Check how many monitors are connected
- Use 0-based indexing (0 for first monitor)

### "Region extends beyond monitor bounds"

**Cause:** Region coordinates outside monitor area

**Solution:**
- Check monitor resolution first
- Ensure X + Width <= Monitor Width
- Ensure Y + Height <= Monitor Height

## Example Test Session

```
=== Main Menu ===
1. Test Single Window Capture
2. Test Continuous Window Capture (5 seconds)
3. Test Monitor Capture
4. Test Region Capture
5. Test Global Configuration
6. List Available Windows
0. Exit

Select option: 6
(View available windows and their process names)

Select option: 1
Enter process name: notepad
✓ Found process: notepad (PID: 12345)
✓ Frame captured in 45ms
✓ Saved to: capture_notepad_20250123_143052.png
```

## CI/CD Testing

For automated testing without GPU:

```bash
# Only build, don't run tests
dotnet build --configuration Release

# Package creation (doesn't require GPU)
dotnet pack --configuration Release
```

## Documentation

- **README.md** - Library overview and usage
- **LICENSE** - MIT license terms
- **TESTING.md** - This file
- **API Documentation** - XML comments in code

## Support

If you encounter issues not covered here:

1. Verify Windows version: `winver` (should be 1903+)
2. Check DirectX: `dxdiag` (should show DirectX 11+)
3. Review error messages carefully
4. Ensure not running via remote desktop

## Hardware Requirements Checklist

- [ ] Windows 10 version 1903+ or Windows 11
- [ ] DirectX 11 compatible GPU
- [ ] GPU drivers installed and updated
- [ ] Not running in remote desktop session
- [ ] Desktop Windows (not Windows Server Core)
- [ ] Hardware acceleration enabled in system settings

---

**Note**: The library is designed for production use on physical machines or properly configured VMs. Remote desktop testing limitations are a Windows Graphics Capture API restriction, not a library limitation.
