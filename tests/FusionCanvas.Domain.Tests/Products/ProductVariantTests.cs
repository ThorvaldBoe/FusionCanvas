using FusionCanvas.Domain.Products;

namespace FusionCanvas.Domain.Tests.Products;

public sealed class ProductVariantTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);

    private static readonly VariantOption[] Options =
    [
        new("Color", "Black"),
        new("Size", "M")
    ];

    [Fact]
    public void ProductVariant_RequiresAtLeastOneOption()
    {
        Assert.Throws<ArgumentException>(() =>
            new ProductVariant(Guid.NewGuid(), Guid.NewGuid(), [], Now, Now));
        Assert.Throws<ArgumentException>(() =>
            new ProductVariant(Guid.NewGuid(), Guid.NewGuid(), null!, Now, Now));
    }

    [Fact]
    public void ProductVariant_KeepsOptionsInOrder()
    {
        var variant = new ProductVariant(Guid.NewGuid(), Guid.NewGuid(), Options, Now, Now);
        Assert.Equal(["Color", "Size"], variant.Options.Select(o => o.Name));
    }

    [Fact]
    public void ProductVariant_RequiresIds()
    {
        Assert.Throws<ArgumentException>(() => new ProductVariant(Guid.Empty, Guid.NewGuid(), Options, Now, Now));
        Assert.Throws<ArgumentException>(() => new ProductVariant(Guid.NewGuid(), Guid.Empty, Options, Now, Now));
    }
}

public sealed class VariantOptionTests
{
    [Fact]
    public void VariantOption_RequiresNameAndValue()
    {
        Assert.Throws<ArgumentException>(() => new VariantOption("  ", "Black"));
        Assert.Throws<ArgumentException>(() => new VariantOption("Color", "  "));
    }

    [Fact]
    public void VariantOption_RetainsValues()
    {
        var option = new VariantOption("Color", "Black");
        Assert.Equal("Color", option.Name);
        Assert.Equal("Black", option.Value);
    }
}
