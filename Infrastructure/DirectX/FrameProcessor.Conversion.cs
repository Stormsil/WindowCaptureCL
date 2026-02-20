using System.Drawing;
using System.Drawing.Imaging;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace WindowCaptureCL.Infrastructure.DirectX;

internal static partial class FrameProcessor
{
    public static Bitmap ConvertTextureToBitmap(
        ID3D11Device device,
        ID3D11DeviceContext deviceContext,
        ID3D11Texture2D texture)
    {
        if (device == null)
            throw new ArgumentNullException(nameof(device));
        if (deviceContext == null)
            throw new ArgumentNullException(nameof(deviceContext));
        if (texture == null)
            throw new ArgumentNullException(nameof(texture));

        ID3D11Texture2D? stagingTexture = null;
        var desc = texture.Description;
        var width = (int)desc.Width;
        var height = (int)desc.Height;

        try
        {
            stagingTexture = GetStagingTexture(width, height);
            deviceContext.CopyResource(stagingTexture, texture);

            var mappedResource = deviceContext.Map(stagingTexture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);

            try
            {
                var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

                var bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppArgb);

                try
                {
                    unsafe
                    {
                        var sourcePtr = (byte*)mappedResource.DataPointer;
                        var destPtr = (byte*)bitmapData.Scan0;

                        var sourceRowPitch = mappedResource.RowPitch;
                        var destRowPitch = bitmapData.Stride;

                        for (var y = 0; y < height; y++)
                        {
                            var sourceRow = sourcePtr + (y * sourceRowPitch);
                            var destRow = destPtr + (y * destRowPitch);

                            Buffer.MemoryCopy(
                                sourceRow,
                                destRow,
                                destRowPitch,
                                Math.Min(sourceRowPitch, destRowPitch));
                        }
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }

                return bitmap;
            }
            finally
            {
                deviceContext.Unmap(stagingTexture, 0);
            }
        }
        catch (Exception ex) when (ex is not FrameCaptureException)
        {
            throw new FrameCaptureException("Failed to convert DirectX texture to Bitmap.", ex);
        }
        finally
        {
            if (stagingTexture != null)
            {
                ReturnStagingTexture(width, height, stagingTexture);
            }
        }
    }

    public static Bitmap ConvertTextureToBitmap(
        ID3D11Device device,
        ID3D11DeviceContext deviceContext,
        ID3D11Texture2D texture,
        Rectangle region)
    {
        if (device == null)
            throw new ArgumentNullException(nameof(device));
        if (deviceContext == null)
            throw new ArgumentNullException(nameof(deviceContext));
        if (texture == null)
            throw new ArgumentNullException(nameof(texture));

        ID3D11Texture2D? stagingTexture = null;
        var desc = texture.Description;
        var sourceWidth = (int)desc.Width;
        var sourceHeight = (int)desc.Height;

        try
        {
            stagingTexture = GetStagingTexture(sourceWidth, sourceHeight);
            deviceContext.CopyResource(stagingTexture, texture);

            var mappedResource = deviceContext.Map(stagingTexture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);

            try
            {
                var bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
                var bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, region.Width, region.Height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppArgb);

                try
                {
                    unsafe
                    {
                        var sourcePtr = (byte*)mappedResource.DataPointer;
                        var destPtr = (byte*)bitmapData.Scan0;

                        var sourceRowPitch = mappedResource.RowPitch;
                        var destRowPitch = bitmapData.Stride;
                        const int bytesPerPixel = 4;

                        for (var y = 0; y < region.Height; y++)
                        {
                            var sourceRow = sourcePtr + ((region.Y + y) * sourceRowPitch) + (region.X * bytesPerPixel);
                            var destRow = destPtr + (y * destRowPitch);

                            Buffer.MemoryCopy(
                                sourceRow,
                                destRow,
                                destRowPitch,
                                region.Width * bytesPerPixel);
                        }
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }

                return bitmap;
            }
            finally
            {
                deviceContext.Unmap(stagingTexture, 0);
            }
        }
        catch (Exception ex) when (ex is not FrameCaptureException)
        {
            throw new FrameCaptureException("Failed to convert DirectX texture region to Bitmap.", ex);
        }
        finally
        {
            if (stagingTexture != null)
            {
                ReturnStagingTexture(sourceWidth, sourceHeight, stagingTexture);
            }
        }
    }
}
