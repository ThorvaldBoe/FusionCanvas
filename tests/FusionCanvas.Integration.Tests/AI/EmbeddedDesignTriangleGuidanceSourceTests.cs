using FusionCanvas.Integration.AI;

namespace FusionCanvas.Integration.Tests.AI;

public sealed class EmbeddedDesignTriangleGuidanceSourceTests
{
    [Fact]
    public void Load_ReturnsNonEmptyContentMentioningIdeaPhraseAndGraphic()
    {
        var source = new EmbeddedDesignTriangleGuidanceSource();
        var content = source.Load();

        Assert.False(string.IsNullOrWhiteSpace(content));
        Assert.Contains("idea", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("phrase", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("graphic", content, StringComparison.OrdinalIgnoreCase);
    }
}