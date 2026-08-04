namespace FusionCanvas.Domain.Products;

/// <summary>
/// A single named option value within a product variant's ordered option set.
/// Color and size are option values on a variant/offering, never global records.
/// </summary>
public sealed record VariantOption(string Name, string Value)
{
    public string Name { get; } = string.IsNullOrWhiteSpace(Name)
        ? throw new ArgumentException("Option name is required.", nameof(Name))
        : Name;

    public string Value { get; } = string.IsNullOrWhiteSpace(Value)
        ? throw new ArgumentException("Option value is required.", nameof(Value))
        : Value;
}
