using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Application.ToolContexts;

namespace FusionCanvas.Application.StageTools;

public sealed record StageToolHostRequest(
    WorkspaceSnapshot Snapshot,
    ToolContextSelectionKind SelectionKind,
    WorkspaceEntityKind SelectedEntityKind,
    Guid SelectedEntityId,
    WorkflowStage WorkflowStage,
    string? RequestedToolId = null,
    ToolContextScopeKind? ScopeOverride = null,
    int NearbyWorkLimit = 8)
{
    public Guid SelectedEntityId { get; } = SelectedEntityId == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(SelectedEntityId))
        : SelectedEntityId;
}
