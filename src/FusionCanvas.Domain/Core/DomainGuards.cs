namespace FusionCanvas.Domain.Core;

internal static class DomainGuards
{
    public static Guid RequireId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier must not be empty.", parameterName);
        }

        return value;
    }

    public static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Text must not be empty.", parameterName);
        }

        return value.Trim();
    }

    public static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
