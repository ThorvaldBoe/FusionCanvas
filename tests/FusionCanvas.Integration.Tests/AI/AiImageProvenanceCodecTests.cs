using FusionCanvas.Application.AI;
using FusionCanvas.Integration.AI;

namespace FusionCanvas.Integration.Tests.AI;

public sealed class AiImageProvenanceCodecTests
{
    [Fact]
    public void Codec_RoundTripsCompleteNonSecretProvenance()
    {
        var source = new AiImageProvenance("OpenRouter", "selected/model", "provider/model", "prompt", new(512, 512), new(1200, 1400), true, false, DateTimeOffset.UtcNow, "request-1", new(2, 3, 0.04m), ["warning"]);

        var codec = new AiImageProvenanceCodec();
        var json = codec.Serialize(source);

        Assert.True(codec.TryDeserialize(json, out var restored));
        Assert.NotNull(restored);
        if (restored is null) return;
        Assert.Equal(source.Provider, restored.Provider);
        Assert.Equal(source.SelectedModelId, restored.SelectedModelId);
        Assert.Equal(source.ResolvedModelId, restored.ResolvedModelId);
        Assert.Equal(source.Prompt, restored.Prompt);
        Assert.Equal(source.RequestedSize, restored.RequestedSize);
        Assert.Equal(source.FinalSize, restored.FinalSize);
        Assert.Equal(source.TransparencyRequested, restored.TransparencyRequested);
        Assert.Equal(source.ResultHasTransparency, restored.ResultHasTransparency);
        Assert.Equal(source.GeneratedAt, restored.GeneratedAt);
        Assert.Equal(source.ProviderRequestId, restored.ProviderRequestId);
        Assert.Equal(source.Usage, restored.Usage);
        Assert.Equal(source.Warnings, restored.Warnings);
        Assert.DoesNotContain("api", json, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{\"version\":99,\"provenance\":{}}")]
    [InlineData("{\"version\":1,\"provenance\":null}")]
    public void Codec_RejectsMalformedOrUnknownVersionsWithoutThrowing(string json)
    {
        Assert.False(new AiImageProvenanceCodec().TryDeserialize(json, out _));
    }
}
