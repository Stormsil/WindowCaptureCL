namespace WindowCaptureCL.Tests.Unit;

public class ExceptionTests
{
    [Fact]
    public void CaptureException_IsAbstract()
    {
        // Verify CaptureException is abstract and cannot be instantiated
        var type = typeof(CaptureException);
        Assert.True(type.IsAbstract);
    }

    [Fact]
    public void GraphicsCaptureNotSupportedException_HasDefaultMessage()
    {
        var ex = new GraphicsCaptureNotSupportedException();
        Assert.NotNull(ex.Message);
        Assert.Contains("Windows Graphics Capture", ex.Message);
    }

    [Fact]
    public void CaptureSourceNotFoundException_StoresSourceId()
    {
        var sourceId = 12345;
        var ex = new CaptureSourceNotFoundException("Test", sourceId);
        Assert.Equal(sourceId, ex.SourceId);
    }

    [Fact]
    public void InvalidCaptureStateException_StoresCurrentState()
    {
        var state = "Disposed";
        var ex = new InvalidCaptureStateException("Test", state);
        Assert.Equal(state, ex.CurrentState);
    }

    [Fact]
    public void DirectXException_StoresHResult()
    {
        var hresult = unchecked((int)0x80004005);
        var ex = new DirectXException("Test", hresult);
        Assert.Equal(hresult, ex.HResult);
    }

    [Fact]
    public void FrameCaptureException_CanBeCreated()
    {
        var ex = new FrameCaptureException("Test message");
        Assert.Equal("Test message", ex.Message);
    }

    [Fact]
    public void ResourceAllocationException_StoresResourceName()
    {
        var resourceName = "StagingTexture";
        var ex = new ResourceAllocationException("Test", resourceName);
        Assert.Equal(resourceName, ex.ResourceName);
    }

    [Fact]
    public void InvalidConfigurationException_StoresPropertyName()
    {
        var propertyName = "MaxFramesPerSecond";
        var ex = new InvalidConfigurationException("Test", propertyName);
        Assert.Equal(propertyName, ex.PropertyName);
    }

    [Fact]
    public void UnsupportedOperationException_CanBeCreated()
    {
        var ex = new UnsupportedOperationException("Test message");
        Assert.Equal("Test message", ex.Message);
    }
}
