namespace FusionCanvas.Domain.Products;

/// <summary>
/// Joins a color value to a <see cref="DesignVariantRow"/>.
/// PK is (rowId, colorValue) — one color belongs to exactly one row.
/// </summary>
public sealed record DesignVariantRowColor
{
    public DesignVariantRowColor(Guid rowId, string colorValue)
    {
        RowId = ProductRecordValidation.RequireId(rowId, nameof(rowId));
        ColorValue = !string.IsNullOrWhiteSpace(colorValue)
            ? colorValue.Trim()
            : throw new ArgumentException("Color value is required.", nameof(colorValue));
    }

    public Guid RowId { get; init; }

    public string ColorValue { get; init; }
}