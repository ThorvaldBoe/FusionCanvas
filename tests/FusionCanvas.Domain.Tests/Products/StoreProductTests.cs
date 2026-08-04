using FusionCanvas.Domain.Products;

namespace FusionCanvas.Domain.Tests.Products;

public sealed class StoreProductTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);

    private static StoreProduct Product(string? name = "Gildan 64000") =>
        new(Guid.NewGuid(), Guid.NewGuid(), name ?? string.Empty, null, "ext-123", Now, Now, "{}");

    [Fact]
    public void StoreProduct_RequiresNonEmptyId()
    {
        var storeId = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => new StoreProduct(Guid.Empty, storeId, "Gildan 64000", null, null, Now, Now, "{}"));
    }

    [Fact]
    public void StoreProduct_RequiresNonEmptyStoreId()
    {
        Assert.Throws<ArgumentException>(() => new StoreProduct(Guid.NewGuid(), Guid.Empty, "Gildan 64000", null, null, Now, Now, "{}"));
    }

    [Fact]
    public void StoreProduct_RequiresName()
    {
        Assert.Throws<ArgumentException>(() => new StoreProduct(Guid.NewGuid(), Guid.NewGuid(), "   ", null, null, Now, Now, "{}"));
    }

    [Fact]
    public void StoreProduct_NormalizesBlankExternalIdToNull()
    {
        var product = new StoreProduct(Guid.NewGuid(), Guid.NewGuid(), "Gildan 64000", null, "   ", Now, Now, "{}");
        Assert.Null(product.ExternalProductId);
    }

    [Fact]
    public void StoreProduct_KeepsValidExternalId()
    {
        Assert.Equal("ext-123", Product().ExternalProductId);
    }

    [Fact]
    public void StoreProduct_DefaultEmptyMetadata()
    {
        var product = new StoreProduct(Guid.NewGuid(), Guid.NewGuid(), "Gildan 64000", null, null, Now, Now, string.Empty);
        Assert.Equal("{}", product.MetadataJson);
    }
}
