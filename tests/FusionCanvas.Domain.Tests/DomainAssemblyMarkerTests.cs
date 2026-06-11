using FusionCanvas.Domain;

namespace FusionCanvas.Domain.Tests;

public class DomainAssemblyMarkerTests
{
    [Fact]
    public void DomainAssemblyMarker_IsInDomainAssembly()
    {
        Assert.Equal("FusionCanvas.Domain", typeof(DomainAssemblyMarker).Assembly.GetName().Name);
    }
}
