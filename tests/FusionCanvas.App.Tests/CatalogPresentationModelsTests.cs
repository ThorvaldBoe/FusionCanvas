using FusionCanvas.App.Stores;
using FusionCanvas.Application.Catalog;
using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.App.Tests;

public sealed class CatalogPresentationModelsTests
{
    [Theory]
    [InlineData(false, 0, 1, 1, "Setup incomplete")]
    [InlineData(false, 1, 1, 1, "Ready")]
    [InlineData(true, 1, 1, 1, "Archived")]
    public void OfferingCardDerivesLifecycleAndAllSetupCounts(bool archived, int variants, int areas, int templates, string expected)
    {
        var summary = new BlueprintOfferingSetupSummary(
            new OfferingContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            "Gildan 64000",
            null,
            archived,
            new OfferingFulfillmentContext(BlueprintOfferingKind.FixedPrintProvider, "SwiftPOD", false),
            new OfferingSetupCounts(variants, areas, templates));

        var card = BlueprintOfferingCardViewModel.From(summary);

        Assert.Equal(expected, card.Status);
        Assert.Equal("SwiftPOD", card.FulfillmentContext);
        Assert.Equal(variants, card.VariantCount);
        Assert.Equal(areas, card.DesignAreaCount);
        Assert.Equal(templates, card.MockupTemplateCount);
    }

    [Fact]
    public void SellableVariantUsesStableOptionKindsInsteadOfEditableNames()
    {
        var offeringId = Guid.NewGuid();
        var colorOption = new OfferingOption(Guid.NewGuid(), offeringId, OptionKind.Color, "Shade", 0);
        var sizeOption = new OfferingOption(Guid.NewGuid(), offeringId, OptionKind.Size, "Dimensions", 1);
        var black = new OfferingOptionValue(Guid.NewGuid(), colorOption.Id, offeringId, "Black", 0);
        var medium = new OfferingOptionValue(Guid.NewGuid(), sizeOption.Id, offeringId, "M", 0);
        var variant = new OfferingVariant(Guid.NewGuid(), offeringId, "Black / M", [black.Id, medium.Id], false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        var row = SellableVariantRowViewModel.From(variant, [colorOption, sizeOption], [black, medium]);

        Assert.Equal("Black", row.Color);
        Assert.Equal("M", row.Size);
        Assert.Contains("Color: Black", row.SemanticSummary);
        Assert.Contains("Size: M", row.SemanticSummary);
    }

    [Fact]
    public void SellableVariantReportsUnresolvedIdentityWithoutInferringFromName()
    {
        var offeringId = Guid.NewGuid();
        var variant = new OfferingVariant(Guid.NewGuid(), offeringId, "Black XXL", [Guid.NewGuid()], false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        var row = SellableVariantRowViewModel.From(variant, [], []);

        Assert.Null(row.Color);
        Assert.Null(row.Size);
        Assert.Equal("Unavailable value", row.Other);
    }

    [Fact]
    public void DesignAreaCardDistinguishesAllVariantsFromSubset()
    {
        var all = DesignAreaCardViewModel.From(new DesignAreaSetupSummary(Guid.NewGuid(), "Front", "front", 4500, 5400, null, null, true, 3, null));
        var subset = DesignAreaCardViewModel.From(new DesignAreaSetupSummary(Guid.NewGuid(), "Sleeve", "left-sleeve", 1200, 800, null, null, false, 2, null));

        Assert.Equal("All active Variants", all.CompatibilitySummary);
        Assert.Equal("2 compatible Variants", subset.CompatibilitySummary);
        Assert.Equal(4500, all.MaximumWidthPixels);
        Assert.Contains("px", all.MaximumSizeSummary);
    }

    [Fact]
    public void MockupCardResolvesColorsAndRevisionFromStableSummary()
    {
        var offeringId = Guid.NewGuid();
        var optionId = Guid.NewGuid();
        var black = new OfferingOptionValue(Guid.NewGuid(), optionId, offeringId, "Black", 0);
        var summary = new MockupTemplateSetupSummary(Guid.NewGuid(), "Front lifestyle", Guid.NewGuid(), "Front", [black.Id], [Guid.NewGuid(), Guid.NewGuid()], "front-black", 3, false);

        var card = MockupTemplateCardViewModel.From(summary, [black]);

        Assert.Equal("Black", card.ColorSummary);
        Assert.Equal("2 compatible Variants", card.VariantSummary);
        Assert.Equal("Revision 3 · Draft", card.RevisionSummary);
    }

    [Theory]
    [InlineData(OptionKind.Color)]
    [InlineData(OptionKind.Size)]
    [InlineData(OptionKind.Other)]
    public void ChoiceGroupOverflowNameIdentifiesTheOptionKind(OptionKind kind)
    {
        var group = new OfferingChoiceGroupViewModel(
            new OfferingOption(Guid.NewGuid(), Guid.NewGuid(), kind, "Shade", 0),
            [],
            new StubCommand());

        Assert.Equal($"More actions for Shade", group.AccessibleOverflowName);
        Assert.StartsWith("Catalog.OptionOverflow.", group.OverflowAutomationId);
        Assert.Equal("Shade", group.Name);
    }

    private sealed class StubCommand : System.Windows.Input.ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) { }
    }
}
