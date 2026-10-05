using System.Globalization;

namespace FusionCanvas.Application.Updates;

public readonly record struct StableProductVersion(int Major, int Minor, int Build) : IComparable<StableProductVersion>
{
    public static bool TryParse(string? value, out StableProductVersion version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Trim().Split('.', StringSplitOptions.None);
        if (parts.Length != 3
            || !TryParsePart(parts[0], out var major)
            || !TryParsePart(parts[1], out var minor)
            || !TryParsePart(parts[2], out var build))
        {
            return false;
        }

        version = new StableProductVersion(major, minor, build);
        return true;
    }

    public int CompareTo(StableProductVersion other)
    {
        var comparison = Major.CompareTo(other.Major);
        if (comparison != 0) return comparison;

        comparison = Minor.CompareTo(other.Minor);
        return comparison != 0 ? comparison : Build.CompareTo(other.Build);
    }

    public override string ToString() => $"{Major}.{Minor}.{Build}";

    private static bool TryParsePart(string value, out int part) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out part) && part >= 0;
}
