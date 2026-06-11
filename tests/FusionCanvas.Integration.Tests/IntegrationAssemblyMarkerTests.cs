using FusionCanvas.Integration;

namespace FusionCanvas.Integration.Tests;

public class IntegrationAssemblyMarkerTests
{
    [Fact]
    public void IntegrationAssemblyMarker_IsInIntegrationAssembly()
    {
        Assert.Equal("FusionCanvas.Integration", typeof(IntegrationAssemblyMarker).Assembly.GetName().Name);
    }
}
