using FusionCanvas.UiDescription.Model;

namespace FusionCanvas.UiDescription.Diagnostics;

public enum UiDiagnosticSeverity
{
    Error
}

public sealed record UiDiagnostic(
    string Code,
    UiDiagnosticSeverity Severity,
    UiSourceLocation Location,
    string Message,
    string? Subject = null) : IComparable<UiDiagnostic>
{
    public int CompareTo(UiDiagnostic? other)
    {
        if (other is null)
        {
            return 1;
        }

        var path = string.Compare(Location.Path, other.Location.Path, StringComparison.Ordinal);
        if (path != 0)
        {
            return path;
        }

        var line = Location.Line.CompareTo(other.Location.Line);
        if (line != 0)
        {
            return line;
        }

        var column = Location.Column.CompareTo(other.Location.Column);
        return column != 0 ? column : string.Compare(Code, other.Code, StringComparison.Ordinal);
    }

    public override string ToString()
    {
        var location = Location.Line > 0
            ? $"{Location.Path}({Location.Line},{Location.Column})"
            : Location.Path;
        var subject = Subject is null ? string.Empty : $" [{Subject}]";
        return $"{location}: {Severity.ToString().ToLowerInvariant()} {Code}{subject}: {Message}";
    }
}
