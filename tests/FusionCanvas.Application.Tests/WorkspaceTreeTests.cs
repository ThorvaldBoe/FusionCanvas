using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests;

public class WorkspaceTreeTests
{
    [Fact]
    public void Project_FreeTextMatchIncludesAncestorPathAndDisablesRelativeReorder()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Coffee", null, false, now, now, "{}");
        var root = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Campaign", null, false, now, now, "{}", 0);
        var child = new TopicGroup(Guid.NewGuid(), store.Id, null, root.Id, "Espresso", null, false, now, now, "{}", 0);
        var snapshot = new WorkspaceSnapshot([store], [niche], [root, child], [], [], [], [], [], []);

        var projection = WorkspaceTreeProjector.Project(snapshot, store.Id, new WorkspaceTreeQuery("espresso"));

        var nicheNode = Assert.Single(projection.Roots);
        var rootNode = Assert.Single(nicheNode.Children);
        var childNode = Assert.Single(rootNode.Children);
        Assert.False(nicheNode.IsDirectMatch);
        Assert.False(rootNode.IsDirectMatch);
        Assert.True(childNode.IsDirectMatch);
        Assert.False(projection.CanReorderBetweenSiblings);
        Assert.Contains(child.Id, projection.VisibleEntityIds);
    }

    [Fact]
    public void SelectionCoordinator_ChangesCanonicalSelectionWithoutDocumentState()
    {
        var coordinator = new WorkspaceTreeSelectionCoordinator();
        WorkspaceTreeSelection? observed = null;
        coordinator.SelectionChanged += (_, selection) => observed = selection;
        var selection = new WorkspaceTreeSelection(WorkspaceEntityKind.Group, Guid.NewGuid());

        coordinator.Select(selection);

        Assert.Equal(selection, coordinator.Selected);
        Assert.Equal(selection, observed);
    }

    [Fact]
    public void Project_RepresentativeDeepAndWideTreePreservesStablePaths()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Niche", null, false, now, now, "{}");
        var groups = new List<TopicGroup>();
        Guid? parentId = null;
        for (var depth = 0; depth < 25; depth++)
        {
            var group = new TopicGroup(
                Guid.NewGuid(), store.Id, parentId is null ? niche.Id : null, parentId,
                $"Depth {depth}", null, false, now, now, "{}", depth);
            groups.Add(group);
            parentId = group.Id;
        }

        for (var index = 0; index < 200; index++)
        {
            groups.Add(new TopicGroup(
                Guid.NewGuid(), store.Id, niche.Id, null,
                $"Wide {index:D3}", null, false, now, now, "{}", index + 25));
        }

        var snapshot = new WorkspaceSnapshot([store], [niche], groups, [], [], [], [], [], []);
        var complete = WorkspaceTreeProjector.Project(snapshot, store.Id, new WorkspaceTreeQuery());
        var filtered = WorkspaceTreeProjector.Project(snapshot, store.Id, new WorkspaceTreeQuery("Depth 24"));

        Assert.Equal(225, complete.VisibleEntityIds.Count(id => id != niche.Id));
        Assert.Equal(25, Descendants(Assert.Single(filtered.Roots)).Count(node => node.EntityKind == WorkspaceEntityKind.Group));
        Assert.Equal(parentId, Descendants(Assert.Single(filtered.Roots)).Last().EntityId);
    }

    private static IEnumerable<WorkspaceTreeProjectionNode> Descendants(WorkspaceTreeProjectionNode node)
    {
        yield return node;
        foreach (var child in node.Children.SelectMany(Descendants))
        {
            yield return child;
        }
    }
}
