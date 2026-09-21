using FusionCanvas.Application.Groups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.WorkspaceTree;

public static class WorkspaceTreeDropPlacementResolver
{
    public static GroupPlacement Resolve(
        WorkspaceEntityKind targetKind,
        Guid targetId,
        double relativePosition)
    {
        if (targetKind != WorkspaceEntityKind.Group)
        {
            return new GroupPlacement();
        }

        return relativePosition switch
        {
            < 0.15 => new GroupPlacement(GroupPlacementKind.Before, targetId),
            > 0.85 => new GroupPlacement(GroupPlacementKind.After, targetId),
            _ => new GroupPlacement()
        };
    }
}
