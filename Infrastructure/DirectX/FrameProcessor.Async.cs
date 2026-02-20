using System.Drawing;
using Vortice.Direct3D11;

namespace WindowCaptureCL.Infrastructure.DirectX;

internal static partial class FrameProcessor
{
    public static Task<Bitmap> ConvertTextureToBitmapAsync(
        ID3D11Device device,
        ID3D11DeviceContext deviceContext,
        ID3D11Texture2D texture,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ConvertTextureToBitmap(device, deviceContext, texture);
        }, cancellationToken);
    }
}
