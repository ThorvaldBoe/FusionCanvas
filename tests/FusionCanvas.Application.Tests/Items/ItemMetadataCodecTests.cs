using FusionCanvas.Application.Items;

namespace FusionCanvas.Application.Tests.Items;

public sealed class ItemMetadataCodecTests
{
    [Fact]
    public void SanitizeCreativeContextMetadata_ExcludesOperationalAndSecretKeys()
    {
        var metadata = ItemMetadataCodec.SanitizeCreativeContextMetadata(
            """{"brand":"playful","id":"item-1","createdAt":"2026-01-01","status":"Draft","inheritedFrom:brand":"store","dbPath":"C:\\db","apiKey":"secret","password":"secret","credential":"secret","token":"secret","secret":"secret"}""");

        Assert.Equal(new Dictionary<string, string> { ["brand"] = "playful" }, metadata);
    }

    [Fact]
    public void SanitizeCreativeContextMetadata_InvalidJsonReturnsEmptyMetadata()
    {
        var metadata = ItemMetadataCodec.SanitizeCreativeContextMetadata("not-json");

        Assert.Empty(metadata);
    }
}
