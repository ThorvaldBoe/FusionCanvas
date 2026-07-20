using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace FusionCanvas.App.Navigation;

public static class TagBrushConverter
{
    public sealed class BackgroundConverter : IValueConverter
    {
        public static readonly BackgroundConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var hex = value as string;
            if (string.IsNullOrWhiteSpace(hex) || !TryParseHex(hex!, out var r, out var g, out var b))
            {
                return new SolidColorBrush(Color.FromRgb(0xF0, 0xF2, 0xF5));
            }

            return new SolidColorBrush(Color.FromArgb(0x33, r, g, b));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    public sealed class ForegroundConverter : IValueConverter
    {
        public static readonly ForegroundConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var hex = value as string;
            if (string.IsNullOrWhiteSpace(hex) || !TryParseHex(hex!, out var r, out var g, out var b))
            {
                return new SolidColorBrush(Color.FromRgb(0x24, 0x34, 0x47));
            }

            var luminance = (0.299 * r) + (0.587 * g) + (0.114 * b);
            return new SolidColorBrush(luminance > 140 ? Color.FromRgb(0x1A, 0x22, 0x2E) : Colors.White);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    public sealed class BorderConverter : IValueConverter
    {
        public static readonly BorderConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var hex = value as string;
            if (string.IsNullOrWhiteSpace(hex) || !TryParseHex(hex!, out var r, out var g, out var b))
            {
                return new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
            }

            return new SolidColorBrush(Color.FromArgb(0xCC, r, g, b));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    public static readonly BackgroundConverter Background = BackgroundConverter.Instance;
    public static readonly ForegroundConverter Foreground = ForegroundConverter.Instance;
    public static readonly BorderConverter Border = BorderConverter.Instance;

    private static bool TryParseHex(string hex, out byte r, out byte g, out byte b)
    {
        r = g = b = 0;
        if (!hex.StartsWith('#'))
        {
            return false;
        }

        var body = hex[1..];
        if (body.Length == 3)
        {
            r = ExpandNibble(body[0]);
            g = ExpandNibble(body[1]);
            b = ExpandNibble(body[2]);
            return true;
        }

        if (body.Length == 6 &&
            byte.TryParse(body.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r) &&
            byte.TryParse(body.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g) &&
            byte.TryParse(body.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b))
        {
            return true;
        }

        return false;
    }

    private static byte ExpandNibble(char c)
    {
        var value = (byte)(char.IsDigit(c) ? c - '0' : (char.ToUpperInvariant(c) - 'A') + 10);
        return (byte)(value * 16 + value);
    }
}
