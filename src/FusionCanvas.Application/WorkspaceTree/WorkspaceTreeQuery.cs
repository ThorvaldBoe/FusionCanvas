using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Navigation;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;

namespace FusionCanvas.Application.WorkspaceTree;

public sealed record WorkspaceTreeQuery(
    string? Text = null,
    IReadOnlySet<WorkspaceEntityKind>? EntityKinds = null,
    IReadOnlySet<ItemStatus>? ItemStatuses = null,
    IReadOnlySet<WorkflowStage>? WorkflowStages = null,
    IReadOnlySet<Guid>? TagIds = null,
    NavigationTopicReference? ScopeTopic = null,
    bool IncludeArchived = false)
{
    public bool IsActive =>
        !string.IsNullOrWhiteSpace(Text) ||
        EntityKinds is { Count: > 0 } ||
        ItemStatuses is { Count: > 0 } ||
        WorkflowStages is { Count: > 0 } ||
        TagIds is { Count: > 0 } ||
        ScopeTopic is not null ||
        IncludeArchived;
}
