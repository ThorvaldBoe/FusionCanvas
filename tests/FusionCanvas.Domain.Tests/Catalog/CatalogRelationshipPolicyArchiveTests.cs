using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.Domain.Tests.Catalog;

public sealed class CatalogRelationshipPolicyArchiveTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ArchivedOptionDoesNotBlockReplacementOfItsOptionKind()
    {
        var storeId = Guid.NewGuid();
        var blueprint = new Blueprint(Guid.NewGuid(), storeId, "T-shirt", null, false, Now, Now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, storeId, "Tee", null,
            BlueprintOfferingKind.ProviderNetwork, null, "network", null, null, false, Now, Now);
        var archivedColor = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Color, "Old Color", 0, true);
        var activeColor = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Color, "Color", 1, false);

        CatalogRelationshipPolicy.ValidateOffering(offering, blueprint, null,
            [archivedColor, activeColor], [], [], []);
    }

    [Fact]
    public void DuplicateActiveOptionKindsRemainRejected()
    {
        var storeId = Guid.NewGuid();
        var blueprint = new Blueprint(Guid.NewGuid(), storeId, "T-shirt", null, false, Now, Now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, storeId, "Tee", null,
            BlueprintOfferingKind.ProviderNetwork, null, "network", null, null, false, Now, Now);
        var first = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Color, "Color 1", 0);
        var second = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Color, "Color 2", 1);

        Assert.Throws<InvalidOperationException>(() => CatalogRelationshipPolicy.ValidateOffering(offering, blueprint, null,
            [first, second], [], [], []));
    }

    [Fact]
    public void ArchivedOfferingMayRetainAnArchivedPrimaryAreaFromItsOwnOffering()
    {
        var storeId = Guid.NewGuid();
        var blueprint = new Blueprint(Guid.NewGuid(), storeId, "T-shirt", null, false, Now, Now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, storeId, "Tee", null,
            BlueprintOfferingKind.ProviderNetwork, null, "network", null, null, true, Now, Now,
            primaryArtworkDesignAreaId: Guid.NewGuid());
        var area = new OfferingPlaceholder(offering.PrimaryArtworkDesignAreaId!.Value, offering.Id, "Front", null,
            "front", "DTG", 1200, 1400, [], true, Now, Now);

        CatalogRelationshipPolicy.ValidateOffering(offering, blueprint, null, [], [], [], [area]);
    }

    [Fact]
    public void ActiveOfferingStillRequiresAnActivePrimaryArea()
    {
        var storeId = Guid.NewGuid();
        var blueprint = new Blueprint(Guid.NewGuid(), storeId, "T-shirt", null, false, Now, Now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, storeId, "Tee", null,
            BlueprintOfferingKind.ProviderNetwork, null, "network", null, null, false, Now, Now,
            primaryArtworkDesignAreaId: Guid.NewGuid());
        var area = new OfferingPlaceholder(offering.PrimaryArtworkDesignAreaId!.Value, offering.Id, "Front", null,
            "front", "DTG", 1200, 1400, [], true, Now, Now);

        Assert.Throws<InvalidOperationException>(() => CatalogRelationshipPolicy.ValidateOffering(offering, blueprint, null, [], [], [], [area]));
    }
}
