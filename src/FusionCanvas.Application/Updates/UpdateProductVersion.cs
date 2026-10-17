using System.Globalization;

namespace FusionCanvas.Application.Updates;

public readonly record struct UpdateProductVersion(int Major, int Minor, int Build, int PreviewNumber = 0, bool IsPreview = false)
    : IComparable<UpdateProductVersion>
{
    public static bool TryParse(string? value, out UpdateProductVersion version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var match = System.Text.RegularExpressions.Regex.Match(value,
            "^(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)(?:-preview\\.(0|[1-9][0-9]*))?$");
        if (!match.Success || !int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var major)
            || !int.TryParse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var minor)
            || !int.TryParse(match.Groups[3].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var build)) return false;
        var preview = match.Groups[4].Success;
        var previewNumber = 0;
        if (preview && (!int.TryParse(match.Groups[4].Value, NumberStyles.None, CultureInfo.InvariantCulture, out previewNumber) || previewNumber < 1)) return false;
        version = new UpdateProductVersion(major, minor, build, previewNumber, preview);
        return true;
    }

    public int CompareTo(UpdateProductVersion other)
    {
        var value = Major.CompareTo(other.Major);
        if (value != 0) return value;
        value = Minor.CompareTo(other.Minor);
        if (value != 0) return value;
        value = Build.CompareTo(other.Build);
        if (value != 0) return value;
        if (IsPreview != other.IsPreview) return IsPreview ? -1 : 1;
        return IsPreview ? PreviewNumber.CompareTo(other.PreviewNumber) : 0;
    }

    public override string ToString() => IsPreview ? $"{Major}.{Minor}.{Build}-preview.{PreviewNumber}" : $"{Major}.{Minor}.{Build}";
}
