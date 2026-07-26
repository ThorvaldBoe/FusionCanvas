using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.WorkspaceTree;

public sealed record WorkspaceTreeProjectionNode(
    Guid NodeId,
    WorkspaceEntityKind EntityKind,
    Guid EntityId,
    string Name,
    bool IsDirectMatch,
    bool HasHiddenChildren,
    IReadOnlyList<WorkspaceTreeProjectionNode> Children,
    bool IsInactive = false);
