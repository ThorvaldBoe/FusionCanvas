namespace FusionCanvas.Domain.Products;

/// <summary>
/// Joins an Item to a DesignArea, representing a selected design target for the
/// Item at the Design stage. Selection validates that the Item and area share a
/// Store and that the Item is editable at Design.
/// </summary>
public sealed record ItemDesignAreaTarget
{
    public ItemDesignAreaTarget(Guid itemId, Guid designAreaId)
    {
        ItemId = ProductRecordValidation.RequireId(itemId, nameof(itemId));
        DesignAreaId = ProductRecordValidation.RequireId(designAreaId, nameof(designAreaId));
    }

    public Guid ItemId { get; init; }

    public Guid DesignAreaId { get; init; }
}
