using FusionCanvas.Application.WorkspaceTree;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Navigation;

/// <summary>
/// Session-only selection state for the workspace tree. The canonical active
/// selection remains owned by <see cref="WorkspaceTreeSelectionCoordinator"/>.
/// </summary>
public static class WorkspaceTreeSelectionNormalizer
{
    public static IReadOnlyList<WorkspaceTreeSelection> Normalize(
        WorkspaceSnapshot snapshot,
        IEnumerable<WorkspaceTreeSelection> selections)
        => WorkspaceTreeSelectionRules.Normalize(snapshot, selections);

    public static bool IsWithinGroup(WorkspaceSnapshot snapshot, Guid entityId, Guid groupId)
        => WorkspaceTreeSelectionRules.IsWithinGroup(snapshot, entityId, groupId);
}
