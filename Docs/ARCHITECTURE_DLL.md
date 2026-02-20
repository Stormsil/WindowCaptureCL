# WindowCaptureCL - Архитектурный документ: Преобразование в самодостаточную DLL

> **Версия документа:** 1.0.0  
> **Дата:** 2025-01-30  
> **Статус:** Спецификация для реализации

---

## 1. Цель преобразования

### 1.1 Что значит "самодостаточная DLL"

**Самодостаточная DLL (Self-Contained DLL)** — это библиотека, которая:

- ✅ **Содержит все зависимости** — не требует отдельной установки NuGet пакетов
- ✅ **Работает без runtime-конфигурации** — не нужен `.runtimeconfig.json`
- ✅ **Имеет стабильное API** — минимум breaking changes между версиями
- ✅ **Легко интегрируется** — простое подключение из PowerShell, C#, COM
- ✅ **Явно управляет ресурсами** — четкие IDisposable паттерны

```
┌─────────────────────────────────────────────────────────────────┐
│                    Самодостаточная DLL                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              WindowCaptureCL.dll [единый файл]          │   │
│  ├─────────────────────────────────────────────────────────┤   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────┐  │   │
│  │  │   Core API  │  │   DirectX   │  │  WinRT Interop  │  │   │
│  │  │  [встроено] │  │ [ILMerged]  │  │   [встроено]    │  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────────┘  │   │
│  └─────────────────────────────────────────────────────────┘   │
│                              │                                  │
│                              ▼                                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  Потребители: PowerShell 7 │ .NET Projects │ COM interop │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 Требования к конечному результату

| Требование | Описание | Критичность |
|------------|----------|-------------|
| **Single-file deployment** | Все зависимости в одной DLL или рядом | Обязательно |
| **PowerShell 7 compatible** | Работа через `Add-Type` и `using assembly` | Обязательно |
| **.NET 6+ compatible** | Поддержка .NET 6, 7, 8+ | Обязательно |
| **COM interop** | Возможность регистрации как COM объект | Желательно |
| **No external runtime deps** | Только системные Windows библиотеки | Обязательно |
| **IntelliSense support** | XML документация в комплекте | Желательно |
| **x64 only** | Поддержка только x64 архитектуры | Обязательно |

### 1.3 Сценарии использования

#### PowerShell 7
```powershell
# Прямое использование DLL
Add-Type -Path "WindowCaptureCL.dll"
using namespace WindowCaptureCL

# Захват скриншота
$session = [Capture]::FromWindow($hwnd)
$frame = $session.CaptureFrame()
$frame.Save("screenshot.png")
```

#### .NET проекты
```csharp
// Прямая ссылка на DLL
using WindowCaptureCL;

// Использование
using var session = Capture.FromScreen(0);
var frame = await session.CaptureFrameAsync();
```

#### COM interop
```csharp
// Регистрация DLL для COM
regsvr32 WindowCaptureCL.dll

// Использование из VBScript/JScript
Set capture = CreateObject("WindowCaptureCL.Capture")
```

---

## 2. Текущее состояние анализ

### 2.1 Что работает хорошо

| Компонент | Состояние | Примечания |
|-----------|-----------|------------|
| **Архитектура API** | ✅ Отлично | Четкое разделение на `Capture`, `ICaptureSession`, `CapturedFrame` |
| **WGC интеграция** | ✅ Отлично | Корректная работа с Windows Graphics Capture API |
| **DirectX 11** | ✅ Отлично | Vortice.Direct3D11 стабильно работает |
| **Event-driven модель** | ✅ Хорошо | События `FrameReady`, `CaptureError`, `CaptureStopped` |
| **Async/await** | ✅ Хорошо | `CaptureFrameAsync()` реализован корректно |
| **Dispose паттерны** | ✅ Хорошо | `IDisposable` на всех уровнях |

### 2.2 Что нужно изменить

| Компонент | Проблема | Решение |
|-----------|----------|---------|
| **Зависимости** | 10+ DLL файлов | ILMerge/ILRepack или self-contained publish |
| **Public API** | Слишком много внутренних типов | Скрыть `Infrastructure` namespace |
| **Конфигурация** | Сложная для DLL | Упростить до fluent API |
| **Исключения** | Нет стандартного обработчика | Добавить `CaptureException` базовый класс |
| **Логирование** | Отсутствует | Добавить события для диагностики |

### 2.3 Блокирующие проблемы

| Проблема | Описание | Влияние | Решение |
|----------|----------|---------|---------|
| **WinRT Runtime** | Требуется `WinRT.Runtime.dll` | Среднее | Включить в поставку |
| **Vortice COM** | Зависимость от `SharpGen.Runtime.COM` | Среднее | ILMerge с осторожностью |
| **System.Drawing** | Требует `System.Drawing.Common` | Низкое | Стандартная зависимость |
| **Windows SDK** | `Microsoft.Windows.SDK.NET` большой | Высокое | Тримминг или ILLink |

---

## 3. Предлагаемая архитектура DLL

### 3.1 Структура проекта

```
WindowCaptureCL/
├── src/
│   ├── WindowCaptureCL/
│   │   ├── Public/                    # Публичное API
│   │   │   ├── Capture.cs             # Статический фасад
│   │   │   ├── ICaptureSession.cs     # Интерфейс сессии
│   │   │   ├── CapturedFrame.cs       # Результат захвата
│   │   │   ├── CaptureOptions.cs      # Упрощенные опции
│   │   │   └── Exceptions.cs          # Публичные исключения
│   │   │
│   │   ├── Internal/                  # Внутренняя реализация
│   │   │   ├── CaptureSession.cs      # Реализация ICaptureSession
│   │   │   ├── DirectX/
│   │   │   │   └── DirectXDeviceManager.cs
│   │   │   └── WGC/
│   │   │       ├── GraphicsCaptureHelper.cs
│   │   │       └── ...
│   │   │
│   │   └── Properties/
│   │       └── AssemblyInfo.cs        # COM visibility, версия
│   │
│   └── WindowCaptureCL.PowerShell/    # PowerShell модуль (опционально)
│       └── WindowCaptureCL.psd1
│
├── build/
│   ├── Build-DLL.ps1                  # Скрипт сборки
│   └── Merge-Dependencies.ps1         # ILRepack скрипт
│
└── tests/
    └── IntegrationTests/
```

### 3.2 Что оставить public, что сделать internal

#### Public API (стабильный контракт)

```csharp
namespace WindowCaptureCL;

// ✅ Оставить public
public static class Capture { }
public interface ICaptureSession : IDisposable { }
public sealed class CapturedFrame : IDisposable { }
public sealed class CaptureOptions { }
public class CaptureException : Exception { }
public class CaptureSourceNotFoundException : CaptureException { }

// ✅ Добавить новый
public static class CaptureDiagnostics 
{ 
    public static event EventHandler<DiagnosticEventArgs>? DiagnosticMessage;
}
```

#### Internal (скрыть от потребителей)

```csharp
namespace WindowCaptureCL.Internal;

// ❌ Сделать internal
internal sealed class CaptureSession : ICaptureSession { }
internal sealed class DirectXDeviceManager : IDisposable { }
internal static class GraphicsCaptureHelper { }
internal static class WgcInterop { }
internal static class FrameProcessor { }
```

### 3.3 Namespace структура

```
WindowCaptureCL                    # Корневой namespace - все public API
├── .Diagnostics                 # События и логирование (новое)
└── .Internal                    # internal types (не видны потребителям)
    ├── .DirectX
    └── .WGC
```

---

## 4. Упрощенное API для DLL

### 4.1 Фасадный класс для PowerShell/.NET

```csharp
namespace WindowCaptureCL;

/// <summary>
/// Упрощенный фасад для использования в PowerShell и .NET проектах.
/// </summary>
public static class Capture
{
    // ========== Синхронные методы ==========
    
    /// <summary>
    /// Захватывает окно по handle.
    /// </summary>
    public static ICaptureSession FromWindow(IntPtr windowHandle)
    {
        // Реализация
    }
    
    /// <summary>
    /// Захватывает монитор по индексу (0 = основной).
    /// </summary>
    public static ICaptureSession FromScreen(int monitorIndex = 0)
    {
        // Реализация
    }
    
    /// <summary>
    /// Захватывает регион экрана.
    /// </summary>
    public static ICaptureSession FromRegion(int monitorIndex, Rectangle region)
    {
        // Реализация
    }
    
    // ========== Упрощенные one-liner методы ==========
    
    /// <summary>
    /// Быстрый захват одного кадра окна.
    /// </summary>
    public static CapturedFrame CaptureWindow(IntPtr windowHandle, CaptureOptions? options = null)
    {
        using var session = FromWindow(windowHandle);
        if (options != null) session.Configure(options);
        return session.CaptureFrame();
    }
    
    /// <summary>
    /// Быстрый захват экрана.
    /// </summary>
    public static CapturedFrame CaptureScreen(int monitorIndex = 0, CaptureOptions? options = null)
    {
        using var session = FromScreen(monitorIndex);
        if (options != null) session.Configure(options);
        return session.CaptureFrame();
    }
    
    // ========== Асинхронные методы ==========
    
    /// <summary>
    /// Асинхронный захват окна.
    /// </summary>
    public static async Task<CapturedFrame> CaptureWindowAsync(
        IntPtr windowHandle, 
        CaptureOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var session = FromWindow(windowHandle);
        if (options != null) session.Configure(options);
        return await session.CaptureFrameAsync(cancellationToken);
    }
    
    /// <summary>
    /// Асинхронный захват экрана.
    /// </summary>
    public static async Task<CapturedFrame> CaptureScreenAsync(
        int monitorIndex = 0,
        CaptureOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var session = FromScreen(monitorIndex);
        if (options != null) session.Configure(options);
        return await session.CaptureFrameAsync(cancellationToken);
    }
    
    // ========== Утилиты ==========
    
    /// <summary>
    /// Проверяет поддержку Windows Graphics Capture.
    /// </summary>
    public static bool IsSupported => GraphicsCaptureHelper.IsSupported();
    
    /// <summary>
    /// Версия библиотеки.
    /// </summary>
    public static string Version => typeof(Capture).Assembly.GetName().Version?.ToString() ?? "1.0.0";
}
```

### 4.2 Упрощенная конфигурация

```csharp
namespace WindowCaptureCL;

/// <summary>
/// Упрощенные опции захвата для DLL сценариев.
/// </summary>
public sealed class CaptureOptions
{
    /// <summary>
    /// Включить захват курсора.
    /// </summary>
    public bool IncludeCursor { get; set; } = false;
    
    /// <summary>
    /// Рисовать рамку вокруг захваченного окна.
    /// </summary>
    public bool DrawBorder { get; set; } = false;
    
    /// <summary>
    /// Целевой FPS для непрерывного захвата (1-120).
    /// </summary>
    public int TargetFPS { get; set; } = 30;
    
    /// <summary>
    /// Таймаут ожидания кадра в миллисекундах.
    /// </summary>
    public int FrameTimeoutMs { get; set; } = 5000;
    
    /// <summary>
    /// Масштабирование результата (1.0 = оригинал).
    /// </summary>
    public double ScaleFactor { get; set; } = 1.0;
    
    // Fluent API
    public CaptureOptions WithCursor(bool include = true)
    {
        IncludeCursor = include;
        return this;
    }
    
    public CaptureOptions WithBorder(bool draw = true)
    {
        DrawBorder = draw;
        return this;
    }
    
    public CaptureOptions WithFPS(int fps)
    {
        TargetFPS = fps;
        return this;
    }
    
    public CaptureOptions WithTimeout(int milliseconds)
    {
        FrameTimeoutMs = milliseconds;
        return this;
    }
    
    public CaptureOptions WithScale(double factor)
    {
        ScaleFactor = factor;
        return this;
    }
}
```

### 4.3 Расширения ICaptureSession

```csharp
namespace WindowCaptureCL;

public interface ICaptureSession : IDisposable
{
    // Существующие члены...
    
    /// <summary>
    /// Применяет опции к сессии.
    /// </summary>
    void Configure(CaptureOptions options);
    
    /// <summary>
    /// Сохраняет кадр в файл (синхронно).
    /// </summary>
    void CaptureToFile(string filePath);
    
    /// <summary>
    /// Сохраняет кадр в файл (асинхронно).
    /// </summary>
    Task CaptureToFileAsync(string filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Получает кадр как массив байт (PNG формат).
    /// </summary>
    byte[] CaptureToByteArray();
    
    /// <summary>
    /// Получает кадр как MemoryStream (PNG формат).
    /// </summary>
    MemoryStream CaptureToStream();
}
```

---

## 5. Управление зависимостями

### 5.1 Какие NuGet пакеты нужны

| Пакет | Версия | Назначение | Стратегия |
|-------|--------|------------|-----------|
| `Vortice.Direct3D11` | 3.6.2 | DirectX 11 обертка | ILMerge |
| `Vortice.Win32` | 2.3.0 | Win32 interop | ILMerge |
| `Vortice.DXGI` | 3.6.2 | DXGI interop | ILMerge (transitive) |
| `Vortice.DirectX` | 3.6.2 | DirectX база | ILMerge (transitive) |
| `Vortice.Mathematics` | 3.6.2 | Математика | ILMerge (transitive) |
| `SharpGen.Runtime` | 2.0.0 | COM runtime | ILMerge (transitive) |
| `SharpGen.Runtime.COM` | 2.0.0 | COM поддержка | ILMerge (transitive) |
| `System.Drawing.Common` | 9.0.10 | Bitmap работа | External dependency |
| `WinRT.Runtime` | 2.0.0 | WinRT projection | External dependency |
| `Microsoft.Windows.SDK.NET` | 10.0.19041.0 | Windows SDK | External dependency |

### 5.2 Self-contained vs Framework-dependent

```xml
<!-- WindowCaptureCL.csproj - Self-contained конфигурация -->
<PropertyGroup>
  <!-- Self-contained публикация -->
  <SelfContained>true</SelfContained>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  
  <!-- Тримминг для уменьшения размера -->
  <PublishTrimmed>true</PublishTrimmed>
  <TrimMode>partial</TrimMode>
  
  <!-- Готовый к выполнению код -->
  <PublishReadyToRun>false</PublishReadyToRun>
  
  <!-- Однфайловая публикация -->
  <PublishSingleFile>false</PublishSingleFile>
</PropertyGroup>
```

**Рекомендация:** Использовать `framework-dependent` с ILRepack для слияния Vortice зависимостей.

### 5.3 ILMerge/ILRepack для слияния зависимостей

```powershell
# Merge-Dependencies.ps1
param(
    [string]$InputPath = ".\bin\Release\net8.0-windows10.0.19041.0\publish",
    [string]$OutputPath = ".\dist\WindowCaptureCL.dll"
)

$ilRepack = ".\tools\ILRepack.exe"
$primary = "$InputPath\WindowCaptureCL.dll"

# Список DLL для слияния (только Vortice/SharpGen)
$assemblies = @(
    "$InputPath\Vortice.Direct3D11.dll"
    "$InputPath\Vortice.Win32.dll"
    "$InputPath\Vortice.DXGI.dll"
    "$InputPath\Vortice.DirectX.dll"
    "$InputPath\Vortice.Mathematics.dll"
    "$InputPath\SharpGen.Runtime.dll"
    "$InputPath\SharpGen.Runtime.COM.dll"
)

# Выполнить слияние
& $ilRepack `
    /out:$OutputPath `
    /wildcards `
    /parallel `
    /union `
    $primary `
    @assemblies

# Копировать оставшиеся зависимости
Copy-Item "$InputPath\System.Drawing.Common.dll" ".\dist\"
Copy-Item "$InputPath\WinRT.Runtime.dll" ".\dist\"
Copy-Item "$InputPath\Microsoft.Windows.SDK.NET.dll" ".\dist\"
```

---

## 6. План миграции

### 6.1 Фаза 1: Рефакторинг API (1-2 дня)

#### 6.1.1 Упрощение публичного интерфейса

```csharp
// До: Сложная конфигурация
var config = new CaptureConfiguration();
config.IncludeCursor = true;
config.DrawBorder = false;
config.TargetFPS = 60;

// После: Fluent API
var options = new CaptureOptions()
    .WithCursor()
    .WithFPS(60);
```

#### 6.1.2 Скрытие внутренних типов

```csharp
// До: Public
namespace WindowCaptureCL.Infrastructure.WGC;
public static class GraphicsCaptureHelper { }

// После: Internal
namespace WindowCaptureCL.Internal.WGC;
internal static class GraphicsCaptureHelper { }
```

#### 6.1.3 Добавление фасадных методов

```csharp
// Новые удобные методы в Capture классе
public static CapturedFrame CaptureWindow(IntPtr hwnd, CaptureOptions? opts = null)
public static Task<CapturedFrame> CaptureWindowAsync(IntPtr hwnd, ...)
public static byte[] CaptureWindowToBytes(IntPtr hwnd, ...)
```

### 6.2 Фаза 2: Упаковка (1 день)

#### 6.2.1 Настройка .csproj для DLL

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
    
    <!-- DLL специфичные настройки -->
    <OutputType>Library</OutputType>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <DocumentationFile>$(OutputPath)$(AssemblyName).xml</DocumentationFile>
    
    <!-- COM interop -->
    <ComVisible>true</ComVisible>
    <EnableComHosting>true</EnableComHosting>
    
    <!-- Версионирование -->
    <Version>1.0.0</Version>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
    <FileVersion>1.0.0.0</FileVersion>
    
    <!-- Оптимизации -->
    <Optimize>true</Optimize>
    <DebugType>portable</DebugType>
  </PropertyGroup>
  
  <!-- Пакеты -->
  <ItemGroup>
    <PackageReference Include="System.Drawing.Common" Version="9.0.10" />
    <PackageReference Include="Vortice.Direct3D11" Version="3.6.2" />
    <PackageReference Include="Vortice.Win32" Version="2.3.0" />
  </ItemGroup>
  
  <!-- Framework reference для WinForms (System.Drawing) -->
  <ItemGroup>
    <FrameworkReference Include="Microsoft.WindowsDesktop.App.WindowsForms" />
  </ItemGroup>
</Project>
```

#### 6.2.2 Конфигурация публикации

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <PublishDir>$(MSBuildThisFileDirectory)publish</PublishDir>
    <PublishProtocol>FileSystem</PublishProtocol>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>false</SelfContained>
    <PublishSingleFile>false</PublishSingleFile>
  </PropertyGroup>
</Project>
```

#### 6.2.3 Скрипты сборки

```powershell
# Build-DLL.ps1
param(
    [string]$Configuration = "Release",
    [string]$OutputDir = ".\dist"
)

Write-Host "Building WindowCaptureCL DLL..." -ForegroundColor Cyan

# 1. Clean
Remove-Item $OutputDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# 2. Restore
dotnet restore

# 3. Build
dotnet build -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

# 4. Publish
dotnet publish -c $Configuration -o "$OutputDir\publish" --no-build

# 5. Merge dependencies (ILRepack)
Write-Host "Merging dependencies..." -ForegroundColor Yellow
.\build\Merge-Dependencies.ps1 `
    -InputPath "$OutputDir\publish" `
    -OutputPath "$OutputDir\WindowCaptureCL.dll"

# 6. Copy external dependencies
Copy-Item "$OutputDir\publish\System.Drawing.Common.dll" $OutputDir
Copy-Item "$OutputDir\publish\WinRT.Runtime.dll" $OutputDir
Copy-Item "$OutputDir\publish\Microsoft.Windows.SDK.NET.dll" $OutputDir
Copy-Item "$OutputDir\publish\WindowCaptureCL.xml" $OutputDir

Write-Host "Build complete! Output: $OutputDir" -ForegroundColor Green
```

### 6.3 Фаза 3: Интеграция (1-2 дня)

#### 6.3.1 PowerShell модуль

```powershell
# WindowCaptureCL.psm1
$ErrorActionPreference = 'Stop'

# Загрузка DLL
$dllPath = Join-Path $PSScriptRoot "WindowCaptureCL.dll"
if (-not (Test-Path $dllPath)) {
    throw "WindowCaptureCL.dll not found at: $dllPath"
}

Add-Type -Path $dllPath

# Экспорт функций
function Get-WindowCapture {
    <#
    .SYNOPSIS
        Захватывает окно или экран.
    #>
    [CmdletBinding()]
    param(
        [Parameter(ParameterSetName = 'Window')]
        [IntPtr]$WindowHandle,
        
        [Parameter(ParameterSetName = 'Screen')]
        [int]$MonitorIndex = 0,
        
        [switch]$IncludeCursor,
        [string]$OutputPath
    )
    
    try {
        if ($PSCmdlet.ParameterSetName -eq 'Window') {
            $frame = [WindowCaptureCL.Capture]::CaptureWindow($WindowHandle)
        } else {
            $frame = [WindowCaptureCL.Capture]::CaptureScreen($MonitorIndex)
        }
        
        if ($OutputPath) {
            $frame.Save($OutputPath)
            Write-Output "Saved to: $OutputPath"
        } else {
            Write-Output $frame.Bitmap
        }
    }
    finally {
        if ($frame) { $frame.Dispose() }
    }
}

Export-ModuleMember -Function Get-WindowCapture
```

#### 6.3.2 Манифест модуля

```powershell
# WindowCaptureCL.psd1
@{
    RootModule = 'WindowCaptureCL.psm1'
    ModuleVersion = '1.0.0'
    GUID = '12345678-1234-1234-1234-123456789012'
    Author = 'WindowCaptureCL Contributors'
    Description = 'PowerShell module for WindowCaptureCL DLL'
    PowerShellVersion = '7.0'
    RequiredAssemblies = @('WindowCaptureCL.dll')
    FunctionsToExport = @('Get-WindowCapture')
    CmdletsToExport = @()
    VariablesToExport = @()
    AliasesToExport = @()
}
```

---

## 7. Технические решения

### 7.1 Обработка зависимостей

#### Vortice.Direct3D11

```csharp
// Проблема: Vortice использует COM interop
// Решение: ILRepack с /union флагом

// Internal wrapper для изоляции
internal sealed class DirectXDevice : IDisposable
{
    private readonly ID3D11Device _device;
    
    public DirectXDevice()
    {
        // Инициализация через Vortice
        _device = D3D11.D3D11CreateDevice(...);
    }
    
    public void Dispose()
    {
        _device?.Dispose();
    }
}
```

#### System.Drawing.Common

```xml
<!-- Не мержить, оставить как внешнюю зависимость -->
<!-- Требует Microsoft.WindowsDesktop.App.WindowsForms -->
<ItemGroup>
  <PackageReference Include="System.Drawing.Common" Version="9.0.10">
    <ExcludeFromSingleFile>true</ExcludeFromSingleFile>
  </PackageReference>
</ItemGroup>
```

#### WinRT Runtime

```xml
<!-- WinRT.Runtime требуется для Windows.Graphics.Capture -->
<!-- Оставить как внешнюю зависимость -->
<PackageReference Include="Microsoft.Windows.SDK.NET.Ref" Version="10.0.19041.0">
  <ExcludeAssets>runtime</ExcludeAssets>
</PackageReference>
```

### 7.2 Управление ресурсами

#### IDisposable паттерны

```csharp
namespace WindowCaptureCL;

/// <summary>
/// Публичный интерфейс с правильным IDisposable.
/// </summary>
public interface ICaptureSession : IDisposable
{
    // Не наследуем от IAsyncDisposable для простоты
}

/// <summary>
/// CapturedFrame с безопасным Dispose.
/// </summary>
public sealed class CapturedFrame : IDisposable
{
    private readonly Bitmap _bitmap;
    private bool _disposed;
    
    public void Dispose()
    {
        if (_disposed) return;
        _bitmap?.Dispose();
        _disposed = true;
    }
    
    // Проверка в каждом методе
    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(CapturedFrame));
    }
}
```

#### Пулы объектов (для continuous capture)

```csharp
internal sealed class FramePool : IDisposable
{
    private readonly ConcurrentBag<Bitmap> _pool = new();
    private readonly int _maxSize;
    
    public Bitmap Rent(int width, int height)
    {
        if (_pool.TryTake(out var bitmap))
        {
            if (bitmap.Width == width && bitmap.Height == height)
                return bitmap;
            bitmap.Dispose();
        }
        return new Bitmap(width, height, PixelFormat.Format32bppArgb);
    }
    
    public void Return(Bitmap bitmap)
    {
        if (_pool.Count < _maxSize)
            _pool.Add(bitmap);
        else
            bitmap.Dispose();
    }
    
    public void Dispose()
    {
        while (_pool.TryTake(out var bitmap))
            bitmap.Dispose();
    }
}
```

#### Обработка ошибок

```csharp
/// <summary>
/// Стандартный обработчик ошибок для DLL.
/// </summary>
public static class CaptureDiagnostics
{
    public static event EventHandler<CaptureErrorEventArgs>? ErrorOccurred;
    public static event EventHandler<DiagnosticEventArgs>? DiagnosticMessage;
    
    internal static void LogError(string message, Exception? ex = null)
    {
        ErrorOccurred?.Invoke(null, new CaptureErrorEventArgs(message, ex));
    }
    
    internal static void LogInfo(string message)
    {
        DiagnosticMessage?.Invoke(null, new DiagnosticEventArgs(message));
    }
}

// Использование в коде
try
{
    // Операция захвата
}
catch (Exception ex)
{
    CaptureDiagnostics.LogError("Capture failed", ex);
    throw; // Пробрасываем дальше
}
```

### 7.3 Производительность

#### Оптимизации для DLL

```csharp
internal sealed class OptimizedCaptureSession : ICaptureSession
{
    // Предварительно выделенные буферы
    private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;
    
    public byte[] CaptureToByteArray()
    {
        // Используем MemoryStream с буфером из пула
        var buffer = _bufferPool.Rent(65536);
        try
        {
            using var ms = new MemoryStream(buffer, 0, buffer.Length, true, true);
            // Запись в stream...
            return ms.ToArray();
        }
        finally
        {
            _bufferPool.Return(buffer);
        }
    }
}
```

#### Минимизация allocations

```csharp
// Плохо: Создает много мусора
public Bitmap CaptureFrame()
{
    var frame = new Bitmap(width, height);
    // ...
    return frame;
}

// Хорошо: Переиспользование
public void CaptureFrame(Span<byte> buffer)
{
    // Запись прямо в предоставленный буфер
}
```

#### Async/await паттерны

```csharp
public async Task<CapturedFrame> CaptureFrameAsync(CancellationToken ct)
{
    // Используем TaskCompletionSource для WGC событий
    var tcs = new TaskCompletionSource<ID3D11Texture2D>();
    
    using (ct.Register(() => tcs.TrySetCanceled()))
    {
        _framePool.FrameArrived += (s, e) =>
        {
            var frame = e.Frame;
            tcs.TrySetResult(frame.Surface);
        };
        
        var texture = await tcs.Task;
        return ConvertToBitmap(texture);
    }
}
```

---

## 8. Примеры использования

### 8.1 Из PowerShell 7

```powershell
# ========== Загрузка DLL ==========
# Способ 1: Add-Type
Add-Type -Path "C:\Tools\WindowCaptureCL.dll"

# Способ 2: using assembly (требует PowerShell 7+)
using assembly "C:\Tools\WindowCaptureCL.dll"
using namespace WindowCaptureCL

# ========== Базовый захват окна ==========
# Найти окно по заголовку
$hwnd = (Get-Process -Name "notepad").MainWindowHandle

# Захватить окно
$frame = [Capture]::CaptureWindow($hwnd)
$frame.Save("C:\Screenshots\notepad.png")
$frame.Dispose()

# ========== Захват с опциями ==========
$options = [CaptureOptions]::new()
$options.IncludeCursor = $true
$options.DrawBorder = $false

$frame = [Capture]::CaptureWindow($hwnd, $options)
$frame.Save("C:\Screenshots\notepad_with_cursor.png")
$frame.Dispose()

# ========== Fluent API ==========
$options = [CaptureOptions]::new().
    WithCursor().
    WithFPS(60).
    WithTimeout(10000)

$frame = [Capture]::CaptureWindow($hwnd, $options)

# ========== Захват экрана ==========
# Основной монитор
$frame = [Capture]::CaptureScreen(0)
$frame.Save("C:\Screenshots\screen.png")
$frame.Dispose()

# ========== Асинхронный захват ==========
# В PowerShell 7+
$cts = [System.Threading.CancellationTokenSource]::new(5000)
$frame = [Capture]::CaptureScreenAsync(0, $null, $cts.Token).Result
$frame.Save("C:\Screenshots\screen_async.png")
$frame.Dispose()

# ========== Непрерывный захват ==========
$session = [Capture]::FromWindow($hwnd)

# Подписка на события
Register-ObjectEvent -InputObject $session -EventName "FrameReady" -Action {
    $frame = $EventArgs.Frame
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss_fff"
    $frame.Save("C:\Screenshots\frame_$timestamp.png")
    $frame.Dispose()
}

$session.StartCapture()
Start-Sleep -Seconds 10
$session.StopCapture()
$session.Dispose()

# ========== Проверка поддержки ==========
if (-not [Capture]::IsSupported) {
    Write-Error "Windows Graphics Capture не поддерживается"
    exit 1
}

# ========== Обработка ошибок ==========
try {
    $frame = [Capture]::CaptureWindow([IntPtr]::Zero)
} catch [WindowCaptureCL.CaptureSourceNotFoundException] {
    Write-Error "Окно не найдено"
} catch [WindowCaptureCL.GraphicsCaptureNotSupportedException] {
    Write-Error "WGC не поддерживается"
} catch {
    Write-Error "Ошибка захвата: $_"
}
```

### 8.2 Из C# проекта

```csharp
// ========== Прямая ссылка на DLL ==========
// 1. Добавить ссылку на WindowCaptureCL.dll
// 2. Добавить using

using WindowCaptureCL;
using System.Drawing;

// ========== Базовый сценарий ==========
public class ScreenshotService
{
    public void CaptureWindow(IntPtr hwnd, string outputPath)
    {
        // Простой one-liner
        using var frame = Capture.CaptureWindow(hwnd);
        frame.Save(outputPath);
    }
    
    public void CaptureScreen(string outputPath, int monitorIndex = 0)
    {
        using var frame = Capture.CaptureScreen(monitorIndex);
        frame.Save(outputPath);
    }
}

// ========== С продвинутой конфигурацией ==========
public class AdvancedCaptureService
{
    public async Task CaptureWithOptionsAsync(
        IntPtr hwnd, 
        string outputPath,
        CancellationToken ct)
    {
        var options = new CaptureOptions()
            .WithCursor()
            .WithFPS(60)
            .WithTimeout(10000);
        
        using var frame = await Capture.CaptureWindowAsync(hwnd, options, ct);
        frame.Save(outputPath, ImageFormat.Png);
    }
}

// ========== Непрерывный захват ==========
public class ContinuousCaptureService : IDisposable
{
    private ICaptureSession? _session;
    
    public void StartCapture(IntPtr hwnd, Action<CapturedFrame> onFrame)
    {
        _session = Capture.FromWindow(hwnd);
        _session.FrameReady += (s, e) => onFrame(e.Frame);
        _session.StartCapture();
    }
    
    public void StopCapture()
    {
        _session?.StopCapture();
    }
    
    public void Dispose()
    {
        _session?.Dispose();
    }
}

// ========== Интеграция с DI ==========
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWindowCapture(this IServiceCollection services)
    {
        services.AddSingleton<ICaptureService, CaptureService>();
        return services;
    }
}

// ========== Обработка ошибок ==========
public class RobustCaptureService
{
    public bool TryCaptureWindow(IntPtr hwnd, string outputPath, out string? error)
    {
        error = null;
        
        try
        {
            if (!Capture.IsSupported)
            {
                error = "Windows Graphics Capture not supported";
                return false;
            }
            
            using var frame = Capture.CaptureWindow(hwnd);
            frame.Save(outputPath);
            return true;
        }
        catch (CaptureSourceNotFoundException ex)
        {
            error = $"Window not found: {ex.Message}";
            return false;
        }
        catch (CaptureException ex)
        {
            error = $"Capture failed: {ex.Message}";
            return false;
        }
    }
}
```

### 8.3 Интеграция с другими библиотеками серии

#### Интеграция с SendSequence

```csharp
// SendSequence использует WindowCaptureCL для скриншотов перед действиями

using SendSequence;
using WindowCaptureCL;

public class AutomatedWorkflow
{
    private readonly SequenceBuilder _sequence;
    private readonly ICaptureService _capture;
    
    public async Task ExecuteWithVerification(IntPtr targetWindow)
    {
        // 1. Захватить состояние до действия
        using var beforeFrame = Capture.CaptureWindow(targetWindow);
        
        // 2. Выполнить действие через SendSequence
        var sequence = new SequenceBuilder()
            .Click(100, 100)
            .TypeText("Hello")
            .Build();
        
        await sequence.ExecuteAsync(targetWindow);
        
        // 3. Захватить состояние после
        using var afterFrame = Capture.CaptureWindow(targetWindow);
        
        // 4. Сравнить (через ImageSearch)
        var diff = ImageSearch.Compare(beforeFrame.Bitmap, afterFrame.Bitmap);
    }
}
```

#### Интеграция с WindowManager

```csharp
// WindowManager предоставляет hwnd, WindowCaptureCL использует его

using WindowManager;
using WindowCaptureCL;

public class WindowMonitor
{
    private readonly WindowManager _manager;
    
    public void CaptureAllWindows(string processName)
    {
        var windows = _manager.FindWindows(processName);
        
        foreach (var window in windows)
        {
            try
            {
                using var frame = Capture.CaptureWindow(window.Handle);
                frame.Save($"screenshots\\{processName}_{window.Id}.png");
            }
            catch (CaptureException ex)
            {
                Logger.LogWarning($"Failed to capture {window.Title}: {ex.Message}");
            }
        }
    }
}
```

#### Интеграция с ImageSearch

```csharp
// ImageSearch использует WindowCaptureCL для получения изображений для анализа

using ImageSearch;
using WindowCaptureCL;

public class VisualAutomation
{
    public bool FindAndClick(IntPtr windowHandle, string templatePath)
    {
        // 1. Захватить окно
        using var frame = Capture.CaptureWindow(windowHandle);
        
        // 2. Найти шаблон
        var result = ImageSearch.FindTemplate(frame.Bitmap, templatePath);
        
        if (result.Found)
        {
            // 3. Кликнуть по найденной позиции
            InputSimulator.Click(result.Location);
            return true;
        }
        
        return false;
    }
    
    public async Task WaitForImageAsync(
        IntPtr windowHandle, 
        string templatePath,
        TimeSpan timeout)
    {
        var cts = new CancellationTokenSource(timeout);
        
        while (!cts.Token.IsCancellationRequested)
        {
            using var frame = await Capture.CaptureWindowAsync(windowHandle, cancellationToken: cts.Token);
            
            if (ImageSearch.ContainsTemplate(frame.Bitmap, templatePath))
                return;
            
            await Task.Delay(100, cts.Token);
        }
        
        throw new TimeoutException("Image not found within timeout");
    }
}
```

---

## 9. Риски и ограничения

### 9.1 Windows Graphics Capture требования

| Требование | Описание | Митигация |
|------------|----------|-----------|
| **Windows 10 1803+** | Минимальная версия ОС | Проверка `Capture.IsSupported` |
| **Видимое окно** | Нельзя захватить свернутое окно | Проверка перед захватом |
| **Пользовательская сессия** | Не работает в RDP/сервисах | Документировать ограничения |
| **Графический адаптер** | Требуется DirectX 11 | Fallback на GDI (в будущем) |

### 9.2 DirectX зависимости

```
┌─────────────────────────────────────────────────────────────┐
│                    DirectX Зависимости                       │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  WindowCaptureCL.dll                                        │
│       │                                                      │
│       ▼                                                      │
│  ┌─────────────────┐    ┌─────────────────┐                │
│  │ Vortice.D3D11   │───▶│ d3d11.dll       │ [системная]     │
│  └─────────────────┘    └─────────────────┘                │
│       │                                                      │
│       ▼                                                      │
│  ┌─────────────────┐    ┌─────────────────┐                │
│  │ Vortice.DXGI    │───▶│ dxgi.dll        │ [системная]     │
│  └─────────────────┘    └─────────────────┘                │
│                                                              │
│  Риск: На системах без DirectX 11 библиотека не работает    │
│  Решение: Проверка Feature Level при инициализации          │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### 9.3 Размер DLL

| Компонент | Размер | Примечание |
|-----------|--------|------------|
| WindowCaptureCL (код) | ~50 KB | Собственный код |
| Vortice.Direct3D11 | ~500 KB | После ILMerge |
| System.Drawing.Common | ~200 KB | Внешняя зависимость |
| WinRT.Runtime | ~300 KB | Внешняя зависимость |
| **Итого** | **~1.1 MB** | Разумный размер |

**Рекомендации по уменьшению размера:**
- Использовать ILRepack с `/wildcards` и `/parallel`
- Включить тримминг неиспользуемых типов
- Рассмотреть NativeAOT для .NET 8+ (дает single-file, но сложнее)

---

## 10. Рекомендации по реализации

### 10.1 Порядок действий

```
Фаза 1: Подготовка (День 1)
├── 1.1 Создать ветку feature/dll-refactoring
├── 1.2 Настроить ILRepack в build скриптах
├── 1.3 Создать тестовый PowerShell скрипт
└── 1.4 Проверить текущие тесты

Фаза 2: Рефакторинг API (День 1-2)
├── 2.1 Создать CaptureOptions класс
├── 2.2 Добавить фасадные методы в Capture
├── 2.3 Сделать Infrastructure типы internal
├── 2.4 Добавить CaptureDiagnostics
└── 2.5 Обновить unit тесты

Фаза 3: Упаковка (День 2)
├── 3.1 Настроить .csproj для DLL
├── 3.2 Создать Build-DLL.ps1
├── 3.3 Настроить ILRepack
├── 3.4 Создать структуру dist/
└── 3.5 Проверить output

Фаза 4: PowerShell модуль (День 3)
├── 4.1 Создать WindowCaptureCL.psm1
├── 4.2 Создать WindowCaptureCL.psd1
├── 4.3 Написать примеры использования
└── 4.4 Протестировать в PowerShell 7

Фаза 5: Документация (День 3)
├── 5.1 Обновить README.md
├── 5.2 Создать QUICKSTART.md
├── 5.3 Добавить примеры интеграции
└── 5.4 Проверить XML документацию
```

### 10.2 Что делать в первую очередь

1. **Настроить build pipeline** — без автоматической сборки сложно тестировать
2. **Создать CaptureOptions** — упрощает API для PowerShell
3. **Сделать типы internal** — защищает от breaking changes
4. **Добавить фасадные методы** — делает API доступным для скриптов

### 10.3 Что можно отложить

| Функция | Приоритет | Примечание |
|---------|-----------|------------|
| COM interop | Низкий | Используется редко |
| NativeAOT | Низкий | Сложная интеграция с WinRT |
| GDI fallback | Низкий | Только для старых систем |
| PowerShell 5.1 | Низкий | PowerShell 7 предпочтителен |
| x86 поддержка | Низкий | x64 достаточно |

---

## Приложение A: Конфигурационные файлы

### A.1 Полный .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    
    <!-- Assembly Info -->
    <AssemblyName>WindowCaptureCL</AssemblyName>
    <RootNamespace>WindowCaptureCL</RootNamespace>
    <Version>1.0.0</Version>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
    <FileVersion>1.0.0.0</FileVersion>
    <Company>WindowCaptureCL</Company>
    <Product>WindowCaptureCL</Product>
    <Description>High-performance screen capture library</Description>
    <Copyright>Copyright © 2025</Copyright>
    
    <!-- DLL Configuration -->
    <OutputType>Library</OutputType>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <DocumentationFile>$(OutputPath)$(AssemblyName).xml</DocumentationFile>
    
    <!-- COM Interop -->
    <ComVisible>true</ComVisible>
    <EnableComHosting>true</EnableComHosting>
    
    <!-- Build Configuration -->
    <Optimize>true</Optimize>
    <DebugType>portable</DebugType>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="System.Drawing.Common" Version="9.0.10" />
    <PackageReference Include="Vortice.Direct3D11" Version="3.6.2" />
    <PackageReference Include="Vortice.Win32" Version="2.3.0" />
  </ItemGroup>
  
  <ItemGroup>
    <FrameworkReference Include="Microsoft.WindowsDesktop.App.WindowsForms" />
  </ItemGroup>
  
  <!-- Assembly Attributes -->
  <ItemGroup>
    <AssemblyAttribute Include="System.Runtime.InteropServices.ComVisible">
      <_Parameter1>true</_Parameter1>
    </AssemblyAttribute>
  </ItemGroup>
</Project>
```

### A.2 Directory.Build.props

```xml
<Project>
  <PropertyGroup>
    <!-- Common properties for all configurations -->
    <LangVersion>latest</LangVersion>
    <Deterministic>true</Deterministic>
    <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
    
    <!-- NuGet -->
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
    
    <!-- Code Analysis -->
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All"/>
  </ItemGroup>
</Project>
```

### A.3 global.json

```json
{
  "sdk": {
    "version": "8.0.100",
    "rollForward": "latestFeature"
  }
}
```

---

## Приложение B: Чеклист перед релизом

- [ ] Все типы `Infrastructure` сделаны `internal`
- [ ] `CaptureOptions` реализован с fluent API
- [ ] Фасадные методы добавлены в `Capture`
- [ ] ILRepack успешно мержит Vortice зависимости
- [ ] PowerShell модуль загружается без ошибок
- [ ] Примеры из документации работают
- [ ] XML документация генерируется
- [ ] Версия обновлена в AssemblyInfo
- [ ] CHANGELOG.md обновлен
- [ ] README.md содержит инструкции по установке DLL
