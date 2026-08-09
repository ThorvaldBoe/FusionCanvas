namespace FusionCanvas.Domain.Catalog;

internal static class CatalogRecordValidation
{
    public static Guid Id(Guid value, string name) => value == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", name)
        : value;

    public static string Text(string? value, string name) => string.IsNullOrWhiteSpace(value)
        ? throw new ArgumentException("A value is required.", name)
        : value.Trim();

    public static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
