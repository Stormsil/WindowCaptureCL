using Vortice.Direct3D11;
using Vortice.DXGI;

namespace WindowCaptureCL.Infrastructure.DirectX;

internal sealed partial class DirectXDeviceManager
{
    public ID3D11Texture2D CreateStagingTexture(int width, int height)
    {
        lock (_lock)
        {
            EnsureNotDisposed();

            var desc = new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CPUAccessFlags = CpuAccessFlags.Read,
                MiscFlags = ResourceOptionFlags.None
            };

            try
            {
                var texture = Device.CreateTexture2D(desc);
                if (texture == null)
                {
                    throw new ResourceAllocationException(
                        $"Failed to create staging texture ({width}x{height}).",
                        "StagingTexture");
                }

                return texture;
            }
            catch (Exception ex) when (ex is not ResourceAllocationException)
            {
                throw new ResourceAllocationException(
                    $"Failed to create staging texture ({width}x{height}).",
                    "StagingTexture",
                    ex);
            }
        }
    }
}
