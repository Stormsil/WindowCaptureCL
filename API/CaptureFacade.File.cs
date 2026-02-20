using System.Drawing;

namespace WindowCaptureCL;

public static partial class CaptureFacade
{
    public static void CaptureWindowToFile(IntPtr windowHandle, string filePath, string format = "png", bool includeCursor = false)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        using var bitmap = CaptureWindow(windowHandle, includeCursor);
        SaveBitmapToFile(bitmap, filePath, format);
    }

    public static void CaptureScreenToFile(string filePath, string format = "png", bool includeCursor = false)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        using var bitmap = CaptureScreen(includeCursor);
        SaveBitmapToFile(bitmap, filePath, format);
    }

    public static void CaptureRegionToFile(int x, int y, int width, int height, string filePath, string format = "png", bool includeCursor = false)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        using var bitmap = CaptureRegion(x, y, width, height, includeCursor);
        SaveBitmapToFile(bitmap, filePath, format);
    }

    private static void SaveBitmapToFile(Bitmap bitmap, string filePath, string format)
    {
        var imageFormat = format.ToLowerInvariant() switch
        {
            "jpg" or "jpeg" => System.Drawing.Imaging.ImageFormat.Jpeg,
            "bmp" => System.Drawing.Imaging.ImageFormat.Bmp,
            "gif" => System.Drawing.Imaging.ImageFormat.Gif,
            _ => System.Drawing.Imaging.ImageFormat.Png,
        };

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        bitmap.Save(filePath, imageFormat);
    }
}
