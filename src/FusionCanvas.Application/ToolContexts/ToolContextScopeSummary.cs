namespace FusionCanvas.Application.ToolContexts;

public sealed record ToolContextScopeSummary(
    ToolContextScopeKind Kind,
    string Label,
    string Description,
    bool CanRun)
{
    public string Label { get; } = string.IsNullOrWhiteSpace(Label)
        ? throw new ArgumentException("Label is required.", nameof(Label))
        : Label;

    public string Description { get; } = string.IsNullOrWhiteSpace(Description)
        ? throw new ArgumentException("Description is required.", nameof(Description))
        : Description;
}
