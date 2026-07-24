namespace FusionCanvas.Application.ToolContexts;

public sealed record ToolContextInheritedValue(
    string Key,
    string Value,
    ToolContextEntityReference Source,
    bool IsInherited)
{
    public string Key { get; } = string.IsNullOrWhiteSpace(Key)
        ? throw new ArgumentException("Key is required.", nameof(Key))
        : Key;

    public string Value { get; } = Value ?? throw new ArgumentNullException(nameof(Value));
}
