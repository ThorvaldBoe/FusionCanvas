using System.Globalization;

namespace FusionCanvas.UiDescription.Model;

public enum UiLengthKind
{
    Content,
    Fill,
    Fixed
}

public readonly record struct UiLength(UiLengthKind Kind, decimal Value = 0)
{
    public static UiLength Content => new(UiLengthKind.Content);

    public static UiLength Fill => new(UiLengthKind.Fill);

    public static UiLength Fixed(decimal value) => new(UiLengthKind.Fixed, value);

    public static bool TryParse(string? value, out UiLength length)
    {
        if (string.Equals(value, "content", StringComparison.OrdinalIgnoreCase))
        {
            length = Content;
            return true;
        }

        if (string.Equals(value, "fill", StringComparison.OrdinalIgnoreCase))
        {
            length = Fill;
            return true;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var fixedValue))
        {
            length = Fixed(fixedValue);
            return true;
        }

        length = default;
        return false;
    }

    public override string ToString() => Kind switch
    {
        UiLengthKind.Content => "content",
        UiLengthKind.Fill => "fill",
        _ => Value.ToString(CultureInfo.InvariantCulture)
    };
}
