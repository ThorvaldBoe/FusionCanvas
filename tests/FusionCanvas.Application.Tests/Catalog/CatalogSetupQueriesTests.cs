using FusionCanvas.Application.Catalog;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Application.Tests;

public sealed class CatalogSetupQueriesTests
{
    [Fact]
    public void ActiveQueries_FilterArchivedRecordsToTheSelectedOffering()
    {
        var offeringId = Guid.NewGuid();
        var otherOfferingId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var activeOption = new OfferingOption(Guid.NewGuid(), offeringId, OptionKind.Color, "Color", 0);
        var archivedOption = activeOption with { Id = Guid.NewGuid(), IsArchived = true };
        var otherOption = activeOption with { Id = Guid.NewGuid(), OfferingId = otherOfferingId };
        var activeValue = new OfferingOptionValue(Guid.NewGuid(), activeOption.Id, offeringId, "Black", 0);
        var archivedValue = activeValue with { Id = Guid.NewGuid(), IsArchived = true };
        var activeVariant = new OfferingVariant(Guid.NewGuid(), offeringId, "Black", [activeValue.Id], false, now, now);
        var archivedVariant = activeVariant with { Id = Guid.NewGuid(), IsArchived = true };
        var activeArea = new OfferingPlaceholder(Guid.NewGuid(), offeringId, "Front", null, "front", "flat", 100, 100, [], false, now, now);
        var archivedArea = activeArea with { Id = Guid.NewGuid(), IsArchived = true };
        var activeTemplate = new MockupTemplate(Guid.NewGuid(), offeringId, activeArea.Id, "Front", null, 1, false, now, now);
        var archivedTemplate = activeTemplate with { Id = Guid.NewGuid(), IsArchived = true };

        Assert.Single(CatalogSetupQueries.ActiveOptions([activeOption, archivedOption, otherOption], offeringId));
        Assert.Single(CatalogSetupQueries.ActiveValues([activeValue, archivedValue], offeringId, activeOption.Id));
        Assert.Single(CatalogSetupQueries.ActiveVariants([activeVariant, archivedVariant], offeringId));
        Assert.Single(CatalogSetupQueries.ActiveDesignAreas([activeArea, archivedArea], offeringId));
        Assert.Single(CatalogSetupQueries.ActiveTemplates([activeTemplate, archivedTemplate], offeringId));
        Assert.Single(CatalogSetupQueries.ActiveColors([activeValue], [activeOption], offeringId));
    }

    [Fact]
    public void CountSetup_ReportsCompletionOnlyWhenEachActiveCollectionIsPresent()
    {
        var offeringId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var option = new OfferingOption(Guid.NewGuid(), offeringId, OptionKind.Color, "Color", 0);
        var optionValue = new OfferingOptionValue(Guid.NewGuid(), option.Id, offeringId, "Black", 0);
        var variant = new OfferingVariant(Guid.NewGuid(), offeringId, "Variant", [optionValue.Id], false, now, now);
        var area = new OfferingPlaceholder(Guid.NewGuid(), offeringId, "Area", null, "area", "flat", 100, 100, [], false, now, now);
        var template = new MockupTemplate(Guid.NewGuid(), offeringId, area.Id, "Template", null, 1, false, now, now);

        var complete = CatalogSetupQueries.CountSetup([variant], [area], [template], offeringId);
        var incomplete = CatalogSetupQueries.CountSetup([], [area], [template], offeringId);

        Assert.True(complete.VariantsComplete && complete.DesignAreasComplete && complete.MockupTemplatesComplete);
        Assert.False(incomplete.VariantsComplete);
    }
}
