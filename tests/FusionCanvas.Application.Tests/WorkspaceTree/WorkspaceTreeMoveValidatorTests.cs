using FusionCanvas.Application.Groups;
using FusionCanvas.Application.WorkspaceTree;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests;

public sealed class WorkspaceTreeMoveValidatorTests
{
    [Fact]
    public void Validate_AllowsActiveItemWithinItsStore()
    {
        var sample = Sample.Create();

        var result = WorkspaceTreeMoveValidator.Validate(
            sample.Snapshot,
            [new WorkspaceTreeSelection(WorkspaceEntityKind.Item, sample.Item.Id)],
            WorkspaceEntityKind.Group,
            sample.Group.Id,
            new GroupPlacement(),
            isFiltering: false);

        Assert.True(result.IsValid);
        Assert.Equal([new WorkspaceTreeSelection(WorkspaceEntityKind.Item, sample.Item.Id)], result.EffectiveSources);
    }

    [Fact]
    public void Validate_RejectsGroupMoveBeneathItsDescendant()
    {
        var sample = Sample.Create(withChildGroup: true);

        var result = WorkspaceTreeMoveValidator.Validate(
            sample.Snapshot,
            [new WorkspaceTreeSelection(WorkspaceEntityKind.Group, sample.Group.Id)],
            WorkspaceEntityKind.Group,
            sample.ChildGroup!.Id,
            new GroupPlacement(),
            isFiltering: false);

        Assert.False(result.IsValid);
        Assert.Contains("outside the selected hierarchy", result.Error);
    }

    [Fact]
    public void Validate_RejectsRelativeGroupMoveWhileFiltering()
    {
        var sample = Sample.Create();

        var result = WorkspaceTreeMoveValidator.Validate(
            sample.Snapshot,
            [new WorkspaceTreeSelection(WorkspaceEntityKind.Group, sample.Group.Id)],
            WorkspaceEntityKind.Niche,
            sample.Niche.Id,
            new GroupPlacement(GroupPlacementKind.Before, sample.Group.Id),
            isFiltering: true);

        Assert.False(result.IsValid);
        Assert.Contains("Clear filtering", result.Error);
    }

    private sealed record Sample(
        WorkspaceSnapshot Snapshot,
        Store Store,
        Niche Niche,
        TopicGroup Group,
        TopicGroup? ChildGroup,
        Item Item)
    {
        public static Sample Create(bool withChildGroup = false)
        {
            var now = DateTimeOffset.UtcNow;
            var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
            var niche = new Niche(Guid.NewGuid(), store.Id, "Niche", null, false, now, now, "{}");
            var group = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Group", null, false, now, now, "{}");
            var child = withChildGroup
                ? new TopicGroup(Guid.NewGuid(), store.Id, null, group.Id, "Child", null, false, now, now, "{}")
                : null;
            var item = new Item(Guid.NewGuid(), store.Id, niche.Id, group.Id, "Item", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
            return new Sample(
                new WorkspaceSnapshot([store], [niche], child is null ? [group] : [group, child], [item], [], [], [], [], []),
                store,
                niche,
                group,
                child,
                item);
        }
    }
}
