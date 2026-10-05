namespace FusionCanvas.Application.DesignFiles;

public readonly record struct GlobalColorRemovalColor(byte Red, byte Green, byte Blue)
{
    public string ToHex() => $"#{Red:X2}{Green:X2}{Blue:X2}";

    public static bool TryParse(string? value, out GlobalColorRemovalColor color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();
        if (normalized.StartsWith('#'))
        {
            normalized = normalized[1..];
        }

        if (normalized.Length != 6
            || !byte.TryParse(normalized[..2], System.Globalization.NumberStyles.HexNumber, null, out var red)
            || !byte.TryParse(normalized[2..4], System.Globalization.NumberStyles.HexNumber, null, out var green)
            || !byte.TryParse(normalized[4..], System.Globalization.NumberStyles.HexNumber, null, out var blue))
        {
            return false;
        }

        color = new GlobalColorRemovalColor(red, green, blue);
        return true;
    }
}
