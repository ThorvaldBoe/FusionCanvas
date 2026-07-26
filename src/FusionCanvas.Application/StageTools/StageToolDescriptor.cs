using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.Application.StageTools;

public sealed record StageToolDescriptor(
    string Id,
    string DisplayName,
    string Description,
    string DetailViewKey,
    IReadOnlyCollection<WorkflowStage> SupportedStages,
    bool RequiresSelectedItem,
    bool IsDefault,
    StageToolSourceKind SourceKind = StageToolSourceKind.BuiltIn,
    bool IsEnabled = true,
    string? FailureMessage = null)
{
    public string Id { get; } = RequireText(Id, nameof(Id));

    public string DisplayName { get; } = RequireText(DisplayName, nameof(DisplayName));

    public string Description { get; } = RequireText(Description, nameof(Description));

    public string DetailViewKey { get; } = RequireText(DetailViewKey, nameof(DetailViewKey));

    public IReadOnlyCollection<WorkflowStage> SupportedStages { get; } =
        SupportedStages.Count > 0
            ? SupportedStages.ToArray()
            : throw new ArgumentException("At least one workflow stage is required.", nameof(SupportedStages));

    private static string RequireText(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", parameterName)
            : value;
}
