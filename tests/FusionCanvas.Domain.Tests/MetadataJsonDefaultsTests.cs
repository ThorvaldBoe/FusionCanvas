using FusionCanvas.Domain;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Products;

namespace FusionCanvas.Domain.Tests;

public sealed class MetadataJsonDefaultsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_UsesEmptyObjectForMissingMetadata(string? metadataJson)
    {
        Assert.Equal(MetadataJsonDefaults.EmptyObject, MetadataJsonDefaults.Normalize(metadataJson));
    }

    [Fact]
    public void Normalize_PreservesExplicitMetadata()
    {
        const string metadataJson = "{\"custom\":\"value\"}";

        Assert.Equal(metadataJson, MetadataJsonDefaults.Normalize(metadataJson));
    }

    [Fact]
    public void DomainRecords_UseTheSharedMetadataDefault()
    {
        var now = DateTimeOffset.UtcNow;
        var blueprint = new Blueprint(Guid.NewGuid(), Guid.NewGuid(), "T-shirt", null, false, now, now, " ");
        var product = new StoreProduct(Guid.NewGuid(), Guid.NewGuid(), "T-shirt", null, null, now, now, null!);

        Assert.Equal(MetadataJsonDefaults.EmptyObject, blueprint.MetadataJson);
        Assert.Equal(MetadataJsonDefaults.EmptyObject, product.MetadataJson);
    }
}
