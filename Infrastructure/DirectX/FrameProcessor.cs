using Vortice.Direct3D11;
using WindowCaptureCL.Infrastructure;

namespace WindowCaptureCL.Infrastructure.DirectX;

internal static partial class FrameProcessor
{
    private static readonly KeyedObjectPool<(int, int), ID3D11Texture2D> _stagingTexturePool;
    private static readonly object _poolLock = new();
    private static bool _isDisposed;

    static FrameProcessor()
    {
        _stagingTexturePool = new KeyedObjectPool<(int, int), ID3D11Texture2D>(
            factory: key => CreateStagingTexture(key.Item1, key.Item2),
            resetAction: null,
            disposeAction: texture => texture?.Dispose(),
            maxSizePerKey: 3
        );
    }
}
