using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.Domain.Tests.Products;

public sealed class DesignStagePolicyTests
{
    private static WorkspaceSnapshot MakeSnapshot(
        Guid? storeId = null,
        Guid? itemId = null,
        Guid? productId = null,
        Guid? offeringId = null)
    {
        storeId ??= Guid.NewGuid();
        itemId ??= Guid.NewGuid();
        productId ??= Guid.NewGuid();
        offeringId ??= Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        return new WorkspaceSnapshot(
            [new Store(storeId.Value, WorkspaceDefaults.DefaultWorkspaceId, "Test Store", null, false, now, now, "{}")],
            [],
            [],
            [new Item(itemId.Value, storeId.Value, null, null, "Test Item", null, ItemStatus.Draft, WorkflowStage.Design, false, now, now, "{}")],
            [],
            [],
            [],
            [],
            [])
        {
            StoreProducts =
            [
                new StoreProduct(productId.Value, storeId.Value, "Test Product", null, null, now, now, "{}")
            ],
            FulfillmentOfferings =
            [
                new FulfillmentOffering(offeringId.Value, productId.Value, "Test Offering", null,
                    FulfillmentKind.FixedProvider, "Printify", null, now, now, "{}")
            ]
        };
    }

    // --- IsValidConfiguration ---

    [Fact]
    public void IsValidConfiguration_ValidOffering_ReturnsTrue()
    {
        var storeId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var offeringId = Guid.NewGuid();
        var snapshot = MakeSnapshot(storeId, itemId, productId, offeringId);

        var result = DesignStagePolicy.IsValidConfiguration(snapshot, itemId, offeringId);

        Assert.True(result);
    }

    [Fact]
    public void IsValidConfiguration_UnknownItem_ReturnsFalse()
    {
        var snapshot = MakeSnapshot();
        var result = DesignStagePolicy.IsValidConfiguration(snapshot, Guid.NewGuid(), snapshot.FulfillmentOfferings[0].Id);
        Assert.False(result);
    }

    [Fact]
    public void IsValidConfiguration_UnknownOffering_ReturnsFalse()
    {
        var snapshot = MakeSnapshot();
        var itemId = snapshot.Items[0].Id;
        var result = DesignStagePolicy.IsValidConfiguration(snapshot, itemId, Guid.NewGuid());
        Assert.False(result);
    }

    [Fact]
    public void IsValidConfiguration_WrongStore_ReturnsFalse()
    {
        var offeringId = Guid.NewGuid();
        var otherStoreId = Guid.NewGuid();
        var snapshot = MakeSnapshot(offeringId: offeringId);
        var wrongStoreItem = new Item(Guid.NewGuid(), otherStoreId, null, null, "Wrong Item", null,
            ItemStatus.Draft, WorkflowStage.Design, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");
        snapshot = snapshot with
        {
            Items = [.. snapshot.Items, wrongStoreItem],
            Stores = [.. snapshot.Stores, new Store(otherStoreId, WorkspaceDefaults.DefaultWorkspaceId, "Other", null, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}")]
        };

        var result = DesignStagePolicy.IsValidConfiguration(snapshot, wrongStoreItem.Id, offeringId);
        Assert.False(result);
    }

    // --- ValidatePartition ---

    [Fact]
    public void ValidatePartition_EmptyState_DoesNotThrow()
    {
        var itemId = Guid.NewGuid();
        DesignStagePolicy.ValidatePartition(itemId, [], [], []);
    }

    [Fact]
    public void ValidatePartition_SingleDefaultRowAndAllColors_DoesNotThrow()
    {
        var itemId = Guid.NewGuid();
        var rowId = Guid.NewGuid();
        var selected = new List<DesignSelectedColor>
        {
            new(itemId, "Black"),
            new(itemId, "White")
        };
        var rows = new List<DesignVariantRow>
        {
            new(rowId, itemId, true, 0)
        };
        var rowColors = new List<DesignVariantRowColor>
        {
            new(rowId, "Black"),
            new(rowId, "White")
        };

        DesignStagePolicy.ValidatePartition(itemId, selected, rows, rowColors);
    }

    [Fact]
    public void ValidatePartition_MissingDefaultRow_Throws()
    {
        var itemId = Guid.NewGuid();
        var selected = new List<DesignSelectedColor> { new(itemId, "Black") };

        Assert.Throws<InvalidOperationException>(() =>
            DesignStagePolicy.ValidatePartition(itemId, selected, [], []));
    }

    [Fact]
    public void ValidatePartition_ColorMissingFromRow_Throws()
    {
        var itemId = Guid.NewGuid();
        var rowId = Guid.NewGuid();
        var selected = new List<DesignSelectedColor>
        {
            new(itemId, "Black"),
            new(itemId, "Red")
        };
        var rows = new List<DesignVariantRow>
        {
            new(rowId, itemId, true, 0)
        };
        var rowColors = new List<DesignVariantRowColor>
        {
            new(rowId, "Black")
        };

        Assert.Throws<InvalidOperationException>(() =>
            DesignStagePolicy.ValidatePartition(itemId, selected, rows, rowColors));
    }

    [Fact]
    public void ValidatePartition_ExtraColorInRow_Throws()
    {
        var itemId = Guid.NewGuid();
        var rowId = Guid.NewGuid();
        var selected = new List<DesignSelectedColor>
        {
            new(itemId, "Black")
        };
        var rows = new List<DesignVariantRow>
        {
            new(rowId, itemId, true, 0)
        };
        var rowColors = new List<DesignVariantRowColor>
        {
            new(rowId, "Black"),
            new(rowId, "Green")
        };

        Assert.Throws<InvalidOperationException>(() =>
            DesignStagePolicy.ValidatePartition(itemId, selected, rows, rowColors));
    }

    [Fact]
    public void ValidatePartition_DuplicateColorAcrossRows_Throws()
    {
        var itemId = Guid.NewGuid();
        var row1Id = Guid.NewGuid();
        var row2Id = Guid.NewGuid();
        var selected = new List<DesignSelectedColor>
        {
            new(itemId, "Black"),
            new(itemId, "White")
        };
        var rows = new List<DesignVariantRow>
        {
            new(row1Id, itemId, true, 0),
            new(row2Id, itemId, false, 1)
        };
        var rowColors = new List<DesignVariantRowColor>
        {
            new(row1Id, "Black"),
            new(row2Id, "White"),
            new(row2Id, "Black")  // Black already in row1
        };

        Assert.Throws<InvalidOperationException>(() =>
            DesignStagePolicy.ValidatePartition(itemId, selected, rows, rowColors));
    }

    // --- ValidateSlots ---

    [Fact]
    public void ValidateSlots_NoAssignments_DoesNotThrow()
    {
        var area = new DesignArea(Guid.NewGuid(), Guid.NewGuid(), "Front", null, "Front-center",
            "Screen", 100, 200, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");
        DesignStagePolicy.ValidateSlots([area], []);
    }

    [Fact]
    public void ValidateSlots_ValidAssignment_DoesNotThrow()
    {
        var areaId = Guid.NewGuid();
        var area = new DesignArea(areaId, Guid.NewGuid(), "Front", null, "Front-center",
            "Screen", 100, 200, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");
        var assignment = new DesignSlotAssignment(Guid.NewGuid(), areaId, null);

        DesignStagePolicy.ValidateSlots([area], [assignment]);
    }

    [Fact]
    public void ValidateSlots_UnknownArea_Throws()
    {
        var assignment = new DesignSlotAssignment(Guid.NewGuid(), Guid.NewGuid(), null);
        var area = new DesignArea(Guid.NewGuid(), Guid.NewGuid(), "Front", null, "Front-center",
            "Screen", 100, 200, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");

        Assert.Throws<InvalidOperationException>(() =>
            DesignStagePolicy.ValidateSlots([area], [assignment]));
    }

    [Fact]
    public void ValidateSlots_DuplicateSlot_Throws()
    {
        var areaId = Guid.NewGuid();
        var rowId = Guid.NewGuid();
        var area = new DesignArea(areaId, Guid.NewGuid(), "Front", null, "Front-center",
            "Screen", 100, 200, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");
        var assignments = new List<DesignSlotAssignment>
        {
            new(rowId, areaId, null),
            new(rowId, areaId, Guid.NewGuid())
        };

        Assert.Throws<InvalidOperationException>(() =>
            DesignStagePolicy.ValidateSlots([area], assignments));
    }

    // --- AvailableColors ---

    [Fact]
    public void AvailableColors_ReturnsDeduplicatedColorValues()
    {
        var offeringId = Guid.NewGuid();
        var variants = new List<ProductVariant>
        {
            new(Guid.NewGuid(), offeringId,
                [new VariantOption("Color", "Black"), new VariantOption("Size", "M")],
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
            new(Guid.NewGuid(), offeringId,
                [new VariantOption("Color", "Black"), new VariantOption("Size", "L")],
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
            new(Guid.NewGuid(), offeringId,
                [new VariantOption("Color", "White"), new VariantOption("Size", "M")],
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
        };

        var colors = DesignStagePolicy.AvailableColors(variants, offeringId);

        Assert.Equal(2, colors.Count);
        Assert.Contains(colors, c => c.Equals("Black", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(colors, c => c.Equals("White", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AvailableColors_OnlyColorOptions_IgnoresSize()
    {
        var offeringId = Guid.NewGuid();
        var variants = new List<ProductVariant>
        {
            new(Guid.NewGuid(), offeringId,
                [new VariantOption("Color", "Navy"), new VariantOption("Size", "XL")],
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
            new(Guid.NewGuid(), offeringId,
                [new VariantOption("Color", "Red"), new VariantOption("Size", "S")],
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
        };

        var colors = DesignStagePolicy.AvailableColors(variants, offeringId);

        Assert.Equal(2, colors.Count);
        Assert.DoesNotContain(colors, c => c.Equals("XL"));
        Assert.DoesNotContain(colors, c => c.Equals("S"));
    }

    // --- AreaIdsForOffering ---

    [Fact]
    public void AreaIdsForOffering_ReturnsAreasForOffering()
    {
        var offeringId = Guid.NewGuid();
        var otherOfferingId = Guid.NewGuid();
        var area1 = new DesignArea(Guid.NewGuid(), offeringId, "Front", null, "Front",
            "Screen", 100, 200, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");
        var area2 = new DesignArea(Guid.NewGuid(), offeringId, "Back", null, "Back",
            "Screen", 100, 200, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");
        var otherArea = new DesignArea(Guid.NewGuid(), otherOfferingId, "Side", null, "Side",
            "Screen", 100, 200, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");

        var ids = DesignStagePolicy.AreaIdsForOffering([area1, area2, otherArea], offeringId);

        Assert.Equal(2, ids.Count);
        Assert.Contains(area1.Id, ids);
        Assert.Contains(area2.Id, ids);
    }
}