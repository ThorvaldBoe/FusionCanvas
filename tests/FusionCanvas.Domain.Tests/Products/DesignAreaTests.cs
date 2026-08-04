using FusionCanvas.Domain.Products;

namespace FusionCanvas.Domain.Tests.Products;

public sealed class DesignAreaTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);

    private static DesignArea Area(
        int width = 3000,
        int height = 4500,
        IReadOnlyList<Guid>? variantIds = null,
        string? position = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Front", null, position ?? "front", "DTG", width, height, variantIds, Now, Now, "{}");

    [Fact]
    public void DesignArea_RequiresPositiveDimensions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Area(width: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Area(height: -1));
    }

    [Fact]
    public void DesignArea_RequiresPositionAndDecorationMethod()
    {
        Assert.Throws<ArgumentException>(() => Area(position: "  "));
        Assert.Throws<ArgumentException>(() => new DesignArea(
            Guid.NewGuid(), Guid.NewGuid(), "Front", null, "front", "  ", 3000, 4500, null, Now, Now, "{}"));
    }

    [Fact]
    public void DesignArea_NoVariantRestrictionAppliesToAll()
    {
        Assert.Empty(Area(variantIds: null).VariantIds);
        Assert.Empty(Area(variantIds: []).VariantIds);
    }

    [Fact]
    public void DesignArea_DeduplicatesApplicableVariants()
    {
        var variant = Guid.NewGuid();
        var area = Area(variantIds: [variant, variant, Guid.NewGuid()]);
        Assert.Equal([variant], area.VariantIds.Where(id => id == variant).ToList());
        Assert.Equal(area.VariantIds.Count, area.VariantIds.Distinct().Count());
    }

    [Fact]
    public void DesignArea_RetainsDimensions()
    {
        var area = Area();
        Assert.Equal(3000, area.Width);
        Assert.Equal(4500, area.Height);
    }

    [Fact]
    public void DesignArea_RequiresIds()
    {
        Assert.Throws<ArgumentException>(() => new DesignArea(
            Guid.Empty, Guid.NewGuid(), "Front", null, "front", "DTG", 3000, 4500, null, Now, Now, "{}"));
        Assert.Throws<ArgumentException>(() => new DesignArea(
            Guid.NewGuid(), Guid.Empty, "Front", null, "front", "DTG", 3000, 4500, null, Now, Now, "{}"));
    }
}
