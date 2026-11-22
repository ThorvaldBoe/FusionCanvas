namespace FusionCanvas.Domain.Products;

/// <summary>
/// A color-independent artwork variant in the final-design slot grid.
/// Exactly one <see cref="IsDefault"/> row exists per item when variants exist.
/// </summary>
public sealed record DesignVariantRow
{
    public DesignVariantRow(Guid id, Guid itemId, bool isDefault, int sortOrder, ArtworkContrastIntent? contrastIntent = null)
    {
        Id = ProductRecordValidation.RequireId(id, nameof(id));
        ItemId = ProductRecordValidation.RequireId(itemId, nameof(itemId));
        IsDefault = isDefault;
        SortOrder = sortOrder;
        ContrastIntent = contrastIntent;
    }

    public Guid Id { get; init; }

    public Guid ItemId { get; init; }

    public bool IsDefault { get; init; }

    public int SortOrder { get; init; }

    public ArtworkContrastIntent? ContrastIntent { get; init; }
}
