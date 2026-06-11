using FusionCanvas.App;

namespace FusionCanvas.App.Tests;

public class AppAssemblyMarkerTests
{
    [Fact]
    public void AppType_IsInAppAssembly()
    {
        Assert.Equal("FusionCanvas.App", typeof(App).Assembly.GetName().Name);
    }
}
