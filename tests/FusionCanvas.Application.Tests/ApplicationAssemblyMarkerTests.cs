using FusionCanvas.Application;

namespace FusionCanvas.Application.Tests;

public class ApplicationAssemblyMarkerTests
{
    [Fact]
    public void ApplicationAssemblyMarker_IsInApplicationAssembly()
    {
        Assert.Equal("FusionCanvas.Application", typeof(ApplicationAssemblyMarker).Assembly.GetName().Name);
    }
}
