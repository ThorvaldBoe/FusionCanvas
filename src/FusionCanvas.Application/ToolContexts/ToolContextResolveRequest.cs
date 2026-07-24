using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.Application.ToolContexts;

public sealed record ToolContextResolveRequest(
    WorkspaceSnapshot Snapshot,
    ToolContextSelectionKind SelectionKind,
    WorkspaceEntityKind SelectedEntityKind,
    Guid SelectedEntityId,
    WorkflowStage? WorkflowStage = null,
    ToolContextScopeKind? ScopeOverride = null,
    bool RequiresSelectedItem = false,
    int NearbyWorkLimit = 8)
{
    public Guid SelectedEntityId { get; } = SelectedEntityId == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(SelectedEntityId))
        : SelectedEntityId;

    public int NearbyWorkLimit { get; } = NearbyWorkLimit > 0
        ? NearbyWorkLimit
        : throw new ArgumentOutOfRangeException(nameof(NearbyWorkLimit), NearbyWorkLimit, "Nearby work limit must be positive.");
}
