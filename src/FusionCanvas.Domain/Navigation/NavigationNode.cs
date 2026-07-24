using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Domain.Navigation;

public sealed record NavigationNode(
    Guid Id,
    NavigationNodeRole Role,
    WorkspaceEntityKind EntityKind,
    Guid EntityId,
    string Name,
    Guid? ParentNodeId,
    IReadOnlyList<NavigationNode> Children,
    bool IsInactive = false);
