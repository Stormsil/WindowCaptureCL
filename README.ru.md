# WindowCaptureCL - Библиотека Захвата Экрана

Высокопроизводительная .NET библиотека для захвата скриншотов окон, мониторов и областей экрана с использованием Windows Graphics Capture API.

## 🚀 Быстрый Старт

```csharp
using WindowCaptureCL;

// Захват одного кадра из окна
var process = Process.GetProcessesByName("notepad").First();
using var session = Capture.FromWindow(process.MainWindowHandle);
using var frame = session.CaptureFrame();
frame.Bitmap.Save("screenshot.png");
```

## ✨ Возможности

- **Захват окон** - даже если окно перекрыто другими
- **Захват мониторов** - захват всего экрана
- **Захват областей** - произвольная область на экране
- **Непрерывный захват** - с контролем FPS (1-120 кадров/сек)
- **Аппаратное ускорение** - DirectX 11 на стороне GPU
- **Потокобезопасность** - все API потокобезопасны
- **Захват курсора** - опциональный захват курсора мыши

## 📋 Требования

- Windows 10 версии 1903 (build 18362) или новее
- .NET 8.0
- DirectX 11 совместимая видеокарта
- **Физический доступ к GPU** (не работает в удалённом рабочем столе)

## ⚠ ВАЖНО: Ограничение Удалённого Рабочего Стола

**Библиотека НЕ РАБОТАЕТ в следующих средах:**

- Remote Desktop (RDP)
- VNC
- NoMachine
- Любое другое ПО удалённого доступа
- Виртуальные машины без GPU passthrough

Это ограничение Windows Graphics Capture API, которому требуется прямой доступ к GPU.

**Ошибка которую вы увидите:**
```
GraphicsDeviceException: Failed to create Direct3D11 capture frame pool.
```

**Решение:** Запускайте библиотеку на физической машине с прямым доступом к GPU.

## 📦 Установка

```bash
dotnet add package WindowCaptureCL
```

Или вручную в .csproj:
```xml
<PackageReference Include="WindowCaptureCL" Version="1.0.0" />
```

## 📚 Примеры Использования

### Захват Окна

```csharp
var process = Process.GetProcessesByName("notepad").First();
using var session = Capture.FromWindow(process.MainWindowHandle);

// Одиночный кадр
using var frame = session.CaptureFrame();
frame.Bitmap.Save("window.png");
```

### Непрерывный Захват

```csharp
using var session = Capture.FromWindow(windowHandle);

// Настройка FPS
var config = new CaptureConfiguration { MaxFramesPerSecond = 60 };
session.UpdateConfiguration(config);

// Подписка на события
session.FrameReady += (sender, args) =>
{
    Console.WriteLine($"Кадр #{args.FrameNumber} получен");
    args.Frame.Save($"frame_{args.FrameNumber}.png");
    args.Frame.Dispose(); // Важно освобождать ресурсы!
};

session.CaptureError += (sender, args) =>
{
    Console.WriteLine($"Ошибка: {args.Exception.Message}");
};

session.StartCapture();
Thread.Sleep(5000); // Захват 5 секунд
session.StopCapture();
```

### Захват Монитора

```csharp
// Захват первого монитора (индекс 0)
using var session = Capture.FromScreen(monitorIndex: 0);
using var frame = session.CaptureFrame();
frame.Bitmap.Save("monitor.png");
```

### Захват Области

```csharp
// Захват области 800x600 начиная с координат (100, 100)
var region = new Rectangle(100, 100, 800, 600);
using var session = Capture.FromScreenRegion(monitorIndex: 0, region);
using var frame = session.CaptureFrame();
frame.Bitmap.Save("region.png");
```

### Глобальная Конфигурация

```csharp
// Установить глобальные настройки для всех новых сессий
CaptureConfiguration.DefaultTargetFPS = 60;
CaptureConfiguration.IncludeCursor = true;

// Создать сессию - будет использовать глобальные настройки
using var session = Capture.FromWindow(windowHandle);
```

## 🧪 Тестирование

Библиотека включает консольное приложение для тестирования:

```bash
cd samples/TestApp
dotnet run
```

**Меню TestApp:**
1. Тест захвата одиночного кадра окна
2. Тест непрерывного захвата (5 секунд)
3. Тест захвата монитора
4. Тест захвата области
5. Тест глобальной конфигурации
6. Список доступных окон

### Как Тестировать в NoMachine/RDP

**Вы не можете** - библиотека требует прямой доступ к GPU.

**Что можно сделать:**
- Проверить сборку: `dotnet build --configuration Release`
- Запустить на физической машине локально
- Настроить VM с GPU passthrough

Подробнее см. [TESTING.md](TESTING.md)

## 🏗 Архитектура

```
WindowCaptureCL
├── API/                    # Публичный API
│   ├── Capture.cs         # Точка входа (FromWindow, FromScreen, FromScreenRegion)
│   ├── ICaptureSession.cs # Интерфейс сессии захвата
│   └── CaptureConfiguration.cs # Конфигурация и глобальные настройки
├── Core/                   # Реализация
│   └── CaptureSession.cs  # Основная логика захвата
└── Infrastructure/         # Низкоуровневые компоненты
    ├── DirectX/           # DirectX 11 интеграция
    └── WGC/               # Windows Graphics Capture интеграция
```

## 🎯 API Справка

### Capture (статический класс)

- `FromWindow(IntPtr windowHandle)` - Создать сессию захвата окна
- `FromScreen(int monitorIndex)` - Создать сессию захвата монитора
- `FromScreenRegion(int monitorIndex, Rectangle region)` - Создать сессию захвата области

### ICaptureSession

**Свойства:**
- `SourceInfo` - Информация об источнике захвата
- `IsActive` - Активна ли непрерывная съёмка
- `TotalFramesCaptured` - Общее количество захваченных кадров

**Методы:**
- `CaptureFrame()` - Захватить один кадр (синхронно)
- `CaptureFrameAsync()` - Захватить один кадр (асинхронно)
- `StartCapture()` - Начать непрерывный захват
- `StopCapture()` - Остановить непрерывный захват
- `UpdateConfiguration(config)` - Обновить конфигурацию

**События:**
- `FrameReady` - Новый кадр готов (непрерывный захват)
- `CaptureError` - Произошла ошибка
- `CaptureStopped` - Захват остановлен

### CaptureConfiguration

**Статические свойства (глобальные):**
- `DefaultTargetFPS` - FPS по умолчанию (1-120, по умолчанию 30)
- `IncludeCursor` - Захватывать курсор мыши (по умолчанию false)
- `DrawBorder` - Рисовать рамку (не поддерживается API)
- `MinimumFPS` - Минимальный FPS = 1
- `MaximumFPS` - Максимальный FPS = 120

**Свойства экземпляра:**
- `MaxFramesPerSecond` - FPS для конкретной сессии

## ⚡ Производительность

- **Аппаратное ускорение**: Вся обработка на GPU (DirectX 11)
- **Низкая задержка**: ~15-30мс на кадр
- **Эффективная память**: Переиспользование буферов Direct3D
- **FPS контроль**: Настраиваемый от 1 до 120 FPS
- **Захват скрытых окон**: Работает даже если окно перекрыто

## 🛡 Обработка Исключений

Все исключения наследуются от `CaptureException`:

- `WindowNotFoundException` - Окно не найдено
- `InvalidMonitorException` - Некорректный индекс монитора
- `RegionOutOfBoundsException` - Область выходит за границы экрана
- `GraphicsDeviceException` - Ошибка DirectX устройства
- `SessionAlreadyStartedException` - Сессия уже запущена
- `SessionNotStartedException` - Сессия не запущена
- `UnsupportedOperationException` - Операция не поддерживается

```csharp
try
{
    using var session = Capture.FromWindow(windowHandle);
    using var frame = session.CaptureFrame();
    frame.Bitmap.Save("screenshot.png");
}
catch (WindowNotFoundException ex)
{
    Console.WriteLine($"Окно не найдено: {ex.Message}");
}
catch (GraphicsDeviceException ex)
{
    Console.WriteLine($"Ошибка GPU: {ex.Message}");
    Console.WriteLine("Возможно вы запущены в удалённом рабочем столе?");
}
catch (CaptureException ex)
{
    Console.WriteLine($"Ошибка захвата: {ex.Message}");
}
```

## 🔒 Потокобезопасность

Все публичные API потокобезопасны:
- Можно вызывать `CaptureFrame()` из разных потоков
- Запуск/остановка из разных потоков безопасна
- Обновление конфигурации конкурентно безопасно

## 📝 Лицензия

MIT License - см. [LICENSE](LICENSE)

## 🤝 Вклад

Приветствуются issue и pull request!

## 📞 Поддержка

При возникновении проблем:

1. Проверьте версию Windows: `winver` (должна быть 1903+)
2. Проверьте DirectX: `dxdiag` (должен быть DirectX 11+)
3. Убедитесь что НЕ работаете через удалённый рабочий стол
4. Прочитайте [TESTING.md](TESTING.md) для деталей

## 🌟 Особенности

- **Захват скрытых окон**: Windows Graphics Capture работает даже если окно закрыто другими
- **Нет GDI**: Использует современный GPU-ускоренный путь
- **Совместимость**: Windows 10 1903+ и Windows 11
- **NuGet Ready**: Готовая к использованию библиотека

---

**Примечание**: Эта библиотека захватывает содержимое экрана. Убедитесь что соблюдаете применимые законы и правила относительно записи экрана и приватности пользователей.
