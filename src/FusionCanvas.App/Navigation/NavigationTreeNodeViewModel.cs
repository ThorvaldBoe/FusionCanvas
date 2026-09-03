using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Navigation;
using FusionCanvas.Application.Navigation;

namespace FusionCanvas.App.Navigation;

public sealed record NavigationTreeNodeViewModel(
    Guid NodeId,
    NavigationNodeRole Role,
    WorkspaceEntityKind EntityKind,
    Guid EntityId,
    string Name,
    bool IsExpanded,
    bool IsSelected,
    bool IsRevealed,
    IReadOnlyList<NavigationTreeNodeViewModel> Children);
