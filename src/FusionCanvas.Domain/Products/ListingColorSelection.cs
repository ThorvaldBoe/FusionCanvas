namespace FusionCanvas.Domain.Products;

/// <summary>A per-Item Color selection and its assigned artwork variant.</summary>
public sealed record ListingColorSelection
{
    public ListingColorSelection(
        Guid itemId,
        Guid? colorOptionValueId,
        Guid artworkVariantId,
        int sortOrder,
        string? legacyColorLabel = null)
    {
        ItemId = ProductRecordValidation.RequireId(itemId, nameof(itemId));
        if (colorOptionValueId is null && string.IsNullOrWhiteSpace(legacyColorLabel))
        {
            throw new ArgumentException("A Color Option Value identity or legacy Color label is required.", nameof(colorOptionValueId));
        }

        if (colorOptionValueId is not null && !string.IsNullOrWhiteSpace(legacyColorLabel))
        {
            throw new ArgumentException("A resolved Color selection cannot also retain an unresolved legacy label.", nameof(legacyColorLabel));
        }

        ColorOptionValueId = colorOptionValueId is Guid id
            ? ProductRecordValidation.RequireId(id, nameof(colorOptionValueId))
            : null;
        ArtworkVariantId = ProductRecordValidation.RequireId(artworkVariantId, nameof(artworkVariantId));
        SortOrder = sortOrder >= 0 ? sortOrder : throw new ArgumentOutOfRangeException(nameof(sortOrder));
        LegacyColorLabel = string.IsNullOrWhiteSpace(legacyColorLabel) ? null : legacyColorLabel.Trim();
    }

    public Guid ItemId { get; init; }
    public Guid? ColorOptionValueId { get; init; }
    public Guid ArtworkVariantId { get; init; }
    public int SortOrder { get; init; }
    public string? LegacyColorLabel { get; init; }
    public bool IsResolved => ColorOptionValueId.HasValue;
}
