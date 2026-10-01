using FusionCanvas.Application.Metadata;

namespace FusionCanvas.Application.Tests.Metadata;

public sealed class StringMetadataCodecTests
{
    [Fact]
    public void ParseAndSerialize_PreserveStringValuedMetadataAndUnknownKeys()
    {
        var metadata = StringMetadataCodec.Parse("""{"notes":"Keep me","custom":"Retain me"}""");

        StringMetadataCodec.SetOptional(metadata, "notes", " Updated ");
        var serialized = StringMetadataCodec.Serialize(metadata);

        Assert.Equal("Updated", metadata["notes"]);
        Assert.Contains("\"custom\":\"Retain me\"", serialized);
        Assert.Contains("\"notes\":\"Updated\"", serialized);
    }

    [Fact]
    public void SetOptional_RemovesBlankValuesAndSerializesEmptyMetadataAsObject()
    {
        var metadata = StringMetadataCodec.Parse("""{"notes":"Old"}""");

        StringMetadataCodec.SetOptional(metadata, "notes", "  ");

        Assert.Empty(metadata);
        Assert.Equal("{}", StringMetadataCodec.Serialize(metadata));
    }

    [Fact]
    public void Parse_NonObjectJsonReturnsEmptyMetadata()
    {
        var metadata = StringMetadataCodec.Parse("[]");

        Assert.Empty(metadata);
    }
}
