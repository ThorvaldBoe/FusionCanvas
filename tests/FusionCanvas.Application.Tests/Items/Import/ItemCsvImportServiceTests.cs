using System.Text.Json;
using FusionCanvas.Application.ToolContexts;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Tags;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.Items.Import;

namespace FusionCanvas.Application.Tests.Items.Import;

public sealed class ItemCsvImportServiceTests
{
    [Fact]
    public async Task ImportIntoNiche_CreatesTopLevelDraftItems()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = sample.Service(repository, nextId: () => new Guid("11111111-1111-1111-1111-111111111111"));
        var request = new ItemCsvImportRequest(
            new ItemTopicReference(WorkspaceEntityKind.Niche, sample.Niche.Id),
            [Row("Alpha", "A1"), Row("Beta", "B1")]);

        var result = await service.ImportAsync(request, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.ImportedCount);
        var persisted = repository.Snapshot.Items.Where(item => item.Name is "Alpha" or "Beta").ToArray();
        Assert.Equal(2, persisted.Length);
        Assert.All(persisted, item =>
        {
            Assert.Equal(sample.Store.Id, item.StoreId);
            Assert.Equal(sample.Niche.Id, item.NicheId);
            Assert.Null(item.GroupId);
            Assert.Equal(ItemStatus.Draft, item.Status);
            Assert.Equal(WorkflowStage.Idea, item.Stage);
        });
    }

    [Fact]
    public async Task ImportIntoGroup_CreatesItemsInThatGroup()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = sample.Service(repository);
        var request = new ItemCsvImportRequest(
            new ItemTopicReference(WorkspaceEntityKind.Group, sample.Child.Id),
            [Row("Gamma")]);

        var result = await service.ImportAsync(request, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var item = repository.Snapshot.Items.Single(candidate => candidate.Name == "Gamma");
        Assert.Equal(sample.Child.Id, item.GroupId);
        Assert.Equal(sample.Niche.Id, item.NicheId);
        Assert.Equal(sample.Store.Id, item.StoreId);
    }

    [Fact]
    public async Task Import_ChoosesConceptStageWhenConceptFieldsPresentElseIdea()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = sample.Service(repository);
        var request = new ItemCsvImportRequest(
            new ItemTopicReference(WorkspaceEntityKind.Niche, sample.Niche.Id),
            [
                Row("Only base", baseIdea: "Idea"),
                Row("Concept idea", conceptIdea: "Concept"),
                Row("Phrase only", phrase: "Phrase"),
                Row("Graphic only", graphic: "Graphic"),
                Row("Empty creative fields")
            ]);

        var result = await service.ImportAsync(request, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var stages = repository.Snapshot.Items
            .Where(item => item.Name is not "Idea")
            .ToDictionary(item => item.Name, item => item.Stage);
        Assert.Equal(WorkflowStage.Idea, stages["Only base"]);
        Assert.Equal(WorkflowStage.Concept, stages["Concept idea"]);
        Assert.Equal(WorkflowStage.Concept, stages["Phrase only"]);
        Assert.Equal(WorkflowStage.Concept, stages["Graphic only"]);
        Assert.Equal(WorkflowStage.Idea, stages["Empty creative fields"]);
    }

    [Fact]
    public async Task Import_WritesMetadataKeysFromColumns()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = sample.Service(repository);
        var request = new ItemCsvImportRequest(
            new ItemTopicReference(WorkspaceEntityKind.Niche, sample.Niche.Id),
            [Row("Tee", "Base", "Concept", "Phrase", "Graphic", "Notes", "tag1", "tag2")]);

        var result = await service.ImportAsync(request, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var item = repository.Snapshot.Items.Single(candidate => candidate.Name == "Tee");
        var metadata = ParseMetadata(item.MetadataJson);
        Assert.Equal("Base", metadata["idea"]);
        Assert.Equal("Concept", metadata["concept.idea"]);
        Assert.Equal("Phrase", metadata["phrase"]);
        Assert.Equal("Graphic", metadata["graphicDirection"]);
        Assert.Equal("Notes", metadata["notes"]);
        Assert.Equal(2, repository.Snapshot.ItemTags.Count(link => link.ItemId == item.Id));
    }

    [Fact]
    public async Task Import_NormalizesPhraseToSingleLineAndTrimsOptionalFields()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = sample.Service(repository);
        var request = new ItemCsvImportRequest(
            new ItemTopicReference(WorkspaceEntityKind.Niche, sample.Niche.Id),
            [Row("Tee", phrase: "First\r\nsecond   ", notes: "  notes  ")]);

        var result = await service.ImportAsync(request, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var item = repository.Snapshot.Items.Single(candidate => candidate.Name == "Tee");
        var metadata = ParseMetadata(item.MetadataJson);
        Assert.Equal("First second", metadata["phrase"]);
        Assert.Equal("notes", metadata["notes"]);
    }

    [Fact]
    public async Task Import_AppliesInheritedMetadataForNonCsvKeysAndCsvOverrides()
    {
        var sample = Sample.Create();
        var inheritedNiche = sample.Niche with
        {
            MetadataJson = "{\"idea.audience\":\"retro\",\"idea\":\"inherited\"}"
        };
        var repository = new TestRepository(sample.Snapshot with { Niches = [inheritedNiche] });
        var service = sample.Service(repository);
        var request = new ItemCsvImportRequest(
            new ItemTopicReference(WorkspaceEntityKind.Niche, sample.Niche.Id),
            [Row("Tee", baseIdea: "csv")]);

        var result = await service.ImportAsync(request, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var item = repository.Snapshot.Items.Single(candidate => candidate.Name == "Tee");
        var metadata = ParseMetadata(item.MetadataJson);
        Assert.Equal("csv", metadata["idea"]);
        Assert.Equal("retro", metadata["idea.audience"]);
        Assert.False(metadata.ContainsKey("inheritedFrom:idea"));
        Assert.True(metadata.ContainsKey("inheritedFrom:idea.audience"));
    }

    [Fact]
    public async Task Import_MergesInheritedTagsWithCsvTagsAndDeduplicates()
    {
        var sample = Sample.Create();
        var source = new ToolContextEntityReference(WorkspaceEntityKind.Niche, sample.Niche.Id, sample.Niche.Name);
        var inheritedTags = new[] { new ToolContextInheritedValue("tag", sample.Tag.Name, source, true) };
        var resolver = new StubContextResolver(inheritedTags: inheritedTags);
        var repository = new TestRepository(sample.Snapshot);
        var service = sample.Service(repository, contextResolver: resolver);
        var request = new ItemCsvImportRequest(
            new ItemTopicReference(WorkspaceEntityKind.Niche, sample.Niche.Id),
            [Row("Tee", tags: [sample.Tag.Name, "fresh"])]);

        var result = await service.ImportAsync(request, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var item = repository.Snapshot.Items.Single(candidate => candidate.Name == "Tee");
        var linked = repository.Snapshot.ItemTags
            .Where(link => link.ItemId == item.Id)
            .Select(link => repository.Snapshot.Tags.Single(tag => tag.Id == link.TagId))
            .Select(tag => tag.Name)
            .ToArray();
        Assert.Equal([sample.Tag.Name, "fresh"], linked);
    }

    [Fact]
    public async Task Import_ArchivedTagNameCreatesNewActiveTag()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = sample.Service(repository);
        var request = new ItemCsvImportRequest(
            new ItemTopicReference(WorkspaceEntityKind.Niche, sample.Niche.Id),
            [Row("Tee", tags: [sample.ArchivedTag.Name])]);

        var result = await service.ImportAsync(request, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var archived = repository.Snapshot.Tags.Single(tag => tag.Id == sample.ArchivedTag.Id);
        Assert.True(archived.IsArchived);
        var active = repository.Snapshot.Tags.Single(tag => tag.Name == sample.ArchivedTag.Name && !tag.IsArchived);
        Assert.NotEqual(sample.ArchivedTag.Id, active.Id);
        var item = repository.Snapshot.Items.Single(candidate => candidate.Name == "Tee");
        Assert.Equal(active.Id, Assert.Single(repository.Snapshot.ItemTags.Where(link => link.ItemId == item.Id)).TagId);
    }

    [Fact]
    public async Task Import_ValidTagsAreLinkedToCreatedItems()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = sample.Service(repository);
        var request = new ItemCsvImportRequest(
            new ItemTopicReference(WorkspaceEntityKind.Niche, sample.Niche.Id),
            [Row("Tee", tags: ["one", "two"])]);

        var result = await service.ImportAsync(request, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var item = repository.Snapshot.Items.Single(candidate => candidate.Name == "Tee");
        var names = repository.Snapshot.ItemTags
            .Where(link => link.ItemId == item.Id)
            .Select(link => repository.Snapshot.Tags.Single(tag => tag.Id == link.TagId).Name)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Assert.Equal(["one", "two"], names);
    }

    [Fact]
    public async Task Import_DuplicateTitlesAreBothImported()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = sample.Service(repository);
        var request = new ItemCsvImportRequest(
            new ItemTopicReference(WorkspaceEntityKind.Niche, sample.Niche.Id),
            [Row("Same"), Row("Same")]);

        var result = await service.ImportAsync(request, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(2, repository.Snapshot.Items.Count(item => item.Name == "Same"));
    }

    [Fact]
    public async Task Import_SavesSnapshotOnceForMultipleRows()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = sample.Service(repository);
        var request = new ItemCsvImportRequest(
            new ItemTopicReference(WorkspaceEntityKind.Niche, sample.Niche.Id),
            [Row("A"), Row("B"), Row("C")]);

        await service.ImportAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task Import_SaveFailureReturnsFailureAndLeavesSnapshot()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot) { FailSaves = true };
        var service = sample.Service(repository);
        var request = new ItemCsvImportRequest(
            new ItemTopicReference(WorkspaceEntityKind.Niche, sample.Niche.Id),
            [Row("A")]);

        var result = await service.ImportAsync(request, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(0, result.ImportedCount);
        Assert.NotEmpty(result.Errors);
        Assert.Empty(repository.Snapshot.Items.Where(item => item.Name == "A"));
    }

    [Fact]
    public async Task Import_ArchivedNicheTargetFails()
    {
        var sample = Sample.Create();
        var archivedNiche = sample.Niche with { IsArchived = true };
        var repository = new TestRepository(sample.Snapshot with { Niches = [archivedNiche] });
        var service = sample.Service(repository);
        var request = new ItemCsvImportRequest(
            new ItemTopicReference(WorkspaceEntityKind.Niche, archivedNiche.Id),
            [Row("A")]);

        var result = await service.ImportAsync(request, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(0, repository.SaveCount);
        Assert.Empty(repository.Snapshot.Items.Where(item => item.Name == "A"));
    }

    [Fact]
    public async Task Import_ArchivedGroupTargetFails()
    {
        var sample = Sample.Create();
        var archived = sample.Child with { IsArchived = true };
        var repository = new TestRepository(sample.Snapshot with { Groups = [sample.Root, archived] });
        var service = sample.Service(repository);
        var request = new ItemCsvImportRequest(
            new ItemTopicReference(WorkspaceEntityKind.Group, archived.Id),
            [Row("A")]);

        var result = await service.ImportAsync(request, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(0, repository.SaveCount);
    }

    private static ItemCsvRow Row(
        string title,
        string? baseIdea = null,
        string? conceptIdea = null,
        string? phrase = null,
        string? graphic = null,
        string? notes = null,
        params string[] tags) =>
        new(title, baseIdea, conceptIdea, phrase, graphic, notes, tags, 1);

    private static Dictionary<string, string> ParseMetadata(string metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson) || metadataJson.Trim() == "{}")
        {
            return [];
        }

        using var document = JsonDocument.Parse(metadataJson);
        return document.RootElement
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.ToString());
    }

    private sealed class TestRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = snapshot;
        public int SaveCount { get; private set; }
        public bool FailSaves { get; init; }
        public void Set(WorkspaceSnapshot value) => Snapshot = value;
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public Task SaveAsync(WorkspaceSnapshot value, CancellationToken cancellationToken = default)
        {
            if (FailSaves)
            {
                throw new IOException("save failed");
            }

            Snapshot = value;
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed record Sample(
        WorkspaceSnapshot Snapshot,
        DateTimeOffset Now,
        Store Store,
        Niche Niche,
        TopicGroup Root,
        TopicGroup Child,
        Item Item,
        Tag Tag,
        Tag ArchivedTag)
    {
        public ItemCsvImportService Service(
            TestRepository repository,
            IToolContextResolver? contextResolver = null,
            Func<Guid>? nextId = null) =>
            new(repository, contextResolver, () => Now.AddMinutes(1), nextId ?? Guid.NewGuid);

        public static Sample Create()
        {
            var now = DateTimeOffset.UtcNow;
            var nicheId = Guid.NewGuid();
            var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
            var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
            var root = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Root", null, false, now, now, "{}");
            var child = new TopicGroup(Guid.NewGuid(), store.Id, null, root.Id, "Child", null, false, now, now, "{}");
            var listing = new Item(Guid.NewGuid(), store.Id, niche.Id, child.Id, "Idea", "Description", ItemStatus.Draft, WorkflowStage.Design, false, now, now, "{}");
            var tag = new Tag(Guid.NewGuid(), store.Id, "Tag", null, false, now, now, "{}");
            var archived = new Tag(Guid.NewGuid(), store.Id, "Old", null, true, now, now, "{}");
            var snapshot = new WorkspaceSnapshot(
                [store], [niche], [root, child], [listing], [], [], [tag, archived], [], []);
            return new(snapshot, now, store, niche, root, child, listing, tag, archived);
        }
    }

    private sealed class StubContextResolver : IToolContextResolver
    {
        private readonly IReadOnlyList<ToolContextInheritedValue> _inheritedMetadata;
        private readonly IReadOnlyList<ToolContextInheritedValue> _inheritedTags;

        public StubContextResolver(
            IReadOnlyList<ToolContextInheritedValue>? inheritedMetadata = null,
            IReadOnlyList<ToolContextInheritedValue>? inheritedTags = null)
        {
            _inheritedMetadata = inheritedMetadata ?? [];
            _inheritedTags = inheritedTags ?? [];
        }

        public ToolContextResolution Resolve(ToolContextResolveRequest request)
        {
            var scope = new ToolContextScopeSummary(ToolContextScopeKind.CurrentTopic, "Topic", "Topic", true);
            var store = new Store(Guid.NewGuid(), "Store", null, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}", Guid.NewGuid());
            return new ToolContextResolution(true, null, scope, new ToolContext(
                store,
                null,
                [],
                null,
                null,
                null,
                ToolContextScopeKind.CurrentTopic,
                scope,
                _inheritedMetadata,
                [],
                _inheritedTags,
                [],
                []));
        }

        public ToolContextCreationDefaults ResolveCreationDefaults(ToolContextResolution resolution) =>
            new(Guid.NewGuid(), null, null, ToolContextScopeKind.CurrentTopic, _inheritedMetadata, _inheritedTags);

        public ToolContextResolution ResolveScope(ToolContextResolveRequest request, ToolContextScopeKind scope) => Resolve(request);
    }
}
