namespace FusionCanvas.Domain.Products;

/// <summary>
/// Anchors an Item at the Design stage to a single listing configuration
/// (a catalog offering). The offering must belong to the Item's Store,
/// be active, and its design areas define the final-design slot grid columns.
/// A dedicated record enforces singular anchoring and referential integrity.
/// </summary>
public sealed record ItemListingConfiguration
{
    public ItemListingConfiguration(Guid itemId, Guid offeringId)
    {
        ItemId = ProductRecordValidation.RequireId(itemId, nameof(itemId));
        OfferingId = ProductRecordValidation.RequireId(offeringId, nameof(offeringId));
    }

    public Guid ItemId { get; init; }

    public Guid OfferingId { get; init; }

    /// <summary>Normalized catalog terminology for the same stable relationship.</summary>
    public Guid BlueprintOfferingId => OfferingId;
}
