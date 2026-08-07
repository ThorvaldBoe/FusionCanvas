using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Tags;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Application.Items;

namespace FusionCanvas.Application.Tests;

public class ItemCsvExportServiceTests
{
    private readonly IItemCsvExportService _service = new ItemCsvExportService();

    [Fact]
    public void Project_GroupIncludesGroupAndDescendantItems()
    {
        var sample = Sample.Create();
        var rows = _service.Project(sample.Snapshot, WorkspaceEntityKind.Group, sample.Root.Id);

        Assert.Equal(new[] { sample.ItemInRoot.Name, sample.ItemInChild.Name }, rows.Select(r => r.Title).ToArray());
    }

    [Fact]
    public void Project_GroupExcludesItemsInSiblingGroups()
    {
        var sample = Sample.Create();
        var sibling = new TopicGroup(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Sibling", null, false, sample.Now, sample.Now, "{}");
        var siblingItem = new Item(
            Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, sibling.Id, "Sibling Item", null, ItemStatus.Draft,
            WorkflowStage.Idea, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with
        {
            Groups = [.. sample.Snapshot.Groups, sibling],
            Items = [.. sample.Snapshot.Items, siblingItem]
        };

        var rows = _service.Project(snapshot, WorkspaceEntityKind.Group, sample.Root.Id);

        Assert.DoesNotContain("Sibling Item", rows.Select(r => r.Title));
    }

    [Fact]
    public void Project_NicheIncludesDirectAndGroupItems()
    {
        var sample = Sample.Create();
        var rows = _service.Project(sample.Snapshot, WorkspaceEntityKind.Niche, sample.Niche.Id);

        var titles = rows.Select(r => r.Title).ToArray();
        Assert.Contains(sample.ItemInChild.Name, titles);
        Assert.Contains(sample.ItemInRoot.Name, titles);
        Assert.Contains(sample.DirectNicheItem.Name, titles);
    }

    [Fact]
    public void Project_ZeroItemGroupReturnsEmpty()
    {
        var sample = Sample.Create();
        var emptyGroup = new TopicGroup(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, "Empty", null, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with { Groups = [.. sample.Snapshot.Groups, emptyGroup] };

        var rows = _service.Project(snapshot, WorkspaceEntityKind.Group, emptyGroup.Id);

        Assert.Empty(rows);
    }

    [Fact]
    public void Project_MissingTopicReturnsEmpty()
    {
        var sample = Sample.Create();

        Assert.Empty(_service.Project(sample.Snapshot, WorkspaceEntityKind.Group, Guid.NewGuid()));
        Assert.Empty(_service.Project(sample.Snapshot, WorkspaceEntityKind.Niche, Guid.NewGuid()));
    }

    [Fact]
    public void Project_ExcludesArchivedAndInactiveItems()
    {
        var sample = Sample.Create();
        var archivedItem = sample.ItemInChild with { Name = "Archived", IsArchived = true };
        var archivedSubgroup = new TopicGroup(Guid.NewGuid(), sample.Store.Id, null, sample.Root.Id, "ArchSub", null, true, sample.Now, sample.Now, "{}");
        var underArchived = new Item(
            Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, archivedSubgroup.Id, "Under Archived", null, ItemStatus.Draft,
            WorkflowStage.Idea, false, sample.Now, sample.Now, "{\"idea\":\"x\"}");
        var snapshot = sample.Snapshot with
        {
            Groups = [.. sample.Snapshot.Groups, archivedSubgroup],
            Items = [.. sample.Snapshot.Items, archivedItem, underArchived]
        };

        var rows = _service.Project(snapshot, WorkspaceEntityKind.Group, sample.Root.Id);

        Assert.DoesNotContain("Archived", rows.Select(r => r.Title));
        Assert.DoesNotContain("Under Archived", rows.Select(r => r.Title));
    }

    [Fact]
    public void Project_ExcludesEmptyItems()
    {
        var sample = Sample.Create();
        var emptyItem = new Item(
            Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, sample.Root.Id, "", null, ItemStatus.Draft,
            WorkflowStage.Idea, false, sample.Now, sample.Now, "{}");
        var snapshot = sample.Snapshot with { Items = [.. sample.Snapshot.Items, emptyItem] };

        var rows = _service.Project(snapshot, WorkspaceEntityKind.Group, sample.Root.Id);

        Assert.DoesNotContain("", rows.Select(r => r.Title));
    }

    [Fact]
    public void ProjectSelected_ExportsOnlyRequestedActiveItems()
    {
        var sample = Sample.Create();
        var archived = sample.ItemInRoot with { Id = Guid.NewGuid(), Name = "Archived", IsArchived = true };
        var snapshot = sample.Snapshot with { Items = [.. sample.Snapshot.Items, archived] };

        var rows = _service.ProjectSelected(snapshot, [sample.ItemInChild.Id, archived.Id]);

        Assert.Equal([sample.ItemInChild.Name], rows.Select(row => row.Title));
    }

    [Fact]
    public void Project_ColumnsMapToFields()
    {
        var sample = Sample.Create();
        var row = _service.Project(sample.Snapshot, WorkspaceEntityKind.Group, sample.Child.Id).Single();

        Assert.Equal(sample.ItemInChild.Name, row.Title);
        Assert.Equal("IdeaA", row.BaseIdea);
        Assert.Equal("ConceptA", row.ConceptIdea);
        Assert.Equal("PhraseA", row.Phrase);
        Assert.Equal("GraphicA", row.Graphic);
        Assert.Equal("NotesA", row.Notes);
    }

    [Fact]
    public void Project_TagsJoinedInDeterministicOrder()
    {
        var sample = Sample.Create();
        var row = _service.Project(sample.Snapshot, WorkspaceEntityKind.Group, sample.Child.Id).Single();

        Assert.Equal("Alpha, Zulu", row.Tags);
    }

    [Fact]
    public void Project_MissingFieldsExportAsEmpty()
    {
        var sample = Sample.Create();
        var stripped = sample.ItemInChild with { MetadataJson = "{}" };
        var snapshot = sample.Snapshot with { Items = [stripped], ItemTags = [] };

        var row = _service.Project(snapshot, WorkspaceEntityKind.Group, sample.Child.Id).Single();

        Assert.Equal(stripped.Name, row.Title);
        Assert.Null(row.BaseIdea);
        Assert.Null(row.ConceptIdea);
        Assert.Null(row.Phrase);
        Assert.Null(row.Graphic);
        Assert.Null(row.Notes);
        Assert.Equal(string.Empty, row.Tags);
    }

    [Fact]
    public void Project_ThrowsForNonTopicKind()
    {
        var sample = Sample.Create();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _service.Project(sample.Snapshot, WorkspaceEntityKind.Item, Guid.NewGuid()));
    }

    private sealed record Sample(
        WorkspaceSnapshot Snapshot,
        DateTimeOffset Now,
        Store Store,
        Niche Niche,
        TopicGroup Root,
        TopicGroup Child,
        Item ItemInRoot,
        Item ItemInChild,
        Item DirectNicheItem)
    {
        public static Sample Create()
        {
            var now = DateTimeOffset.UtcNow;
            var nicheId = Guid.NewGuid();
            var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
            var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
            var root = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Root", null, false, now.AddSeconds(1), now, "{}");
            var child = new TopicGroup(Guid.NewGuid(), store.Id, null, root.Id, "Child", null, false, now.AddSeconds(2), now, "{}");

            var itemInRoot = new Item(
                Guid.NewGuid(), store.Id, niche.Id, root.Id, "Root Item", null, ItemStatus.Draft,
                WorkflowStage.Idea, false, now.AddSeconds(3), now, "{\"idea\":\"root idea\"}");
            var itemInChild = new Item(
                Guid.NewGuid(), store.Id, niche.Id, child.Id, "Child Item", null, ItemStatus.Draft,
                WorkflowStage.Concept, false, now.AddSeconds(4), now,
                "{\"notes\":\"NotesA\",\"idea\":\"IdeaA\",\"concept.idea\":\"ConceptA\",\"phrase\":\"PhraseA\",\"graphicDirection\":\"GraphicA\"}");
            var directNicheItem = new Item(
                Guid.NewGuid(), store.Id, niche.Id, null, "Direct Niche Item", null, ItemStatus.Draft,
                WorkflowStage.Idea, false, now.AddSeconds(5), now, "{\"idea\":\"direct\"}");

            var alpha = new Tag(Guid.NewGuid(), store.Id, "Zulu", null, false, now, now, "{}");
            var zulu = new Tag(Guid.NewGuid(), store.Id, "Alpha", null, false, now, now, "{}");

            var snapshot = new WorkspaceSnapshot(
                [store],
                [niche],
                [root, child],
                [itemInRoot, itemInChild, directNicheItem],
                [],
                [],
                [alpha, zulu],
                [new ItemTag(itemInChild.Id, alpha.Id), new ItemTag(itemInChild.Id, zulu.Id)],
                []);

            return new(snapshot, now, store, niche, root, child, itemInRoot, itemInChild, directNicheItem);
        }
    }
}
