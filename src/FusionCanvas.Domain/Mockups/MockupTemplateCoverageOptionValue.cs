using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.Domain.Mockups;

public sealed record MockupTemplateCoverageOptionValue(Guid Id, OptionKind Kind, string Value)
{
    public Guid Id { get; } = Id == Guid.Empty ? throw new ArgumentException("Identifier must not be empty.", nameof(Id)) : Id;
    public string Value { get; } = string.IsNullOrWhiteSpace(Value) ? throw new ArgumentException("Value must not be blank.", nameof(Value)) : Value;
}
