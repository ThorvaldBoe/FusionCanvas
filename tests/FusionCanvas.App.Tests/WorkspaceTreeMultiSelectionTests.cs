using FusionCanvas.App.Navigation;
using FusionCanvas.Application.WorkspaceTree;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.App.Tests;

public sealed class WorkspaceTreeMultiSelectionTests
{
    [Fact]
    public void ReplaceToggleAndRangeSelectionFollowDesktopSemantics()
    {
        var ids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();
        var selection = new WorkspaceTreeMultiSelection();

        selection.Replace(ids[1]);
        selection.Toggle(ids[3]);
        selection.SelectRange(ids, ids[4], extend: false);

        Assert.Equal([ids[1], ids[2], ids[3], ids[4]], selection.SelectedIds);
        Assert.Equal(ids[4], selection.ActiveId);
        Assert.Equal(ids[1], selection.AnchorId);

        selection.SelectRange(ids, ids[0], extend: true);
        Assert.Equal([ids[1], ids[2], ids[3], ids[4], ids[0]], selection.SelectedIds);
    }

    [Fact]
    public void SelectAllAndReconcileUseVisibleStableIds()
    {
        var visible = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var hidden = Guid.NewGuid();
        var selection = new WorkspaceTreeMultiSelection();
        selection.SelectAll(visible);
        selection.Toggle(hidden);
        selection.Reconcile(visible);

        Assert.Equal(visible, selection.SelectedIds);
        Assert.DoesNotContain(hidden, selection.SelectedIds);
    }

    [Fact]
    public void NormalizeSelectionRemovesItemsInsideSelectedGroup()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Niche", null, false, now, now, "{}");
        var group = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Group", null, false, now, now, "{}");
        var item = new Item(Guid.NewGuid(), store.Id, niche.Id, group.Id, "Item", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        var snapshot = new WorkspaceSnapshot([store], [niche], [group], [item], [], [], [], [], []);

        var normalized = WorkspaceTreeSelectionNormalizer.Normalize(snapshot,
        [
            new WorkspaceTreeSelection(WorkspaceEntityKind.Group, group.Id),
            new WorkspaceTreeSelection(WorkspaceEntityKind.Item, item.Id)
        ]);

        Assert.Equal([new WorkspaceTreeSelection(WorkspaceEntityKind.Group, group.Id)], normalized);
    }
}
