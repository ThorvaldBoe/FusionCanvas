using FusionCanvas.Integration.AI;

namespace FusionCanvas.Integration.Tests.AI;

public sealed class EmbeddedDesignTriangleGuidanceSourceTests
{
    [Fact]
    public void Load_ReturnsCanonicalFrameworkContent()
    {
        var source = new EmbeddedDesignTriangleGuidanceSource();
        var content = source.Load();

        Assert.False(string.IsNullOrWhiteSpace(content));
        Assert.Contains("Foundations of PoD Design", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Design Triangle and Design Pyramid", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Sketch Layout Language", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Generating SLL", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("—", content, StringComparison.Ordinal);
        Assert.Contains("“", content, StringComparison.Ordinal);
        Assert.DoesNotContain("â", content, StringComparison.Ordinal);
        Assert.Contains("idea", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("phrase", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("graphic", content, StringComparison.OrdinalIgnoreCase);
    }
}
