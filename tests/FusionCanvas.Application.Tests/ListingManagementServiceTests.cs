using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests;

public class ListingManagementServiceTests
{
    [Fact]
    public async Task ResolveCreateTopic_UsesSelectionListingParentAndDefaultWithoutGuessing()
    {
        var sample = Sample.Create();
        var service = sample.Service();

        var group = await service.ResolveCreateTopicAsync(sample.Store.Id, new WorkspaceTreeSelection(WorkspaceEntityKind.Group, sample.Child.Id));
        var listing = await service.ResolveCreateTopicAsync(sample.Store.Id, new WorkspaceTreeSelection(WorkspaceEntityKind.Listing, sample.Listing.Id));
        var fallback = await service.ResolveCreateTopicAsync(sample.Store.Id, null);
        var ambiguousStore = sample.Store with { DefaultNicheId = null };
        var ambiguous = await new ListingManagementService(new TestRepository(sample.Snapshot with
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

        var created = await service.CreateListingAsync(new ListingManagementCreateRequest(
            new ListingTopicReference(WorkspaceEntityKind.Group, sample.Child.Id),
            " Idea ",
            new ListingContext(" Description ", " Notes ", new Dictionary<string, string> { ["unknown"] = "kept" }, [sample.Tag.Id])));
        var updated = await service.UpdateListingAsync(new ListingManagementUpdateRequest(
            id,
            " Renamed ",
            created.Listing!.Context with { Description = " Changed ", Notes = " Revised " }));

        Assert.True(created.Succeeded);
        Assert.True(updated.Succeeded);
        Assert.Equal(ListingStatus.Draft, created.Listing.Status);
        Assert.Equal("Renamed", updated.Listing!.Name);
        Assert.Equal("Changed", updated.Listing.Context.Description);
        Assert.Equal("Revised", updated.Listing.Context.Notes);
        Assert.Equal("kept", updated.Listing.Context.Metadata!["unknown"]);
        Assert.Contains(repository.Snapshot.ListingTags, link => link.ListingId == id && link.TagId == sample.Tag.Id);
        Assert.Equal(2, repository.SaveCount);
    }

    [Fact]
    public async Task InvalidNamesAndSaveFailureLeaveSnapshotUnchanged()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot) { FailSaves = true };
        var service = sample.Service(repository);

        var invalid = await service.CreateListingAsync(new ListingManagementCreateRequest(
            new ListingTopicReference(WorkspaceEntityKind.Niche, sample.Niche.Id), "two\nlines"));
        var failed = await service.CreateListingAsync(new ListingManagementCreateRequest(
            new ListingTopicReference(WorkspaceEntityKind.Niche, sample.Niche.Id), "Valid"));

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

        var crossStore = await service.MoveListingAsync(new(sample.Listing.Id, new(WorkspaceEntityKind.Niche, otherNiche.Id)));
        var inactiveSnapshot = repository.Snapshot with
        {
            Groups = repository.Snapshot.Groups.Select(group => group.Id == sample.Root.Id ? group with { IsArchived = true } : group).ToArray()
        };
        repository.Set(inactiveSnapshot);
        var inactive = await service.MoveListingAsync(new(sample.Listing.Id, new(WorkspaceEntityKind.Niche, sample.Niche.Id)));

        Assert.False(crossStore.Succeeded);
        Assert.False(inactive.Succeeded);
        Assert.Equal(sample.Child.Id, repository.Snapshot.Listings.Single(listing => listing.Id == sample.Listing.Id).GroupId);
        Assert.Single(repository.Snapshot.ListingTags);
    }

    [Fact]
    public async Task Duplicate_CopiesCoreMetadataAndTagsButNotPromptOrAssetLinks()
    {
        var sample = Sample.Create(withRelationships: true);
        var repository = new TestRepository(sample.Snapshot);
        var duplicateId = Guid.NewGuid();
        var result = await sample.Service(repository, duplicateId).DuplicateListingAsync(new(sample.Listing.Id));

        Assert.True(result.Succeeded);
        Assert.Equal(duplicateId, result.Listing!.Id);
        Assert.Equal("Idea Copy", result.Listing.Name);
        Assert.Equal(ListingStatus.Draft, result.Listing.Status);
        Assert.Contains(repository.Snapshot.ListingTags, link => link.ListingId == duplicateId);
        Assert.DoesNotContain(repository.Snapshot.Prompts, prompt => prompt.ListingId == duplicateId);
        Assert.DoesNotContain(repository.Snapshot.AssetLinks, link => link.EntityId == duplicateId);
    }

    [Fact]
    public async Task ArchiveRestoreAndStateRespectEffectiveActivityAndUnavailableParent()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = sample.Service(repository);

        var archived = await service.ArchiveListingAsync(sample.Listing.Id);
        var restored = await service.RestoreListingAsync(new(sample.Listing.Id));
        await service.ArchiveListingAsync(sample.Listing.Id);
        repository.Set(repository.Snapshot with
        {
            Groups = repository.Snapshot.Groups.Select(group => group.Id == sample.Root.Id ? group with { IsArchived = true } : group).ToArray()
        });
        var blocked = await service.RestoreListingAsync(new(sample.Listing.Id));

        Assert.True(archived.Succeeded);
        Assert.Single(archived.State.ArchivedListings);
        Assert.True(restored.Succeeded);
        Assert.Single(restored.State.ActiveListings);
        Assert.False(blocked.Succeeded);
        Assert.True(repository.Snapshot.Listings.Single().IsArchived);
    }

    [Fact]
    public async Task DeleteRequiresConfirmationAndNoCreativeDependenciesThenRemovesOnlyListingLinks()
    {
        var sample = Sample.Create(withRelationships: true);
        var repository = new TestRepository(sample.Snapshot);
        var service = sample.Service(repository);

        Assert.False((await service.DeleteListingAsync(new(sample.Listing.Id, false))).Succeeded);
        Assert.False((await service.DeleteListingAsync(new(sample.Listing.Id, true))).Succeeded);
        repository.Set(repository.Snapshot with { Prompts = [], AssetLinks = [] });
        var deleted = await service.DeleteListingAsync(new(sample.Listing.Id, true));

        Assert.True(deleted.Succeeded);
        Assert.Empty(repository.Snapshot.Listings);
        Assert.Empty(repository.Snapshot.ListingTags);
        Assert.Single(repository.Snapshot.Tags);
    }

    [Fact]
    public async Task ActiveWorkspaceScopesSelectionAndClearsInactiveSelection()
    {
        var sample = Sample.Create();
        var service = sample.Service();
        service.SetActiveWorkspace(sample.Store.WorkspaceId);

        Assert.True((await service.SelectListingAsync(sample.Listing.Id)).Succeeded);
        var state = await service.LoadAsync(sample.Store.Id);
        service.SetActiveWorkspace(Guid.NewGuid());
        var outside = await service.LoadAsync(sample.Store.Id);

        Assert.Equal(sample.Listing.Id, state.ActiveListingId);
        Assert.Null(outside.ActiveStoreId);
        Assert.Null(outside.ActiveListingId);
    }

    [Fact]
    public async Task SetStatus_ChangesStatusAndPersistsAtomicallyWithoutMovingStage()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = sample.Service(repository);

        var result = await service.SetListingStatusAsync(new(sample.Listing.Id, ListingStatus.Paused));

        Assert.True(result.Succeeded);
        Assert.Equal(ListingStatus.Paused, result.Listing!.Status);
        Assert.Equal(WorkflowStage.Design, result.Listing.Stage);
        var persisted = repository.Snapshot.Listings.Single(listing => listing.Id == sample.Listing.Id);
        Assert.Equal(ListingStatus.Paused, persisted.Status);
        Assert.Equal(WorkflowStage.Design, persisted.Stage);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task SetStatus_ReactivatesRejectedListingWithoutRestrictingTarget()
    {
        var sample = Sample.Create();
        var rejected = sample.Listing with { Status = ListingStatus.Rejected };
        var repository = new TestRepository(sample.Snapshot with { Listings = [rejected] });
        var service = sample.Service(repository);

        var result = await service.SetListingStatusAsync(new(rejected.Id, ListingStatus.Draft));

        Assert.True(result.Succeeded);
        Assert.Equal(ListingStatus.Draft, repository.Snapshot.Listings.Single(listing => listing.Id == rejected.Id).Status);
    }

    [Fact]
    public async Task SetStatus_SaveFailureLeavesSnapshotUnchanged()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot) { FailSaves = true };
        var service = sample.Service(repository);

        var failed = await service.SetListingStatusAsync(new(sample.Listing.Id, ListingStatus.Published));

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

        var advanced = await service.MoveListingStageAsync(new(sample.Listing.Id, WorkflowStage.Listing));
        var regressed = await service.MoveListingStageAsync(new(sample.Listing.Id, WorkflowStage.Concept));

        Assert.True(advanced.Succeeded);
        Assert.Equal(WorkflowStage.Listing, advanced.Listing!.Stage);
        Assert.Equal(ListingStatus.Draft, advanced.Listing.Status);
        Assert.True(regressed.Succeeded);
        Assert.Equal(WorkflowStage.Concept, repository.Snapshot.Listings.Single(listing => listing.Id == sample.Listing.Id).Stage);
        Assert.Equal(ListingStatus.Draft, repository.Snapshot.Listings.Single(listing => listing.Id == sample.Listing.Id).Status);
        Assert.Equal(2, repository.SaveCount);
    }

    [Fact]
    public async Task MoveStage_RejectsInactiveListings()
    {
        var sample = Sample.Create();
        var rejected = sample.Listing with { Status = ListingStatus.Rejected };
        var repository = new TestRepository(sample.Snapshot with { Listings = [rejected] });
        var service = sample.Service(repository);
        var before = repository.Snapshot;

        var result = await service.MoveListingStageAsync(new(rejected.Id, WorkflowStage.Listing));

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

        var failed = await service.MoveListingStageAsync(new(sample.Listing.Id, WorkflowStage.Listing));

        Assert.False(failed.Succeeded);
        Assert.Equal(sample.Snapshot, repository.Snapshot);
    }

    [Fact]
    public async Task Duplicate_ResetsStageToIdeaAlongsideDraftStatus()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var duplicateId = Guid.NewGuid();
        var advancedSource = sample.Listing with { Stage = WorkflowStage.Listing, Status = ListingStatus.Published };
        repository.Set(sample.Snapshot with { Listings = [advancedSource] });

        var result = await sample.Service(repository, duplicateId).DuplicateListingAsync(new(advancedSource.Id));

        Assert.True(result.Succeeded);
        Assert.Equal(WorkflowStage.Idea, result.Listing!.Stage);
        Assert.Equal(ListingStatus.Draft, result.Listing.Status);
        Assert.Equal(WorkflowStage.Listing, repository.Snapshot.Listings.Single(listing => listing.Id == advancedSource.Id).Stage);
    }

    [Fact]
    public async Task Move_PreservesStageAndStatus()
    {
        var sample = Sample.Create();
        var advanced = sample.Listing with { Stage = WorkflowStage.Listing, Status = ListingStatus.Published };
        var repository = new TestRepository(sample.Snapshot with { Listings = [advanced] });
        var service = sample.Service(repository);

        var moved = await service.MoveListingAsync(new(advanced.Id, new(WorkspaceEntityKind.Niche, sample.Niche.Id)));

        Assert.True(moved.Succeeded);
        var persisted = repository.Snapshot.Listings.Single(listing => listing.Id == advanced.Id);
        Assert.Equal(WorkflowStage.Listing, persisted.Stage);
        Assert.Equal(ListingStatus.Published, persisted.Status);
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

    private sealed record Sample(WorkspaceSnapshot Snapshot, DateTimeOffset Now, Store Store, Niche Niche, TopicGroup Root, TopicGroup Child, Listing Listing, Tag Tag)
    {
        public ListingManagementService Service(TestRepository? repository = null, Guid? nextId = null) =>
            new(repository ?? new TestRepository(Snapshot), clock: () => Now.AddMinutes(1), newId: () => nextId ?? Guid.NewGuid());

        public static Sample Create(bool withRelationships = false)
        {
            var now = DateTimeOffset.UtcNow;
            var nicheId = Guid.NewGuid();
            var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
            var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
            var root = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Root", null, false, now, now, "{}");
            var child = new TopicGroup(Guid.NewGuid(), store.Id, null, root.Id, "Child", null, false, now, now, "{}");
            var listing = new Listing(Guid.NewGuid(), store.Id, niche.Id, child.Id, "Idea", "Description", ListingStatus.Draft, WorkflowStage.Design, false, now, now, "{\"notes\":\"Notes\",\"unknown\":\"kept\"}");
            var tag = new Tag(Guid.NewGuid(), store.Id, "Tag", null, false, now, now, "{}");
            var prompt = new Prompt(Guid.NewGuid(), store.Id, listing.Id, "Prompt", null, "Text", false, now, now, "{}");
            var assetLink = new AssetLink(Guid.NewGuid(), WorkspaceEntityKind.Listing, listing.Id);
            var snapshot = WorkspaceSnapshot.FromStores(
                [store], [niche], [root, child], [listing], [],
                withRelationships ? [prompt] : [], [tag], [new ListingTag(listing.Id, tag.Id)],
                withRelationships ? [assetLink] : []);
            return new(snapshot, now, store, niche, root, child, listing, tag);
        }
    }
}
