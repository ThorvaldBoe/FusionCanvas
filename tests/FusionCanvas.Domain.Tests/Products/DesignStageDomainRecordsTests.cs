using FusionCanvas.Domain.Products;

namespace FusionCanvas.Domain.Tests.Products;

public sealed class ItemListingConfigurationTests
{
    [Fact]
    public void Configuration_RequiresNonEmptyIds()
    {
        Assert.Throws<ArgumentException>(() => new ItemListingConfiguration(Guid.Empty, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new ItemListingConfiguration(Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void Configuration_RetainsReferences()
    {
        var itemId = Guid.NewGuid();
        var offeringId = Guid.NewGuid();
        var config = new ItemListingConfiguration(itemId, offeringId);
        Assert.Equal(itemId, config.ItemId);
        Assert.Equal(offeringId, config.OfferingId);
    }

    [Fact]
    public void Configuration_EqualityByValue()
    {
        var itemId = Guid.NewGuid();
        var offeringId = Guid.NewGuid();
        Assert.Equal(
            new ItemListingConfiguration(itemId, offeringId),
            new ItemListingConfiguration(itemId, offeringId));
    }
}

public sealed class DesignSelectedColorTests
{
    [Fact]
    public void SelectedColor_RequiresNonEmptyItemId()
    {
        Assert.Throws<ArgumentException>(() => new DesignSelectedColor(Guid.Empty, "Black"));
    }

    [Fact]
    public void SelectedColor_RequiresColorValue()
    {
        Assert.Throws<ArgumentException>(() => new DesignSelectedColor(Guid.NewGuid(), ""));
        Assert.Throws<ArgumentException>(() => new DesignSelectedColor(Guid.NewGuid(), "  "));
    }

    [Fact]
    public void SelectedColor_TrimsColorValue()
    {
        var color = new DesignSelectedColor(Guid.NewGuid(), "  Black  ");
        Assert.Equal("Black", color.ColorValue);
    }

    [Fact]
    public void SelectedColor_RetainsReferences()
    {
        var itemId = Guid.NewGuid();
        var color = new DesignSelectedColor(itemId, "Navy");
        Assert.Equal(itemId, color.ItemId);
        Assert.Equal("Navy", color.ColorValue);
    }
}

public sealed class DesignVariantRowTests
{
    [Fact]
    public void Row_RequiresNonEmptyId()
    {
        Assert.Throws<ArgumentException>(() => new DesignVariantRow(Guid.Empty, Guid.NewGuid(), true, 0));
    }

    [Fact]
    public void Row_RequiresNonEmptyItemId()
    {
        Assert.Throws<ArgumentException>(() => new DesignVariantRow(Guid.NewGuid(), Guid.Empty, true, 0));
    }

    [Fact]
    public void Row_RetainsProperties()
    {
        var id = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var row = new DesignVariantRow(id, itemId, true, 2);
        Assert.Equal(id, row.Id);
        Assert.Equal(itemId, row.ItemId);
        Assert.True(row.IsDefault);
        Assert.Equal(2, row.SortOrder);
    }
}

public sealed class DesignVariantRowColorTests
{
    [Fact]
    public void RowColor_RequiresNonEmptyRowId()
    {
        Assert.Throws<ArgumentException>(() => new DesignVariantRowColor(Guid.Empty, "Red"));
    }

    [Fact]
    public void RowColor_RequiresColorValue()
    {
        Assert.Throws<ArgumentException>(() => new DesignVariantRowColor(Guid.NewGuid(), ""));
    }

    [Fact]
    public void RowColor_TrimsColorValue()
    {
        var rc = new DesignVariantRowColor(Guid.NewGuid(), "  Red  ");
        Assert.Equal("Red", rc.ColorValue);
    }
}

public sealed class DesignSlotAssignmentTests
{
    [Fact]
    public void Assignment_RequiresNonEmptyRowId()
    {
        Assert.Throws<ArgumentException>(() => new DesignSlotAssignment(Guid.Empty, Guid.NewGuid(), null));
    }

    [Fact]
    public void Assignment_RequiresNonEmptyDesignAreaId()
    {
        Assert.Throws<ArgumentException>(() => new DesignSlotAssignment(Guid.NewGuid(), Guid.Empty, null));
    }

    [Fact]
    public void Assignment_AllowsNullAssetId()
    {
        var assignment = new DesignSlotAssignment(Guid.NewGuid(), Guid.NewGuid(), null);
        Assert.Null(assignment.AssetId);
    }

    [Fact]
    public void Assignment_RetainsProperties()
    {
        var rowId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var assignment = new DesignSlotAssignment(rowId, areaId, assetId);
        Assert.Equal(rowId, assignment.RowId);
        Assert.Equal(areaId, assignment.DesignAreaId);
        Assert.Equal(assetId, assignment.AssetId);
    }
}