using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;

namespace FusionCanvas.Application.Items;

public sealed record ItemInspectorState(
    Guid Id,
    string Title,
    string? Description,
    ItemInspectorCreativeFields Creative,
    string? Notes,
    ItemStatus Status,
    WorkflowStage Stage,
    bool IsArchived,
    bool IsEffectivelyActive,
    string DisplayPath,
    IReadOnlyList<ItemInspectorTagEntry> Tags,
    IReadOnlyList<ItemInspectorAssetEntry> Assets,
    IReadOnlyList<string> AvailableTagNames,
    DateTimeOffset UpdatedAt)
{
    public bool IsReadOnly => !IsEffectivelyActive;
}
