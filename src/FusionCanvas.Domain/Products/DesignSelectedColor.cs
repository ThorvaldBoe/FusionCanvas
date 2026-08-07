namespace FusionCanvas.Domain.Products;

/// <summary>
/// A color value selected for the Item's design working set.
/// Deduplicated by color value across the configuration's variants.
/// Size never participates; color value is the <see cref="VariantOption.Value"/>
/// where <see cref="VariantOption.Name"/> is "Color" (case-insensitive).
/// </summary>
public sealed record DesignSelectedColor
{
    public DesignSelectedColor(Guid itemId, string colorValue)
    {
        ItemId = ProductRecordValidation.RequireId(itemId, nameof(itemId));
        ColorValue = !string.IsNullOrWhiteSpace(colorValue)
            ? colorValue.Trim()
            : throw new ArgumentException("Color value is required.", nameof(colorValue));
    }

    public Guid ItemId { get; init; }

    public string ColorValue { get; init; }
}