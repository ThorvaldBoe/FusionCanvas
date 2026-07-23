using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests;

public class ItemInspectorServiceTests
{
    [Fact]
    public async Task Load_BuildsStateFromListingMetadataTagsAndAssets()
    {
        var sample = Sample.Create(withRelationships: true);
        var repository = new TestRepository(sample.Snapshot);
        var service = new ItemInspectorService(repository, clock: () => sample.Now.AddMinutes(2), newId: Guid.NewGuid);

        var state = await service.LoadAsync(sample.Item.Id);

        Assert.NotNull(state);
        Assert.Equal(sample.Item.Name, state!.Title);
        Assert.Equal("Description", state.Description);
        Assert.Equal("Notes", state.Notes);
        Assert.Equal("idea-value", state.Creative.Idea);
        Assert.Equal("audience-value", state.Creative.Audience);
        Assert.Equal("phrase-value", state.Creative.Phrase);
        Assert.Equal("graphic-value", state.Creative.GraphicDirection);
        Assert.Equal(sample.Tag.Name, state.Tags.Single().Name);
        Assert.Equal(AssetKind.SourceDesign, state.Assets.Single().Kind);
        Assert.Contains(sample.Tag.Name, state.AvailableTagNames);
        Assert.Equal("Niche / Root / Child", state.DisplayPath);
        Assert.True(state.IsEffectivelyActive);
        Assert.False(state.IsReadOnly);
    }

    [Fact]
    public async Task Load_ReturnsNullForMissingListing()
    {
        var sample = Sample.Create();
        var service = new ItemInspectorService(new TestRepository(sample.Snapshot));

        var state = await service.LoadAsync(Guid.NewGuid());

        Assert.Null(state);
    }

    [Fact]
    public async Task Load_ReportsEmptyCreativeTagsAndAssetsWithoutFabricating()
    {
        var sample = Sample.Create();
        var listing = sample.Item with { MetadataJson = "{}", Description = null };
        var repository = new TestRepository(sample.Snapshot with
        {
            Items = [listing],
            ItemTags = [],
            AssetLinks = []
        });
        var service = new ItemInspectorService(repository);

        var state = await service.LoadAsync(listing.Id);

        Assert.NotNull(state);
        Assert.Null(state!.Description);
        Assert.Null(state.Notes);
        Assert.Null(state.Creative.Idea);
        Assert.Null(state.Creative.Audience);
        Assert.Null(state.Creative.Phrase);
        Assert.Null(state.Creative.GraphicDirection);
        Assert.Empty(state.Tags);
        Assert.Empty(state.Assets);
        Assert.False(state.Creative.HasAny);
    }

    [Fact]
    public async Task Save_NormalizesFieldsAndPersistsAtomically()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = new ItemInspectorService(repository, clock: () => sample.Now.AddMinutes(3), newId: Guid.NewGuid);

        var result = await service.SaveAsync(new(
            sample.Item.Id,
            "  Renamed Title ",
            "  A short description  ",
            "  A multi\nline idea  ",
            "  Coffee lovers  ",
            "phrase one\nphrase two",
            "  Graphic direction  ",
            "  Revised notes  ",
            []));

        Assert.True(result.Succeeded);
        Assert.Equal("Renamed Title", result.State!.Title);
        Assert.Equal("A short description", result.State.Description);
        Assert.Equal("Revised notes", result.State.Notes);
        Assert.Equal("A multi\nline idea", result.State.Creative.Idea);
        Assert.Equal("Coffee lovers", result.State.Creative.Audience);
        Assert.Equal("phrase one phrase two", result.State.Creative.Phrase);
        Assert.Equal("Graphic direction", result.State.Creative.GraphicDirection);
        var persisted = repository.Snapshot.Items.Single(listing => listing.Id == sample.Item.Id);
        Assert.Equal("Renamed Title", persisted.Name);
        Assert.Equal("A short description", persisted.Description);
        Assert.Contains("\"idea\":\"A multi\\nline idea\"", persisted.MetadataJson);
        Assert.Contains("\"idea.audience\":\"Coffee lovers\"", persisted.MetadataJson);
        Assert.Contains("\"phrase\":\"phrase one phrase two\"", persisted.MetadataJson);
        Assert.Contains("\"graphicDirection\":\"Graphic direction\"", persisted.MetadataJson);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task Save_OmitsAudienceWhenEmptyAndPreservesItAcrossUnrelatedEdits()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = new ItemInspectorService(repository, clock: () => sample.Now.AddMinutes(1), newId: Guid.NewGuid);

        var omitted = await service.SaveAsync(new(
            sample.Item.Id, sample.Item.Name, null, "idea", null, null, null, null, []));

        Assert.True(omitted.Succeeded);
        Assert.DoesNotContain("\"idea.audience\"", repository.Snapshot.Items.Single().MetadataJson);

        var withAudience = await service.SaveAsync(new(
            sample.Item.Id, sample.Item.Name, null, "idea", "Night-shift nurses", null, null, null, []));

        Assert.True(withAudience.Succeeded);
        Assert.Contains("\"idea.audience\":\"Night-shift nurses\"", repository.Snapshot.Items.Single().MetadataJson);

        var unrelatedEdit = await service.SaveAsync(new(
            sample.Item.Id, sample.Item.Name, "desc", "idea", "Night-shift nurses", null, null, "notes", []));

        Assert.True(unrelatedEdit.Succeeded);
        var persisted = repository.Snapshot.Items.Single();
        Assert.Equal("desc", persisted.Description);
        Assert.Contains("\"idea.audience\":\"Night-shift nurses\"", persisted.MetadataJson);
        Assert.Contains("\"unknown\":\"kept\"", persisted.MetadataJson);
        Assert.Equal("Night-shift nurses", unrelatedEdit.State!.Creative.Audience);
    }

    [Fact]
    public async Task Save_PreservesUnknownMetadataAndInheritedProvenance()
    {
        var sample = Sample.Create();
        var listing = sample.Item with
        {
            MetadataJson = "{\"notes\":\"old\",\"idea\":\"old idea\",\"custom\":\"keep\",\"inheritedFrom:custom\":\"Niche:" + sample.Niche.Id + "\"}"
        };
        var repository = new TestRepository(sample.Snapshot with { Items = [listing] });
        var service = new ItemInspectorService(repository, clock: () => sample.Now.AddMinutes(1), newId: Guid.NewGuid);

        var result = await service.SaveAsync(new(
            listing.Id,
            listing.Name,
            Description: null,
            Idea: "new idea",
            Audience: null,
            Phrase: null,
            GraphicDirection: null,
            Notes: "new notes",
            TagNames: []));

        Assert.True(result.Succeeded);
        var persisted = repository.Snapshot.Items.Single(candidate => candidate.Id == listing.Id);
        Assert.Contains("\"custom\":\"keep\"", persisted.MetadataJson);
        Assert.Contains($"\"inheritedFrom:custom\":\"Niche:{sample.Niche.Id}\"", persisted.MetadataJson);
        Assert.DoesNotContain("\"idea\":\"old idea\"", persisted.MetadataJson);
        Assert.Contains("\"idea\":\"new idea\"", persisted.MetadataJson);
        Assert.DoesNotContain("\"phrase\"", persisted.MetadataJson);
        Assert.Contains("\"notes\":\"new notes\"", persisted.MetadataJson);
    }

    [Fact]
    public async Task Save_AllowsBlankTitleButRejectsMultiLineTitleAndKeepsSnapshotUnchanged()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = new ItemInspectorService(repository);

        var blank = await service.SaveAsync(new(sample.Item.Id, "   ", null, null, null, null, null, null, []));
        var multiLine = await service.SaveAsync(new(sample.Item.Id, "two\nlines", null, null, null, null, null, null, []));

        Assert.True(blank.Succeeded);
        Assert.False(multiLine.Succeeded);
        Assert.Equal(string.Empty, repository.Snapshot.Items.Single(item => item.Id == sample.Item.Id).Name);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task Save_LinksExistingTagCaseInsensitivelyWithoutDuplicates()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = new ItemInspectorService(repository, clock: () => sample.Now.AddMinutes(1), newId: Guid.NewGuid);

        var result = await service.SaveAsync(new(sample.Item.Id, sample.Item.Name, null, null, null, null, null, null, ["TAG", "tag"]));

        Assert.True(result.Succeeded);
        var links = repository.Snapshot.ItemTags.Where(link => link.ItemId == sample.Item.Id).ToArray();
        Assert.Single(links);
        Assert.Equal(sample.Tag.Id, links[0].TagId);
        Assert.Single(repository.Snapshot.Tags);
    }

    [Fact]
    public async Task Save_CreatesMissingTagAndLinksIt()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var newTagId = Guid.NewGuid();
        var service = new ItemInspectorService(repository, clock: () => sample.Now.AddMinutes(1), newId: () => newTagId);

        var result = await service.SaveAsync(new(sample.Item.Id, sample.Item.Name, null, null, null, null, null, null, ["New Tag"]));

        Assert.True(result.Succeeded);
        var created = repository.Snapshot.Tags.SingleOrDefault(tag => tag.Name == "New Tag");
        Assert.NotNull(created);
        Assert.Equal(newTagId, created!.Id);
        Assert.Equal(sample.Store.Id, created.StoreId);
        Assert.Contains(repository.Snapshot.ItemTags, link => link.ItemId == sample.Item.Id && link.TagId == newTagId);
    }

    [Fact]
    public async Task Save_RemovesTagLinkButPreservesReusableTag()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = new ItemInspectorService(repository, clock: () => sample.Now.AddMinutes(1), newId: Guid.NewGuid);

        var result = await service.SaveAsync(new(sample.Item.Id, sample.Item.Name, null, null, null, null, null, null, []));

        Assert.True(result.Succeeded);
        Assert.Empty(repository.Snapshot.ItemTags.Where(link => link.ItemId == sample.Item.Id));
        Assert.Contains(repository.Snapshot.Tags, tag => tag.Id == sample.Tag.Id);
    }

    [Fact]
    public async Task Save_RejectsInvalidTagNames()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = new ItemInspectorService(repository);

        var blank = await service.SaveAsync(new(sample.Item.Id, sample.Item.Name, null, null, null, null, null, null, ["   "]));
        var multiLine = await service.SaveAsync(new(sample.Item.Id, sample.Item.Name, null, null, null, null, null, null, ["two\nlines"]));

        Assert.False(blank.Succeeded);
        Assert.False(multiLine.Succeeded);
        Assert.Equal(sample.Snapshot, repository.Snapshot);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task Save_BlocksInactiveListingAndKeepsSnapshotUnchanged()
    {
        var sample = Sample.Create();
        var archived = sample.Item with { IsArchived = true };
        var repository = new TestRepository(sample.Snapshot with { Items = [archived] });
        var service = new ItemInspectorService(repository);

        var result = await service.SaveAsync(new(archived.Id, archived.Name, null, "idea", null, null, null, null, []));

        Assert.False(result.Succeeded);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task Save_RejectsProductContentMutationsForPublishedItem()
    {
        var sample = Sample.Create();
        var published = sample.Item with { Status = ItemStatus.Published, Stage = WorkflowStage.Listing };
        var repository = new TestRepository(sample.Snapshot with { Items = [published] });
        var service = new ItemInspectorService(repository);

        var result = await service.SaveAsync(new(published.Id, published.Name, null, "new idea", null, null, null, null, []));

        Assert.False(result.Succeeded);
        Assert.Contains("Pause", result.Error);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task SaveStageAsync_RejectsStaleStageWrite()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = new ItemInspectorService(repository);

        var staleRequest = new ItemStageAwareSaveRequest(
            sample.Item.Id,
            ExpectedCurrentStage: WorkflowStage.Listing,
            sample.Item.Name,
            Notes: null,
            new ItemStageSavePayload(WorkflowStage.Idea, "idea", null, null, null),
            []);

        var result = await service.SaveStageAsync(staleRequest, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("current stage has changed", result.Error);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task SaveStageAsync_PersistsStagePayloadAndPreservesHiddenAudience()
    {
        var sample = Sample.Create();
        var withAudience = sample.Item with
        {
            MetadataJson = """{"idea.audience":"Coffee fans","unknownKey":"keep"}""",
            Stage = WorkflowStage.Concept
        };
        var repository = new TestRepository(sample.Snapshot with { Items = [withAudience] });
        var service = new ItemInspectorService(repository, clock: () => sample.Now.AddMinutes(1));

        var result = await service.SaveStageAsync(new ItemStageAwareSaveRequest(
            withAudience.Id,
            ExpectedCurrentStage: WorkflowStage.Concept,
            Title: "Updated",
            Notes: "saved notes",
            new ItemStageSavePayload(
                WorkflowStage.Concept,
                Idea: null,
                ConceptIdea: "concept idea",
                Phrase: "  trimmed phrase  ",
                GraphicDirection: "direction"),
            []), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var persisted = repository.Snapshot.Items.Single(item => item.Id == withAudience.Id);
        Assert.Equal("Updated", persisted.Name);
        Assert.Contains("\"concept.idea\":\"concept idea\"", persisted.MetadataJson);
        Assert.Contains("\"phrase\":\"trimmed phrase\"", persisted.MetadataJson);
        Assert.Contains("\"idea.audience\":\"Coffee fans\"", persisted.MetadataJson);
        Assert.Contains("\"unknownKey\":\"keep\"", persisted.MetadataJson);
        Assert.Contains("\"notes\":\"saved notes\"", persisted.MetadataJson);
    }

    [Fact]
    public async Task SaveStageAsync_WritesOnlyTheSelectedStagesMetadata()
    {
        var sample = Sample.Create();
        var item = sample.Item with
        {
            MetadataJson = """
                {
                  "idea":"original idea",
                  "concept.idea":"original concept",
                  "phrase":"original phrase",
                  "graphicDirection":"original direction"
                }
                """,
            Stage = WorkflowStage.Idea
        };
        var repository = new TestRepository(sample.Snapshot with { Items = [item] });
        var service = new ItemInspectorService(repository, clock: () => sample.Now.AddMinutes(1));

        var result = await service.SaveStageAsync(new ItemStageAwareSaveRequest(
            item.Id,
            ExpectedCurrentStage: WorkflowStage.Idea,
            Title: item.Name,
            Notes: null,
            new ItemStageSavePayload(
                WorkflowStage.Idea,
                Idea: "updated idea",
                ConceptIdea: null,
                Phrase: null,
                GraphicDirection: null),
            []), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var persisted = repository.Snapshot.Items.Single(candidate => candidate.Id == item.Id);
        Assert.Contains("\"idea\":\"updated idea\"", persisted.MetadataJson);
        Assert.Contains("\"concept.idea\":\"original concept\"", persisted.MetadataJson);
        Assert.Contains("\"phrase\":\"original phrase\"", persisted.MetadataJson);
        Assert.Contains("\"graphicDirection\":\"original direction\"", persisted.MetadataJson);
    }

    [Fact]
    public async Task Save_FailureLeavesNoPartialChanges()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot) { FailSaves = true };
        var service = new ItemInspectorService(repository, clock: () => sample.Now.AddMinutes(1), newId: Guid.NewGuid);

        var result = await service.SaveAsync(new(sample.Item.Id, sample.Item.Name, null, "idea", null, null, null, null, ["New Tag"]));

        Assert.False(result.Succeeded);
        Assert.Equal(sample.Snapshot, repository.Snapshot);
        Assert.Empty(repository.Snapshot.Tags.Where(tag => tag.Name == "New Tag"));
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task Save_RoundTripsThroughReloadedSnapshot()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = new ItemInspectorService(repository, clock: () => sample.Now.AddMinutes(1), newId: Guid.NewGuid);

        await service.SaveAsync(new(sample.Item.Id, "Updated", "desc", "idea", "audience", "phrase", "graphic", "notes", []));
        var reloaded = await service.LoadAsync(sample.Item.Id);

        Assert.Equal("Updated", reloaded!.Title);
        Assert.Equal("desc", reloaded.Description);
        Assert.Equal("idea", reloaded.Creative.Idea);
        Assert.Equal("audience", reloaded.Creative.Audience);
        Assert.Equal("phrase", reloaded.Creative.Phrase);
        Assert.Equal("graphic", reloaded.Creative.GraphicDirection);
        Assert.Equal("notes", reloaded.Notes);
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
        Asset Asset)
    {
        public static Sample Create(bool withRelationships = false)
        {
            var now = DateTimeOffset.UtcNow;
            var nicheId = Guid.NewGuid();
            var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
            var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
            var root = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Root", null, false, now, now, "{}");
            var child = new TopicGroup(Guid.NewGuid(), store.Id, null, root.Id, "Child", null, false, now, now, "{}");
            var listing = new Item(
                Guid.NewGuid(), store.Id, niche.Id, child.Id, "Idea", "Description", ItemStatus.Draft, WorkflowStage.Design, false, now, now,
                "{\"notes\":\"Notes\",\"idea\":\"idea-value\",\"idea.audience\":\"audience-value\",\"phrase\":\"phrase-value\",\"graphicDirection\":\"graphic-value\",\"unknown\":\"kept\"}");
            var tag = new Tag(Guid.NewGuid(), store.Id, "Tag", null, false, now, now, "{}");
            var asset = new Asset(
                Guid.NewGuid(), store.Id, "Asset", null, AssetKind.SourceDesign, "assets/asset.png", null,
                isMissing: false, isArchived: false, now, now, "{}");
            var snapshot = new WorkspaceSnapshot(
                [store], [niche], [root, child], [listing],
                withRelationships ? [asset] : [],
                [],
                [tag],
                [new ItemTag(listing.Id, tag.Id)],
                withRelationships ? [new AssetLink(asset.Id, WorkspaceEntityKind.Item, listing.Id)] : []);
            return new(snapshot, now, store, niche, root, child, listing, tag, asset);
        }
    }
}
