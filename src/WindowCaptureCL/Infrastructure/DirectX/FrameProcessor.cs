using System.Drawing;
using System.Drawing.Imaging;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace WindowCaptureCL.Infrastructure.DirectX;

/// <summary>
/// Processes captured DirectX frames and converts them to Bitmap format.
/// </summary>
internal static class FrameProcessor
{
    /// <summary>
    /// Converts a Direct3D11 texture to a Bitmap.
    /// </summary>
    /// <param name="device">The D3D11 device.</param>
    /// <param name="deviceContext">The D3D11 device context.</param>
    /// <param name="texture">The source texture to convert.</param>
    /// <returns>A Bitmap containing the texture data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="FrameCaptureException">Thrown when conversion fails.</exception>
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

        try
        {
            // Get texture description
            var desc = texture.Description;

            // Create staging texture for CPU readback
            stagingTexture = DirectXDeviceManager.Instance.CreateStagingTexture((int)desc.Width, (int)desc.Height);

            // Copy texture to staging
            deviceContext.CopyResource(stagingTexture, texture);

            // Map the staging texture for CPU read
            var mappedResource = deviceContext.Map(stagingTexture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);

            try
            {
                // Create bitmap and copy data
                var bitmap = new Bitmap((int)desc.Width, (int)desc.Height, PixelFormat.Format32bppArgb);

                var bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, (int)desc.Width, (int)desc.Height),
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

                        // Copy row by row to handle potential pitch differences
                        for (int y = 0; y < desc.Height; y++)
                        {
                            var sourceRow = sourcePtr + (y * sourceRowPitch);
                            var destRow = destPtr + (y * destRowPitch);

                            // BGRA format - copy directly
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
            stagingTexture?.Dispose();
        }
    }

    /// <summary>
    /// Converts a region of a Direct3D11 texture to a Bitmap.
    /// </summary>
    /// <param name="device">The D3D11 device.</param>
    /// <param name="deviceContext">The D3D11 device context.</param>
    /// <param name="texture">The source texture to convert.</param>
    /// <param name="region">The region to crop from the texture.</param>
    /// <returns>A Bitmap containing the cropped texture data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="FrameCaptureException">Thrown when conversion fails.</exception>
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

        try
        {
            // Get texture description
            var desc = texture.Description;

            // Create staging texture for CPU readback
            stagingTexture = DirectXDeviceManager.Instance.CreateStagingTexture((int)desc.Width, (int)desc.Height);

            // Copy texture to staging
            deviceContext.CopyResource(stagingTexture, texture);

            // Map the staging texture for CPU read
            var mappedResource = deviceContext.Map(stagingTexture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);

            try
            {
                // Create bitmap with region size
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
                        var bytesPerPixel = 4; // BGRA format

                        // Copy region row by row
                        for (int y = 0; y < region.Height; y++)
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
            stagingTexture?.Dispose();
        }
    }

    /// <summary>
    /// Converts a Direct3D11 texture to a Bitmap asynchronously.
    /// </summary>
    /// <param name="device">The D3D11 device.</param>
    /// <param name="deviceContext">The D3D11 device context.</param>
    /// <param name="texture">The source texture to convert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation, containing the converted Bitmap.</returns>
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
