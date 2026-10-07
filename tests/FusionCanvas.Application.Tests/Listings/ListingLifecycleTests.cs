using FusionCanvas.Application.Listings;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.Application.Tests.Listings;

public sealed class ListingLifecycleTests
{
    [Fact]
    public void Placement_scales_to_full_width_and_aligns_top_edge()
    {
        var placement = ListingPlacementCalculator.Calculate(
            new ArtworkDimensions(1000, 2000),
            new ArtworkDimensions(2000, 3000));

        Assert.Equal(2d, placement.Scale);
        Assert.Equal(0.5d, placement.X);
        Assert.Equal(2d / 3d, placement.Y, precision: 10);
        Assert.Equal(0d, placement.Angle);
    }

    [Fact]
    public void Pricing_fixed_profit_preserves_variant_cost_differences()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        var prices = ListingPricingCalculator.Calculate(
            new ListingPricingInput(ListingPricingPolicy.FixedProfitAmount, 10m),
            new Dictionary<Guid, decimal>
            {
                [first] = 12m,
                [second] = 15m
            });

        Assert.Equal(22m, prices.Single(value => value.SourceVariantId == first).RetailPrice);
        Assert.Equal(25m, prices.Single(value => value.SourceVariantId == second).RetailPrice);
    }

    [Fact]
    public void Pricing_rejects_negative_amount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ListingPricingCalculator.Calculate(
                new ListingPricingInput(ListingPricingPolicy.FixedRetailPrice, -1m),
                new Dictionary<Guid, decimal> { [Guid.NewGuid()] = 10m }));
    }

    [Fact]
    public void Drift_comparison_identifies_remote_only_change()
    {
        var comparison = ListingDriftComparer.Compare(
            new ListingSnapshot(new Dictionary<string, string?> { ["price"] = "30" }),
            new ListingSnapshot(new Dictionary<string, string?> { ["price"] = "40" }),
            new ListingSnapshot(new Dictionary<string, string?> { ["price"] = "30" }));

        var change = Assert.Single(comparison.Changes);
        Assert.Equal("price", change.Field);
        Assert.True(change.RemoteChanged);
        Assert.False(change.LocalChanged);
        Assert.True(comparison.HasRemoteOnlyChanges);
        Assert.False(comparison.HasConflict);
    }

    [Fact]
    public void Drift_comparison_reports_local_and_both_changed_fields()
    {
        var comparison = ListingDriftComparer.Compare(
            new ListingSnapshot(new Dictionary<string, string?> { ["title"] = "old", ["description"] = "old" }),
            new ListingSnapshot(new Dictionary<string, string?> { ["title"] = "old", ["description"] = "remote" }),
            new ListingSnapshot(new Dictionary<string, string?> { ["title"] = "local", ["description"] = "local" }));

        Assert.Contains(comparison.Changes, value => value.Field == "title" && !value.RemoteChanged && value.LocalChanged);
        Assert.Contains(comparison.Changes, value => value.Field == "description" && value.RemoteChanged && value.LocalChanged);
        Assert.True(comparison.HasConflict);
    }

    [Fact]
    public void Drift_comparison_reports_missing_remote_fields()
    {
        var comparison = ListingDriftComparer.Compare(
            new ListingSnapshot(new Dictionary<string, string?> { ["title"] = "same", ["removed"] = "value" }),
            new ListingSnapshot(new Dictionary<string, string?> { ["title"] = "same" }),
            new ListingSnapshot(new Dictionary<string, string?> { ["title"] = "same" }));

        Assert.Single(comparison.Changes);
        Assert.Equal("removed", comparison.Changes[0].Field);
        Assert.True(comparison.Changes[0].RemoteChanged);
    }

    [Fact]
    public void Drift_comparison_is_clean_when_all_values_match()
    {
        var snapshot = new ListingSnapshot(new Dictionary<string, string?> { ["title"] = "same" });

        var comparison = ListingDriftComparer.Compare(snapshot, snapshot, snapshot);

        Assert.True(comparison.IsClean);
    }

    [Fact]
    public void Projection_uses_design_colors_and_builds_tolerant_artwork()
    {
        var request = CreateRequest();

        var result = ListingProjectionBuilder.Build(request);

        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(value => value.Message)));
        Assert.NotNull(result.Projection);
        var projection = result.Projection!;
        Assert.Equal("Dad Joke Loading… – T-shirt", projection.Title);
        Assert.Single(projection.Variants);
        Assert.Equal(30m, projection.Variants[0].Price.RetailPrice);
        Assert.Single(projection.Artwork);
        Assert.Equal(2d, projection.Artwork[0].Placement.Scale);
        Assert.Equal(0d, projection.Artwork[0].Placement.Angle);
    }

    [Fact]
    public void Projection_rejects_missing_design_assignment()
    {
        var request = CreateRequest() with { SlotAssignments = [] };

        var result = ListingProjectionBuilder.Build(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, value => value.Code == "artwork-assignment-missing");
    }

    private static ListingProjectionRequest CreateRequest()
    {
        var now = DateTimeOffset.UtcNow;
        var storeId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var blueprintId = Guid.NewGuid();
        var offeringId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var optionId = Guid.NewGuid();
        var optionValueId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var rowId = Guid.NewGuid();
        var assetId = Guid.NewGuid();

        return new ListingProjectionRequest(
            new Item(itemId, storeId, null, null, "Dad Joke Loading…", "A joke shirt.", ItemStatus.Draft, WorkflowStage.Listing, false, now, now, "{}"),
            new Blueprint(blueprintId, storeId, "T-shirt", null, false, now, now),
            new BlueprintOffering(offeringId, blueprintId, storeId, "SwiftPod T-shirt", null, BlueprintOfferingKind.FixedPrintProvider, providerId, null, null, null, false, now, now),
            new PrintProvider(providerId, storeId, "SwiftPod", "provider-1", false, now, now),
            [
                new OfferingVariant(variantId, offeringId, "Black / M", [optionValueId], false, now, now, "{\"productionCost\":20}")
            ],
            [new OfferingOption(optionId, offeringId, OptionKind.Color, "Color", 0)],
            [new OfferingOptionValue(optionValueId, optionId, offeringId, "Black", 0)],
            [new DesignSelectedColor(itemId, "Black")],
            [new DesignVariantRow(rowId, itemId, true, 0)],
            [new DesignVariantRowColor(rowId, "Black")],
            [new DesignSlotAssignment(rowId, areaId, assetId)],
            [new DesignArea(areaId, offeringId, "Front", null, "front", "dtg", 2000, 3000, [], now, now, "{}")],
            [new Asset(assetId, storeId, "Dad joke artwork", null, AssetKind.ExportedImage, "assets/dad-joke.png", null, false, false, now, now, "{}")],
            new Dictionary<Guid, ArtworkDimensions> { [assetId] = new(1000, 1000) },
            new ListingPricingInput(ListingPricingPolicy.FixedRetailPrice, 30m),
            "Default",
            "Hide out of stock");
    }
}
