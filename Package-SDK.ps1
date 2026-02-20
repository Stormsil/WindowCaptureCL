# Package-SDK.ps1 - Builds and packages WindowCaptureCL SDK

param(
    [string]$OutputPath = ".\WindowCaptureCL-SDK",
    [string]$Configuration = "Release"
)

Write-Host "WindowCaptureCL SDK Packaging Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Clean and build
Write-Host "[1/6] Building WindowCaptureCL..." -ForegroundColor Yellow
dotnet clean -c $Configuration | Out-Null
dotnet build -c $Configuration

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "[2/6] Collecting dependencies..." -ForegroundColor Yellow

# Publish to temp folder to collect dependencies
$publishTemp = ".\publish-temp"
dotnet publish -c $Configuration -o $publishTemp --no-self-contained 2>&1 | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed!" -ForegroundColor Red
    exit 1
}

Write-Host "[3/6] Creating SDK structure..." -ForegroundColor Yellow

# Create SDK structure
$sdkPath = $OutputPath
$assembliesPath = Join-Path $sdkPath "Assemblies"
$dependenciesPath = Join-Path $assembliesPath "Dependencies"
$docsPath = Join-Path $sdkPath "Documentation"
$examplesPath = Join-Path $sdkPath "Examples"

# Clean and create directories
Remove-Item $sdkPath -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $assembliesPath -Force | Out-Null
New-Item -ItemType Directory -Path $dependenciesPath -Force | Out-Null
New-Item -ItemType Directory -Path $docsPath -Force | Out-Null
New-Item -ItemType Directory -Path $examplesPath -Force | Out-Null

Write-Host "[4/6] Copying assemblies..." -ForegroundColor Yellow

# Copy main library files
Copy-Item "$publishTemp\WindowCaptureCL.dll" $assembliesPath
Copy-Item "$publishTemp\WindowCaptureCL.pdb" $assembliesPath
Copy-Item "$publishTemp\WindowCaptureCL.xml" $assembliesPath
Copy-Item "$publishTemp\WindowCaptureCL.deps.json" $assembliesPath

# Copy dependencies (all DLLs except WindowCaptureCL)
Get-ChildItem $publishTemp -Filter "*.dll" -Exclude "WindowCaptureCL.dll" |
    Copy-Item -Destination $dependenciesPath

# Copy runtimes folder if exists
$runtimesFolder = Join-Path $publishTemp "runtimes"
if (Test-Path $runtimesFolder) {
    Copy-Item $runtimesFolder $dependenciesPath -Recurse
    Write-Host "  - Copied runtimes folder" -ForegroundColor Gray
}

$dependencyCount = (Get-ChildItem $dependenciesPath -Filter "*.dll").Count
Write-Host "  - Copied $dependencyCount dependency DLLs" -ForegroundColor Gray

Write-Host "[5/6] Copying documentation..." -ForegroundColor Yellow

# Copy documentation
Copy-Item ".\Docs\API_REFERENCE.md" "$docsPath\WindowCaptureCL_Reference.md"
Copy-Item ".\Docs\README.md" "$docsPath\WindowCaptureCL_QuickStart.md"
Copy-Item ".\Docs\PACKAGING.md" $docsPath

# Copy CLAUDE.md if exists
if (Test-Path ".\CLAUDE.md") {
    Copy-Item ".\CLAUDE.md" $docsPath
}

# Create README for SDK
$version = Get-Date -Format "yyyy.MM.dd"
$readmeContent = @"
# WindowCaptureCL SDK

High-performance .NET library for Windows screen capture using DirectX 11 and Windows Graphics Capture API.

## Contents

- **Assemblies/** - Library DLL, PDB, XML documentation, and dependencies
- **Documentation/** - Complete API reference and quick start guide
- **Examples/** - (Reserved for future example projects)

## Quick Start

### 1. Add Reference to Your Project

**Option A: Direct DLL Reference**

Copy the Assemblies folder to your project and add this to your .csproj:

``````xml
<ItemGroup>
  <Reference Include="WindowCaptureCL">
    <HintPath>Libraries\WindowCaptureCL-SDK\Assemblies\WindowCaptureCL.dll</HintPath>
    <Private>True</Private>
  </Reference>
</ItemGroup>

<ItemGroup>
  <None Include="Libraries\WindowCaptureCL-SDK\Assemblies\Dependencies\*.dll">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
``````

### 2. Use in Your Code

``````csharp
using WindowCaptureCL;
using WindowCaptureCL.Infrastructure.WGC;

// Find a window by title
var windowInfo = WindowEnumerator.FindWindowByTitle("Calculator");
if (windowInfo != null)
{
    // Create capture session
    using var session = Capture.FromWindow(windowInfo.Handle);

    // Capture a frame
    using var frame = session.CaptureFrame();

    // Save to file
    frame.Save("screenshot.png");
}
``````

### 3. Enumerate Monitors

``````csharp
// Get all monitors
var monitors = MonitorEnumerator.GetAllMonitors();
foreach (var monitor in monitors)
{
    Console.WriteLine($"{monitor.DeviceName}: {monitor.Width}x{monitor.Height}");
}

// Capture primary monitor
using var session = Capture.FromScreen(0);
using var frame = session.CaptureFrame();
frame.Save("monitor.png");
``````

## Requirements

- .NET 8.0 for Windows
- Windows 10 version 1803 (April 2018 Update) or later
- DirectX 11 compatible graphics hardware

## Documentation

- **WindowCaptureCL_QuickStart.md** - Quick reference and common usage patterns
- **WindowCaptureCL_Reference.md** - Complete API documentation with all classes, methods, and examples
- **CLAUDE.md** - Developer guide for AI agents and codebase architecture
- **PACKAGING.md** - Build and packaging instructions

## Key Features

- ✅ Window capture by handle
- ✅ Full monitor/screen capture
- ✅ Region capture (rectangular area)
- ✅ Single-frame capture (sync/async)
- ✅ Continuous capture (1-120 FPS event-driven)
- ✅ Hardware-accelerated DirectX 11 pipeline
- ✅ Window discovery utilities (WindowEnumerator)
- ✅ Monitor discovery utilities (MonitorEnumerator)
- ✅ Cursor capture support
- ✅ Thread-safe implementation
- ✅ Complete XML documentation
- ✅ Comprehensive error handling

## Support

For issues, questions, or contributions, please visit the project repository.

**Version:** $version
**Target Framework:** .NET 8.0-windows10.0.19041.0
**Platform:** Windows 10 1803+
"@

Set-Content -Path (Join-Path $sdkPath "README.txt") -Value $readmeContent

Write-Host "[6/6] Finalizing..." -ForegroundColor Yellow

# Clean temp folder
Remove-Item $publishTemp -Recurse -Force

# Calculate package size
$packageSize = (Get-ChildItem $sdkPath -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
$packageSizeFormatted = "{0:N2}" -f $packageSize

Write-Host ""
Write-Host "=====================================" -ForegroundColor Green
Write-Host "SDK Package Created Successfully!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host "Location: $sdkPath" -ForegroundColor Cyan
Write-Host "Size: $packageSizeFormatted MB" -ForegroundColor Cyan
Write-Host ""
Write-Host "Contents:" -ForegroundColor Yellow
Write-Host "  - WindowCaptureCL.dll + XML docs" -ForegroundColor Gray
Write-Host "  - $dependencyCount dependency DLLs" -ForegroundColor Gray
Write-Host "  - Complete documentation" -ForegroundColor Gray
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Review the SDK contents in $sdkPath" -ForegroundColor Gray
Write-Host "  2. Test integration in a sample project" -ForegroundColor Gray
Write-Host "  3. Distribute or archive as needed" -ForegroundColor Gray
Write-Host ""
