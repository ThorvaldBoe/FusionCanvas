using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Prompts;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Tags;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.WorkspaceTree;

namespace FusionCanvas.Application.Tests;

public class ItemManagementServiceTests
{
    [Fact]
    public async Task ResolveCreateTopic_UsesSelectionListingParentAndDefaultWithoutGuessing()
    {
        var sample = Sample.Create();
        var service = sample.Service();

        var group = await service.ResolveCreateTopicAsync(sample.Store.Id, new WorkspaceTreeSelection(WorkspaceEntityKind.Group, sample.Child.Id));
        var listing = await service.ResolveCreateTopicAsync(sample.Store.Id, new WorkspaceTreeSelection(WorkspaceEntityKind.Item, sample.Item.Id));
        var fallback = await service.ResolveCreateTopicAsync(sample.Store.Id, null);
        var ambiguousStore = sample.Store with { DefaultNicheId = null };
        var ambiguous = await new ItemManagementService(new TestRepository(sample.Snapshot with
        {
            Stores = [ambiguousStore],
            Niches = [.. sample.Snapshot.Niches, new Niche(Guid.NewGuid(), sample.Store.Id, "Other", null, false, sample.Now, sample.Now, "{}")]
        })).ResolveCreateTopicAsync(sample.Store.Id, null);

        Assert.Equal(sample.Child.Id, group.Topic!.Id);
        Assert.Equal(sample.Child.Id, listing.Topic!.Id);
        Assert.Equal(sample.Niche.Id, fallback.Topic!.Id);
        Assert.False(ambiguous.Succeeded);
    }

    [Fact]
    public async Task CreateAndUpdate_NormalizeCoreDetailsMetadataAndTagsAtomically()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var id = Guid.NewGuid();
        var service = sample.Service(repository, id);

        var created = await service.CreateItemAsync(new ItemManagementCreateRequest(
            new ItemTopicReference(WorkspaceEntityKind.Group, sample.Child.Id),
            " Idea ",
            new ItemContext(" Description ", " Notes ", new Dictionary<string, string> { ["unknown"] = "kept" }, [sample.Tag.Id])));
        var updated = await service.UpdateItemAsync(new ItemManagementUpdateRequest(
            id,
            " Renamed ",
            created.Item!.Context with { Description = " Changed ", Notes = " Revised " }));

        Assert.True(created.Succeeded);
        Assert.True(updated.Succeeded);
        Assert.Equal(ItemStatus.Draft, created.Item.Status);
        Assert.Equal("Renamed", updated.Item!.Name);
        Assert.Equal("Changed", updated.Item.Context.Description);
        Assert.Equal("Revised", updated.Item.Context.Notes);
        Assert.Equal("kept", updated.Item.Context.Metadata!["unknown"]);
        Assert.Contains(repository.Snapshot.ItemTags, link => link.ItemId == id && link.TagId == sample.Tag.Id);
        Assert.Equal(2, repository.SaveCount);
    }

    [Fact]
    public async Task InvalidNamesAndSaveFailureLeaveSnapshotUnchanged()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot) { FailSaves = true };
        var service = sample.Service(repository);

        var invalid = await service.CreateItemAsync(new ItemManagementCreateRequest(
            new ItemTopicReference(WorkspaceEntityKind.Niche, sample.Niche.Id), "two\nlines"));
        var failed = await service.CreateItemAsync(new ItemManagementCreateRequest(
            new ItemTopicReference(WorkspaceEntityKind.Niche, sample.Niche.Id), "Valid"));

        Assert.False(invalid.Succeeded);
        Assert.False(failed.Succeeded);
        Assert.Equal(sample.Snapshot, repository.Snapshot);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task Move_RejectsArchivedAncestryAndCrossStoreAndPreservesRelationships()
    {
        var sample = Sample.Create();
        var otherStore = new Store(Guid.NewGuid(), "Other", null, false, sample.Now, sample.Now, "{}");
        var otherNiche = new Niche(Guid.NewGuid(), otherStore.Id, "Other", null, false, sample.Now, sample.Now, "{}");
        var repository = new TestRepository(sample.Snapshot with { Stores = [sample.Store, otherStore], Niches = [sample.Niche, otherNiche] });
        var service = sample.Service(repository);

        var crossStore = await service.MoveItemAsync(new(sample.Item.Id, new(WorkspaceEntityKind.Niche, otherNiche.Id)));
        var inactiveSnapshot = repository.Snapshot with
        {
            Groups = repository.Snapshot.Groups.Select(group => group.Id == sample.Root.Id ? group with { IsArchived = true } : group).ToArray()
        };
        repository.Set(inactiveSnapshot);
        var inactive = await service.MoveItemAsync(new(sample.Item.Id, new(WorkspaceEntityKind.Niche, sample.Niche.Id)));

        Assert.False(crossStore.Succeeded);
        Assert.False(inactive.Succeeded);
        Assert.Equal(sample.Child.Id, repository.Snapshot.Items.Single(listing => listing.Id == sample.Item.Id).GroupId);
        Assert.Single(repository.Snapshot.ItemTags);
    }

    [Fact]
    public async Task Duplicate_CopiesCoreMetadataAndTagsButNotPromptOrAssetLinks()
    {
        var sample = Sample.Create(withRelationships: true);
        var repository = new TestRepository(sample.Snapshot);
        var duplicateId = Guid.NewGuid();
        var result = await sample.Service(repository, duplicateId).DuplicateItemAsync(new(sample.Item.Id));

        Assert.True(result.Succeeded);
        Assert.Equal(duplicateId, result.Item!.Id);
        Assert.Equal("Idea Copy", result.Item.Name);
        Assert.Equal(ItemStatus.Draft, result.Item.Status);
        Assert.Contains(repository.Snapshot.ItemTags, link => link.ItemId == duplicateId);
        Assert.DoesNotContain(repository.Snapshot.Prompts, prompt => prompt.ItemId == duplicateId);
        Assert.DoesNotContain(repository.Snapshot.AssetLinks, link => link.EntityId == duplicateId);
    }

    [Fact]
    public async Task ArchiveRestoreAndStateRespectEffectiveActivityAndUnavailableParent()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = sample.Service(repository);

        var archived = await service.ArchiveItemAsync(sample.Item.Id);
        var restored = await service.RestoreItemAsync(new(sample.Item.Id));
        await service.ArchiveItemAsync(sample.Item.Id);
        repository.Set(repository.Snapshot with
        {
            Groups = repository.Snapshot.Groups.Select(group => group.Id == sample.Root.Id ? group with { IsArchived = true } : group).ToArray()
        });
        var blocked = await service.RestoreItemAsync(new(sample.Item.Id));

        Assert.True(archived.Succeeded);
        Assert.Single(archived.State.ArchivedListings);
        Assert.True(restored.Succeeded);
        Assert.Single(restored.State.ActiveItems);
        Assert.False(blocked.Succeeded);
        Assert.True(repository.Snapshot.Items.Single().IsArchived);
    }

    [Fact]
    public async Task DeleteRequiresConfirmationAndNoCreativeDependenciesThenRemovesOnlyListingLinks()
    {
        var sample = Sample.Create(withRelationships: true);
        var repository = new TestRepository(sample.Snapshot);
        var service = sample.Service(repository);

        Assert.False((await service.DeleteItemAsync(new(sample.Item.Id, false))).Succeeded);
        Assert.False((await service.DeleteItemAsync(new(sample.Item.Id, true))).Succeeded);
        repository.Set(repository.Snapshot with { Prompts = [], AssetLinks = [] });
        var deleted = await service.DeleteItemAsync(new(sample.Item.Id, true));

        Assert.True(deleted.Succeeded);
        Assert.Empty(repository.Snapshot.Items);
        Assert.Empty(repository.Snapshot.ItemTags);
        Assert.Single(repository.Snapshot.Tags);
    }

    [Fact]
    public async Task ActiveWorkspaceScopesSelectionAndClearsInactiveSelection()
    {
        var sample = Sample.Create();
        var service = sample.Service();
        service.SetActiveWorkspace(sample.Store.WorkspaceId);

        Assert.True((await service.SelectItemAsync(sample.Item.Id)).Succeeded);
        var state = await service.LoadAsync(sample.Store.Id);
        service.SetActiveWorkspace(Guid.NewGuid());
        var outside = await service.LoadAsync(sample.Store.Id);

        Assert.Equal(sample.Item.Id, state.ActiveItemId);
        Assert.Null(outside.ActiveStoreId);
        Assert.Null(outside.ActiveItemId);
    }

    [Fact]
    public async Task SetStatus_ChangesStatusAndPersistsAtomicallyWithoutMovingStage()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = sample.Service(repository);

        var result = await service.SetItemStatusAsync(new(sample.Item.Id, ItemStatus.Rejected, ConfirmProtectedTransition: true));

        Assert.True(result.Succeeded);
        Assert.Equal(ItemStatus.Rejected, result.Item!.Status);
        Assert.Equal(WorkflowStage.Design, result.Item.Stage);
        var persisted = repository.Snapshot.Items.Single(listing => listing.Id == sample.Item.Id);
        Assert.Equal(ItemStatus.Rejected, persisted.Status);
        Assert.Equal(WorkflowStage.Design, persisted.Stage);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task SetStatus_RequiresConfirmationForProtectedTransitions()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = sample.Service(repository);

        var unconfirmed = await service.SetItemStatusAsync(new(sample.Item.Id, ItemStatus.Rejected, ConfirmProtectedTransition: false));

        Assert.False(unconfirmed.Succeeded);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task SetStatus_RejectsDisallowedDirectTransition()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = sample.Service(repository);

        var result = await service.SetItemStatusAsync(new(sample.Item.Id, ItemStatus.Paused, ConfirmProtectedTransition: true));

        Assert.False(result.Succeeded);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task SetStatus_ReactivatesRejectedListingWithoutRestrictingTarget()
    {
        var sample = Sample.Create();
        var rejected = sample.Item with { Status = ItemStatus.Rejected };
        var repository = new TestRepository(sample.Snapshot with { Items = [rejected] });
        var service = sample.Service(repository);

        var result = await service.SetItemStatusAsync(new(rejected.Id, ItemStatus.Draft));

        Assert.True(result.Succeeded);
        Assert.Equal(ItemStatus.Draft, repository.Snapshot.Items.Single(listing => listing.Id == rejected.Id).Status);
    }

    [Fact]
    public async Task SetStatus_SaveFailureLeavesSnapshotUnchanged()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot) { FailSaves = true };
        var service = sample.Service(repository);

        var failed = await service.SetItemStatusAsync(new(sample.Item.Id, ItemStatus.Published));

        Assert.False(failed.Succeeded);
        Assert.Equal(sample.Snapshot, repository.Snapshot);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task MoveStage_AdvancesAndRegressesStageWithoutChangingStatus()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = sample.Service(repository);

        var advanced = await service.MoveItemStageAsync(new(sample.Item.Id, WorkflowStage.Listing));
        var regressed = await service.MoveItemStageAsync(new(sample.Item.Id, WorkflowStage.Design));

        Assert.True(advanced.Succeeded);
        Assert.Equal(WorkflowStage.Listing, advanced.Item!.Stage);
        Assert.Equal(ItemStatus.Draft, advanced.Item.Status);
        Assert.True(regressed.Succeeded);
        Assert.Equal(WorkflowStage.Design, repository.Snapshot.Items.Single(listing => listing.Id == sample.Item.Id).Stage);
        Assert.Equal(ItemStatus.Draft, repository.Snapshot.Items.Single(listing => listing.Id == sample.Item.Id).Status);
        Assert.Equal(2, repository.SaveCount);
    }

    [Fact]
    public async Task MoveStage_RejectsNonAdjacentMovement()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = sample.Service(repository);

        var result = await service.MoveItemStageAsync(new(sample.Item.Id, WorkflowStage.Listing));

        Assert.True(result.Succeeded);

        var nonAdjacent = await service.MoveItemStageAsync(new(sample.Item.Id, WorkflowStage.Concept));

        Assert.False(nonAdjacent.Succeeded);
        Assert.Equal(WorkflowStage.Listing, repository.Snapshot.Items.Single(listing => listing.Id == sample.Item.Id).Stage);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task MoveStage_RejectsInactiveListings()
    {
        var sample = Sample.Create();
        var rejected = sample.Item with { Status = ItemStatus.Rejected };
        var repository = new TestRepository(sample.Snapshot with { Items = [rejected] });
        var service = sample.Service(repository);
        var before = repository.Snapshot;

        var result = await service.MoveItemStageAsync(new(rejected.Id, WorkflowStage.Listing));

        Assert.False(result.Succeeded);
        Assert.Equal(before, repository.Snapshot);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task MoveStage_SaveFailureLeavesSnapshotUnchanged()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot) { FailSaves = true };
        var service = sample.Service(repository);

        var failed = await service.MoveItemStageAsync(new(sample.Item.Id, WorkflowStage.Listing));

        Assert.False(failed.Succeeded);
        Assert.Equal(sample.Snapshot, repository.Snapshot);
    }

    [Fact]
    public async Task Duplicate_ResetsStageToIdeaAlongsideDraftStatus()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var duplicateId = Guid.NewGuid();
        var advancedSource = sample.Item with { Stage = WorkflowStage.Listing, Status = ItemStatus.Published };
        repository.Set(sample.Snapshot with { Items = [advancedSource] });

        var result = await sample.Service(repository, duplicateId).DuplicateItemAsync(new(advancedSource.Id));

        Assert.True(result.Succeeded);
        Assert.Equal(WorkflowStage.Idea, result.Item!.Stage);
        Assert.Equal(ItemStatus.Draft, result.Item.Status);
        Assert.Equal(WorkflowStage.Listing, repository.Snapshot.Items.Single(listing => listing.Id == advancedSource.Id).Stage);
    }

    [Fact]
    public async Task Move_PreservesStageAndStatus()
    {
        var sample = Sample.Create();
        var advanced = sample.Item with { Stage = WorkflowStage.Listing, Status = ItemStatus.Published };
        var repository = new TestRepository(sample.Snapshot with { Items = [advanced] });
        var service = sample.Service(repository);

        var moved = await service.MoveItemAsync(new(advanced.Id, new(WorkspaceEntityKind.Niche, sample.Niche.Id)));

        Assert.True(moved.Succeeded);
        var persisted = repository.Snapshot.Items.Single(listing => listing.Id == advanced.Id);
        Assert.Equal(WorkflowStage.Listing, persisted.Stage);
        Assert.Equal(ItemStatus.Published, persisted.Status);
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
            if (FailSaves) throw new IOException("save failed");
            Snapshot = value;
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed record Sample(WorkspaceSnapshot Snapshot, DateTimeOffset Now, Store Store, Niche Niche, TopicGroup Root, TopicGroup Child, Item Item, Tag Tag)
    {
        public ItemManagementService Service(TestRepository? repository = null, Guid? nextId = null) =>
            new(repository ?? new TestRepository(Snapshot), clock: () => Now.AddMinutes(1), newId: () => nextId ?? Guid.NewGuid());

        public static Sample Create(bool withRelationships = false)
        {
            var now = DateTimeOffset.UtcNow;
            var nicheId = Guid.NewGuid();
            var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
            var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
            var root = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Root", null, false, now, now, "{}");
            var child = new TopicGroup(Guid.NewGuid(), store.Id, null, root.Id, "Child", null, false, now, now, "{}");
            var listing = new Item(Guid.NewGuid(), store.Id, niche.Id, child.Id, "Idea", "Description", ItemStatus.Draft, WorkflowStage.Design, false, now, now, "{\"notes\":\"Notes\",\"unknown\":\"kept\"}");
            var tag = new Tag(Guid.NewGuid(), store.Id, "Tag", null, false, now, now, "{}");
            var prompt = new Prompt(Guid.NewGuid(), store.Id, listing.Id, "Prompt", null, "Text", false, now, now, "{}");
            var assetLink = new AssetLink(Guid.NewGuid(), WorkspaceEntityKind.Item, listing.Id);
            var snapshot = new WorkspaceSnapshot(
                [store], [niche], [root, child], [listing], [],
                withRelationships ? [prompt] : [], [tag], [new ItemTag(listing.Id, tag.Id)],
                withRelationships ? [assetLink] : []);
            return new(snapshot, now, store, niche, root, child, listing, tag);
        }
    }
}
