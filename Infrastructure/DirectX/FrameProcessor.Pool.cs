using Vortice.Direct3D11;

namespace WindowCaptureCL.Infrastructure.DirectX;

internal static partial class FrameProcessor
{
    private static ID3D11Texture2D CreateStagingTexture(int width, int height)
    {
        return DirectXDeviceManager.Instance.CreateStagingTexture(width, height);
    }

    private static ID3D11Texture2D GetStagingTexture(int width, int height)
    {
        lock (_poolLock)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(FrameProcessor), "FrameProcessor has been disposed.");

            return _stagingTexturePool.Get((width, height));
        }
    }

    private static void ReturnStagingTexture(int width, int height, ID3D11Texture2D texture)
    {
        lock (_poolLock)
        {
            if (_isDisposed)
            {
                texture?.Dispose();
                return;
            }

            _stagingTexturePool.Return((width, height), texture);
        }
    }

    public static void Dispose()
    {
        lock (_poolLock)
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _stagingTexturePool.Clear();
        }
    }
}
