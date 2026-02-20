# WindowCaptureCL - Packaging Guide

This guide explains how to build, package, and distribute WindowCaptureCL as a standalone SDK for integration with other class libraries or applications.

## Table of Contents

- [Building Release Configuration](#building-release-configuration)
- [Collecting Dependencies](#collecting-dependencies)
- [Creating SDK Package](#creating-sdk-package)
- [Distribution Structure](#distribution-structure)
- [Integration Guide](#integration-guide)
- [Versioning and Updates](#versioning-and-updates)

---

## Building Release Configuration

### Prerequisites

- Visual Studio 2022 or later (recommended)
- .NET 8.0 SDK installed
- Windows 10 SDK version 10.0.19041.0 or later

### Option 1: Using Visual Studio

1. Open `WindowCaptureCL.csproj` in Visual Studio
2. Select **Release** configuration from the toolbar dropdown
3. Build > **Clean Solution**
4. Build > **Build Solution** (or press `Ctrl+Shift+B`)
5. Build artifacts will be in: `bin\Release\net8.0-windows10.0.19041.0\`

### Option 2: Using .NET CLI

```bash
# Navigate to project directory
cd C:\Users\warfr\source\repos\CLs\WindowCaptureCL

# Clean previous builds
dotnet clean -c Release

# Build Release configuration
dotnet build -c Release

# Build artifacts location:
# bin\Release\net8.0-windows10.0.19041.0\
```

### Build Output Files

After a successful Release build, you'll find these files in the output directory:

**Required for Distribution:**
- `WindowCaptureCL.dll` - The main library assembly
- `WindowCaptureCL.pdb` - Debug symbols (optional but recommended)
- `WindowCaptureCL.xml` - XML documentation for IntelliSense
- `WindowCaptureCL.deps.json` - Dependency manifest

**Not Required for Distribution:**
- `*.runtimeconfig.json` - Only for executable projects

---

## Collecting Dependencies

WindowCaptureCL requires several NuGet packages and their transitive dependencies. Here's how to collect them all.

### Method 1: Using dotnet publish (Recommended)

This automatically collects all dependencies:

```bash
# Publish to a temporary folder to collect all dependencies
dotnet publish -c Release -o ./publish-temp --no-self-contained

# This creates a folder with WindowCaptureCL.dll and ALL dependencies
```

The `publish-temp` folder will contain:
- WindowCaptureCL.dll and related files
- All NuGet package DLLs
- Any runtime-specific dependencies in `runtimes\` subfolder

### Method 2: Manual Collection from Build Output

After building, dependencies are already copied to the build output folder:
```
bin\Release\net8.0-windows10.0.19041.0\
```

**Required Dependencies:**
- `System.Drawing.Common.dll` (v9.0.10)
- `Vortice.Direct3D11.dll` (v3.6.2)
- `Vortice.Win32.dll` (v2.3.0)
- `Vortice.DXGI.dll` (transitive from Vortice.Direct3D11)
- `Vortice.DirectX.dll` (transitive from Vortice.Direct3D11)
- `Vortice.Mathematics.dll` (transitive from Vortice.Direct3D11)
- `SharpGen.Runtime.dll` (transitive from Vortice packages)
- `SharpGen.Runtime.COM.dll` (transitive from Vortice packages)
- `WinRT.Runtime.dll` (for Windows Runtime interop)
- `Microsoft.Windows.SDK.NET.dll` (Windows SDK projections)
- Any additional DLLs in the output folder

**Check for runtimes folder:**
Some packages may include native dependencies in a `runtimes\` folder. Include this folder if present.

### Method 3: From NuGet Cache

Dependencies are stored in your NuGet cache:
```
%USERPROFILE%\.nuget\packages\
```

Navigate to each package folder and collect the appropriate DLL from:
```
<package-name>\<version>\lib\net8.0\
```

For example:
```
system.drawing.common\9.0.10\lib\net8.0\System.Drawing.Common.dll
vortice.direct3d11\3.6.2\lib\net8.0\Vortice.Direct3D11.dll
```

---

## Creating SDK Package

### Recommended SDK Structure

Create the following folder structure for distribution:

```
WindowCaptureCL-SDK/
├── Assemblies/
│   ├── WindowCaptureCL.dll
│   ├── WindowCaptureCL.pdb
│   ├── WindowCaptureCL.xml
│   ├── WindowCaptureCL.deps.json
│   └── Dependencies/
│       ├── System.Drawing.Common.dll
│       ├── Vortice.Direct3D11.dll
│       ├── Vortice.Win32.dll
│       ├── Vortice.DXGI.dll
│       ├── Vortice.DirectX.dll
│       ├── Vortice.Mathematics.dll
│       ├── SharpGen.Runtime.dll
│       ├── SharpGen.Runtime.COM.dll
│       ├── WinRT.Runtime.dll
│       ├── Microsoft.Windows.SDK.NET.dll
│       └── runtimes/ (if present)
│
├── Documentation/
│   ├── WindowCaptureCL_Reference.md      (copy of API_REFERENCE.md)
│   ├── WindowCaptureCL_QuickStart.md     (copy of README.md)
│   ├── PACKAGING.md                      (this file)
│   └── CLAUDE.md                         (developer guide for AI agents)
│
├── Examples/
│   └── (optional: example projects or code snippets)
│
├── LICENSE.txt
└── README.txt (brief overview and links to documentation)
```

### Automated Packaging Script

Create a PowerShell script `Package-SDK.ps1`:

```powershell
# Package-SDK.ps1 - Builds and packages WindowCaptureCL SDK

param(
    [string]$OutputPath = ".\WindowCaptureCL-SDK",
    [string]$Configuration = "Release"
)

Write-Host "Building WindowCaptureCL..." -ForegroundColor Cyan

# Clean and build
dotnet clean -c $Configuration
dotnet build -c $Configuration

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Collecting dependencies..." -ForegroundColor Cyan

# Publish to temp folder to collect dependencies
$publishTemp = ".\publish-temp"
dotnet publish -c $Configuration -o $publishTemp --no-self-contained

# Create SDK structure
$sdkPath = $OutputPath
$assembliesPath = Join-Path $sdkPath "Assemblies"
$dependenciesPath = Join-Path $assembliesPath "Dependencies"
$docsPath = Join-Path $sdkPath "Documentation"

# Clean and create directories
Remove-Item $sdkPath -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $assembliesPath -Force | Out-Null
New-Item -ItemType Directory -Path $dependenciesPath -Force | Out-Null
New-Item -ItemType Directory -Path $docsPath -Force | Out-Null

Write-Host "Copying assemblies..." -ForegroundColor Cyan

# Copy main library files
Copy-Item "$publishTemp\WindowCaptureCL.dll" $assembliesPath
Copy-Item "$publishTemp\WindowCaptureCL.pdb" $assembliesPath
Copy-Item "$publishTemp\WindowCaptureCL.xml" $assembliesPath
Copy-Item "$publishTemp\WindowCaptureCL.deps.json" $assembliesPath

# Copy dependencies
Get-ChildItem $publishTemp -Filter "*.dll" -Exclude "WindowCaptureCL.dll" |
    Copy-Item -Destination $dependenciesPath

# Copy runtimes folder if exists
$runtimesFolder = Join-Path $publishTemp "runtimes"
if (Test-Path $runtimesFolder) {
    Copy-Item $runtimesFolder $dependenciesPath -Recurse
}

Write-Host "Copying documentation..." -ForegroundColor Cyan

# Copy documentation
Copy-Item ".\Docs\API_REFERENCE.md" "$docsPath\WindowCaptureCL_Reference.md"
Copy-Item ".\Docs\README.md" "$docsPath\WindowCaptureCL_QuickStart.md"
Copy-Item ".\Docs\PACKAGING.md" $docsPath
Copy-Item ".\CLAUDE.md" $docsPath -ErrorAction SilentlyContinue

# Create README for SDK
$readmeContent = @"
# WindowCaptureCL SDK

High-performance .NET library for Windows screen capture using DirectX 11 and Windows Graphics Capture API.

## Contents

- **Assemblies/** - Library DLL, PDB, XML documentation, and dependencies
- **Documentation/** - Complete API reference and quick start guide

## Quick Start

1. Add reference to WindowCaptureCL.dll in your project
2. Copy all DLLs from Assemblies/Dependencies/ to your output directory
3. See WindowCaptureCL_QuickStart.md for usage examples
4. See WindowCaptureCL_Reference.md for complete API documentation

## Requirements

- .NET 8.0 for Windows
- Windows 10 version 1803 or later
- DirectX 11 compatible graphics hardware

## Documentation

- **WindowCaptureCL_QuickStart.md** - Quick reference and common patterns
- **WindowCaptureCL_Reference.md** - Complete API documentation
- **CLAUDE.md** - Developer guide for AI agents
- **PACKAGING.md** - Build and packaging instructions

## Support

For issues, questions, or contributions, please visit the project repository.

Version: $(Get-Date -Format "yyyy.MM.dd")
"@

Set-Content -Path (Join-Path $sdkPath "README.txt") -Value $readmeContent

# Clean temp folder
Remove-Item $publishTemp -Recurse -Force

Write-Host "SDK package created successfully at: $sdkPath" -ForegroundColor Green
Write-Host "Package size: $((Get-ChildItem $sdkPath -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB) MB" -ForegroundColor Cyan
```

### Running the Packaging Script

```powershell
# Run from project root
.\Package-SDK.ps1

# Custom output path
.\Package-SDK.ps1 -OutputPath "C:\SDK-Output\WindowCaptureCL"
```

---

## Distribution Structure

### For Internal Use (Integration with Other CLs)

When integrating with other class libraries in your solution:

```
CLs/
├── WindowCaptureCL/           (this library)
├── SendSequenceCL/            (another library)
├── WindowManagerCL/           (another library)
└── Shared/
    └── WindowCaptureCL-SDK/   (packaged SDK for reference)
```

**Integration Pattern:**
- Other libraries reference the SDK package
- Each consuming library copies dependencies to its output
- OR use a shared dependencies folder at solution level

### For External Distribution

Create a ZIP or NuGet package:

**Option 1: ZIP Archive**
```bash
# Compress the SDK folder
Compress-Archive -Path WindowCaptureCL-SDK -DestinationPath WindowCaptureCL-SDK-v1.0.0.zip
```

**Option 2: NuGet Package** (Advanced)
Create a `.nuspec` file and use `nuget pack` to create a NuGet package for private or public distribution.

---

## Integration Guide

### Adding to Another .NET Project

#### Method 1: Direct DLL Reference

1. Copy the entire `WindowCaptureCL-SDK/Assemblies/` folder to your project
2. In your `.csproj`, add:

```xml
<ItemGroup>
  <!-- Reference main library -->
  <Reference Include="WindowCaptureCL">
    <HintPath>$(ProjectDir)Libraries\WindowCaptureCL-SDK\Assemblies\WindowCaptureCL.dll</HintPath>
    <Private>True</Private>
  </Reference>
</ItemGroup>

<ItemGroup>
  <!-- Copy dependencies to output -->
  <None Include="Libraries\WindowCaptureCL-SDK\Assemblies\Dependencies\*.dll">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

#### Method 2: Project Reference (Source Available)

If you have access to the source:

```xml
<ItemGroup>
  <ProjectReference Include="..\WindowCaptureCL\WindowCaptureCL.csproj" />
</ItemGroup>
```

#### Method 3: Local NuGet Package

Create a local NuGet package and reference it like any other NuGet package.

### Verifying Integration

Create a test program:

```csharp
using WindowCaptureCL;
using WindowCaptureCL.Infrastructure.WGC;

// Test basic functionality
var monitors = MonitorEnumerator.GetAllMonitors();
Console.WriteLine($"Found {monitors.Count} monitor(s)");

using var session = Capture.FromScreen(0);
using var frame = session.CaptureFrame();
Console.WriteLine($"Captured {frame.Width}x{frame.Height} frame");

// If this runs without exceptions, integration is successful
```

### Troubleshooting Integration Issues

**Issue: "Could not load file or assembly"**
- Ensure all dependencies from `Assemblies/Dependencies/` are copied to output directory
- Check that target framework matches (.NET 8.0-windows)

**Issue: "GraphicsCaptureNotSupportedException"**
- Target system must be Windows 10 1803+
- Not a packaging issue, platform limitation

**Issue: Missing IntelliSense**
- Ensure `WindowCaptureCL.xml` is in the same folder as `WindowCaptureCL.dll`

---

## Versioning and Updates

### Version Information

Version information is stored in `WindowCaptureCL.csproj`:

```xml
<PropertyGroup>
  <Version>1.0.0</Version>
  <AssemblyVersion>1.0.0.0</AssemblyVersion>
  <FileVersion>1.0.0.0</FileVersion>
</PropertyGroup>
```

Update these before building a new release.

### Dependency Tracking

Current dependencies (as of this guide):
- System.Drawing.Common: 9.0.10
- Vortice.Direct3D11: 3.6.2
- Vortice.Win32: 2.3.0

**When updating dependencies:**
1. Update NuGet packages
2. Rebuild in Release configuration
3. Re-run packaging script
4. Test packaged SDK
5. Update this guide with new version numbers

### Release Checklist

- [ ] Update version numbers in `.csproj`
- [ ] Update CHANGELOG (if exists)
- [ ] Clean solution
- [ ] Build Release configuration
- [ ] Run all tests (if test project exists)
- [ ] Run packaging script
- [ ] Verify SDK package contents
- [ ] Test integration in sample project
- [ ] Update documentation if API changed
- [ ] Tag git commit with version number
- [ ] Archive SDK package with version in filename

---

## Size Optimization

### Full Package (Recommended)

Includes all dependencies: ~5-10 MB

### Minimal Package (Advanced)

If consuming application already has some dependencies (e.g., System.Drawing.Common), you can exclude them:

```
Assemblies/
├── WindowCaptureCL.dll
├── WindowCaptureCL.pdb
├── WindowCaptureCL.xml
└── Dependencies/
    ├── Vortice.Direct3D11.dll
    ├── Vortice.Win32.dll
    └── [other Vortice and SharpGen dependencies]
```

**Note:** Only do this if you're certain the consuming application provides compatible versions of excluded dependencies.

---

## Multi-Library Solution Structure

When building multiple CL libraries that depend on each other:

```
CLs-Solution/
├── WindowCaptureCL/
│   ├── WindowCaptureCL.csproj
│   └── ...
│
├── SendSequenceCL/
│   ├── SendSequenceCL.csproj (references WindowCaptureCL)
│   └── ...
│
├── WindowManagerCL/
│   ├── WindowManagerCL.csproj (references WindowCaptureCL)
│   └── ...
│
└── Shared-Dependencies/
    └── (optional: shared dependency folder)
```

**Build Order:**
1. Build WindowCaptureCL first
2. Package WindowCaptureCL SDK
3. Other libraries reference WindowCaptureCL SDK or project directly

**Dependency Management Strategy:**
- Option A: Each library includes its own copy of dependencies
- Option B: Use a shared dependencies folder at solution level
- Option C: NuGet packages for internal distribution

---

## Continuous Integration

For automated builds (CI/CD pipelines):

```yaml
# Example: GitHub Actions
name: Build and Package SDK

on: [push, pull_request]

jobs:
  build:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build Release
      run: dotnet build -c Release --no-restore

    - name: Package SDK
      run: ./Package-SDK.ps1
      shell: pwsh

    - name: Upload SDK artifact
      uses: actions/upload-artifact@v3
      with:
        name: WindowCaptureCL-SDK
        path: ./WindowCaptureCL-SDK/
```

---

## Support and Maintenance

### Updating This Guide

When the build process, dependencies, or structure changes:
1. Update this PACKAGING.md file
2. Test the updated process end-to-end
3. Commit changes with clear description

### Common Package Updates

**Adding new dependencies:**
- Update `.csproj` with new NuGet package
- Rebuild and re-run packaging script
- No manual steps needed if using `dotnet publish` method

**Removing dependencies:**
- Remove from `.csproj`
- Rebuild and re-run packaging script
- Update this guide's dependency list

**API changes:**
- Update API_REFERENCE.md
- Update README.md if affects common usage
- Increment version number

---

## Questions and Troubleshooting

**Q: Can I use this library in .NET Framework projects?**
A: No, this library targets .NET 8.0-windows and requires features not available in .NET Framework.

**Q: Do consumers need to install DirectX?**
A: No, DirectX 11 is built into Windows 10 1803+. No additional installation needed.

**Q: Can I redistribute this as part of my application?**
A: Check the project license. Generally, yes, but ensure you comply with license terms for WindowCaptureCL and its dependencies.

**Q: How do I handle dependency conflicts?**
A: Use assembly binding redirects in consuming application's config, or ensure all projects use compatible dependency versions.

**Q: Can I create a single-file deployment?**
A: Yes, using `dotnet publish` with `/p:PublishSingleFile=true`, but this is application-level, not library-level.

---

## Additional Resources

- **Visual Studio Project Templates**: Consider creating a VSIX template for easy integration
- **NuGet.org**: For public distribution, publish as a NuGet package
- **GitHub Releases**: Attach SDK ZIP to GitHub releases for version tracking

---

**Last Updated:** 2026-01-25
**SDK Version:** 1.0.0 (Initial Release)
