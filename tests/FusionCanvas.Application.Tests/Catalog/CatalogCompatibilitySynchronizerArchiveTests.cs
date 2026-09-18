using FusionCanvas.Application.Catalog;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests.Catalog;

public sealed class CatalogCompatibilitySynchronizerArchiveTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ArchivedOfferingDoesNotRecreateLegacyDescendantsOnRepeatedSynchronization()
    {
        var storeId = Guid.NewGuid();
        var blueprintId = Guid.NewGuid();
        var offeringId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var optionId = Guid.NewGuid();
        var valueId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var blueprint = new Blueprint(blueprintId, storeId, "T-shirt", null, false, Now, Now);
        var offering = new BlueprintOffering(offeringId, blueprintId, storeId, "Tee", null,
            BlueprintOfferingKind.ProviderNetwork, null, "network", null, null, true, Now, Now);
        var legacyOffering = new FulfillmentOffering(offeringId, blueprintId, "Tee", null,
            FulfillmentKind.PrintifyChoiceNetwork, null, null, Now, Now, "{}");
        var legacyVariant = new ProductVariant(variantId, offeringId,
            [new VariantOption("Color", "Black"), new VariantOption("Size", "M")], Now, Now);
        var snapshot = new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)],
            [new Store(storeId, "First", null, false, Now, Now, "{}")], [], [], [], [], [], [], [], [])
        {
            Blueprints = [blueprint],
            BlueprintOfferings = [offering],
            OfferingOptions = [new OfferingOption(optionId, offeringId, OptionKind.Color, "Color", 0, true)],
            OfferingOptionValues = [new OfferingOptionValue(valueId, optionId, offeringId, "Black", 0, true)],
            OfferingVariants = [new OfferingVariant(variantId, offeringId, "Black", [valueId], true, Now, Now)],
            OfferingPlaceholders = [new OfferingPlaceholder(areaId, offeringId, "Front", null, "front", "DTG", 1200, 1400, [variantId], true, Now, Now)],
            FulfillmentOfferings = [legacyOffering],
            ProductVariants = [legacyVariant],
            DesignAreas = [new DesignArea(areaId, offeringId, "Front", null, "front", "DTG", 1200, 1400, [variantId], Now, Now, "{}")]
        };

        var first = CatalogCompatibilitySynchronizer.SynchronizeStore(snapshot, storeId, () => Now, Guid.NewGuid);
        var second = CatalogCompatibilitySynchronizer.SynchronizeStore(first.Snapshot, storeId, () => Now, Guid.NewGuid);

        Assert.True(first.Changed);
        Assert.False(second.Changed);
        Assert.Single(second.Snapshot.OfferingOptions);
        Assert.Single(second.Snapshot.OfferingOptionValues);
        Assert.Single(second.Snapshot.OfferingVariants);
        Assert.Single(second.Snapshot.OfferingPlaceholders);
    }

    [Fact]
    public void ExistingNormalizedVariantPreventsLegacyProjectionFromRecreatingItsArchivedOption()
    {
        var storeId = Guid.NewGuid();
        var blueprintId = Guid.NewGuid();
        var offeringId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var optionId = Guid.NewGuid();
        var valueId = Guid.NewGuid();
        var blueprint = new Blueprint(blueprintId, storeId, "T-shirt", null, false, Now, Now);
        var offering = new BlueprintOffering(offeringId, blueprintId, storeId, "Tee", null,
            BlueprintOfferingKind.ProviderNetwork, null, "network", null, null, false, Now, Now);
        var product = new StoreProduct(blueprintId, storeId, "T-shirt", null, null, Now, Now, "{}");
        var legacyOffering = new FulfillmentOffering(offeringId, blueprintId, "Tee", null,
            FulfillmentKind.PrintifyChoiceNetwork, null, null, Now, Now, "{}");
        var snapshot = new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)],
            [new Store(storeId, "First", null, false, Now, Now, "{}")], [], [], [], [], [], [], [], [])
        {
            Blueprints = [blueprint],
            BlueprintOfferings = [offering],
            OfferingOptions = [new OfferingOption(optionId, offeringId, OptionKind.Color, "Color", 0, true)],
            OfferingOptionValues = [new OfferingOptionValue(valueId, optionId, offeringId, "Black", 0, true)],
            OfferingVariants = [new OfferingVariant(variantId, offeringId, "Black", [valueId], true, Now, Now)],
            StoreProducts = [product],
            FulfillmentOfferings = [legacyOffering],
            ProductVariants = [new ProductVariant(variantId, offeringId, [new VariantOption("Color", "Black")], Now, Now)]
        };

        var result = CatalogCompatibilitySynchronizer.SynchronizeStore(snapshot, storeId, () => Now, Guid.NewGuid);

        Assert.Single(result.Snapshot.OfferingOptions);
        Assert.Single(result.Snapshot.OfferingOptionValues);
        Assert.Single(result.Snapshot.OfferingVariants);
    }
}
