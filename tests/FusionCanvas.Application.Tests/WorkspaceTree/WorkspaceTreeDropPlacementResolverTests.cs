using FusionCanvas.Application.Groups;
using FusionCanvas.Application.WorkspaceTree;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests;

public sealed class WorkspaceTreeDropPlacementResolverTests
{
    [Theory]
    [InlineData(0.14, GroupPlacementKind.Before)]
    [InlineData(0.50, GroupPlacementKind.Append)]
    [InlineData(0.86, GroupPlacementKind.After)]
    public void Resolve_UsesRowEdgeThresholds(double relativePosition, GroupPlacementKind expectedKind)
    {
        var targetId = Guid.NewGuid();

        var placement = WorkspaceTreeDropPlacementResolver.Resolve(
            WorkspaceEntityKind.Group,
            targetId,
            relativePosition);

        Assert.Equal(expectedKind, placement.Kind);
        if (expectedKind == GroupPlacementKind.Append)
        {
            Assert.Null(placement.RelativeGroupId);
        }
        else
        {
            Assert.Equal(targetId, placement.RelativeGroupId);
        }
    }

    [Fact]
    public void Resolve_ForNicheAlwaysAppends()
    {
        var placement = WorkspaceTreeDropPlacementResolver.Resolve(
            WorkspaceEntityKind.Niche,
            Guid.NewGuid(),
            0.01);

        Assert.Equal(GroupPlacementKind.Append, placement.Kind);
    }
}
