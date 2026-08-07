namespace FusionCanvas.Domain.Products;

/// <summary>
/// A row in the final-design slot grid. One row serves a set of colors;
/// exactly one <see cref="IsDefault"/> row exists per item at any time.
/// Rows partition the selected colors: every selected color belongs to
/// exactly one row, and rows may serve multiple colors.
/// </summary>
public sealed record DesignVariantRow
{
    public DesignVariantRow(Guid id, Guid itemId, bool isDefault, int sortOrder)
    {
        Id = ProductRecordValidation.RequireId(id, nameof(id));
        ItemId = ProductRecordValidation.RequireId(itemId, nameof(itemId));
        IsDefault = isDefault;
        SortOrder = sortOrder;
    }

    public Guid Id { get; init; }

    public Guid ItemId { get; init; }

    public bool IsDefault { get; init; }

    public int SortOrder { get; init; }
}