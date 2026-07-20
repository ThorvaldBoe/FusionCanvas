using System.Text.Json;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests;

public class IdeaInboxServiceTests
{
    [Fact]
    public async Task Load_ListsOnlyActiveIdeaStageListingsExcludingRejected()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var listingService = sample.ListingService(repository);
        var inbox = new IdeaInboxService(repository, listingService, clock: () => sample.Now.AddMinutes(1));

        var state = await inbox.LoadAsync(sample.Store.Id);

        Assert.Single(state.Ideas);
        Assert.Equal(sample.Listing.Id, state.Ideas[0].Id);
        Assert.False(state.Ideas[0].IsRejected);
    }

    [Fact]
    public async Task Capture_DelegatesToListingManagementAndPersistsIdeaMetadata()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var id = Guid.NewGuid();
        var listingService = sample.ListingService(repository, id);
        var inbox = new IdeaInboxService(repository, listingService, clock: () => sample.Now.AddMinutes(1));

        var result = await inbox.CaptureAsync(new IdeaInboxCaptureRequest(
            new ListingTopicReference(WorkspaceEntityKind.Niche, sample.Niche.Id),
            "New Idea",
            new IdeaInboxContext("Coffee lovers", "coffee first", "grumpy pug")));

        Assert.True(result.Succeeded);
        Assert.Equal("New Idea", result.Idea!.Name);
        Assert.Equal("Coffee lovers", result.Idea.Audience);
        Assert.Equal("coffee first", result.Idea.PhraseFragments);
        Assert.Equal("grumpy pug", result.Idea.VisualDirection);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task Capture_RejectsInvalidNameWithoutPersisting()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot) { FailSaves = true };
        var listingService = sample.ListingService(repository);
        var inbox = new IdeaInboxService(repository, listingService, clock: () => sample.Now.AddMinutes(1));

        var result = await inbox.CaptureAsync(new IdeaInboxCaptureRequest(
            new ListingTopicReference(WorkspaceEntityKind.Niche, sample.Niche.Id),
            ""));

        Assert.False(result.Succeeded);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task EditContext_PreservesIdentityAndUnknownMetadata()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var listingService = sample.ListingService(repository);
        var inbox = new IdeaInboxService(repository, listingService, clock: () => sample.Now.AddMinutes(1));

        var result = await inbox.EditContextAsync(new IdeaInboxEditContextRequest(
            sample.Listing.Id,
            new IdeaInboxContext("New audience", null, null)));

        Assert.True(result.Succeeded);
        var metadata = ParseMetadata(repository.Snapshot.Listings.Single(l => l.Id == sample.Listing.Id).MetadataJson);
        Assert.Equal("New audience", metadata["idea.audience"]);
        Assert.Equal("kept", metadata["unknown"]);
    }

    [Fact]
    public async Task EditContext_RejectsNonIdeaStageListing()
    {
        var sample = Sample.Create();
        var advanced = sample.Listing with { Stage = WorkflowStage.Design };
        var repository = new TestRepository(sample.Snapshot with { Listings = [advanced] });
        var listingService = sample.ListingService(repository);
        var inbox = new IdeaInboxService(repository, listingService, clock: () => sample.Now.AddMinutes(1));

        var result = await inbox.EditContextAsync(new IdeaInboxEditContextRequest(advanced.Id, new IdeaInboxContext("a")));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Promote_RequestsStageNavigatorAndRemovesFromInbox()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var listingService = sample.ListingService(repository);
        var inbox = new IdeaInboxService(repository, listingService, clock: () => sample.Now.AddMinutes(1));

        var result = await inbox.PromoteAsync(sample.Listing.Id, WorkflowStage.Concept);

        Assert.True(result.Succeeded);
        Assert.Empty(result.State.Ideas);
        Assert.Equal(WorkflowStage.Concept, repository.Snapshot.Listings.Single().Stage);
    }

    [Fact]
    public async Task Promote_RejectsIdeaStageTarget()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var listingService = sample.ListingService(repository);
        var inbox = new IdeaInboxService(repository, listingService, clock: () => sample.Now.AddMinutes(1));

        var result = await inbox.PromoteAsync(sample.Listing.Id, WorkflowStage.Idea);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Archive_DelegatesToListingManagementAndRemovesFromInbox()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var listingService = sample.ListingService(repository);
        var inbox = new IdeaInboxService(repository, listingService, clock: () => sample.Now.AddMinutes(1));

        var result = await inbox.ArchiveAsync(sample.Listing.Id);

        Assert.True(result.Succeeded);
        Assert.Empty(result.State.Ideas);
        Assert.True(repository.Snapshot.Listings.Single().IsArchived);
    }

    [Fact]
    public async Task Reject_ArchivesWithMarkerAndPreservesRecord()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var listingService = sample.ListingService(repository);
        var inbox = new IdeaInboxService(repository, listingService, clock: () => sample.Now.AddMinutes(1));

        var result = await inbox.RejectAsync(new IdeaInboxRejectRequest(sample.Listing.Id, "Not strong enough"));

        Assert.True(result.Succeeded);
        Assert.True(repository.Snapshot.Listings.Single().IsArchived);
        var metadata = ParseMetadata(repository.Snapshot.Listings.Single().MetadataJson);
        Assert.Equal("true", metadata["idea.rejected"]);
        Assert.Equal("Not strong enough", metadata["idea.rejectedReason"]);
    }

    [Fact]
    public async Task Restore_ClearsRejectedMarkerAndReturnsToInbox()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var listingService = sample.ListingService(repository);
        var inbox = new IdeaInboxService(repository, listingService, clock: () => sample.Now.AddMinutes(1));

        await inbox.RejectAsync(new IdeaInboxRejectRequest(sample.Listing.Id, "bad"));
        var result = await inbox.RestoreAsync(sample.Listing.Id);

        Assert.True(result.Succeeded);
        Assert.False(repository.Snapshot.Listings.Single().IsArchived);
        var metadata = ParseMetadata(repository.Snapshot.Listings.Single().MetadataJson);
        Assert.False(metadata.ContainsKey("idea.rejected"));
        Assert.False(metadata.ContainsKey("idea.rejectedReason"));
        Assert.Single(result.State.Ideas);
    }

    [Fact]
    public async Task Restore_BlockedWhenParentTopicIsArchived()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var listingService = sample.ListingService(repository);
        var inbox = new IdeaInboxService(repository, listingService, clock: () => sample.Now.AddMinutes(1));

        await inbox.RejectAsync(new IdeaInboxRejectRequest(sample.Listing.Id));
        repository.Set(repository.Snapshot with
        {
            Groups = repository.Snapshot.Groups.Select(group => group.Id == sample.Root.Id ? group with { IsArchived = true } : group).ToArray()
        });
        var result = await inbox.RestoreAsync(sample.Listing.Id);

        Assert.False(result.Succeeded);
        Assert.True(repository.Snapshot.Listings.Single().IsArchived);
    }

    [Fact]
    public async Task SaveFailureLeavesSnapshotUnchanged()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot) { FailSaves = true };
        var listingService = sample.ListingService(repository);
        var inbox = new IdeaInboxService(repository, listingService, clock: () => sample.Now.AddMinutes(1));

        var editResult = await inbox.EditContextAsync(new IdeaInboxEditContextRequest(sample.Listing.Id, new IdeaInboxContext("x")));
        var rejectResult = await inbox.RejectAsync(new IdeaInboxRejectRequest(sample.Listing.Id));

        Assert.False(editResult.Succeeded);
        Assert.False(rejectResult.Succeeded);
        Assert.Equal(sample.Snapshot, repository.Snapshot);
    }

    [Fact]
    public async Task Select_OnlyAcceptsActiveIdeaStageListings()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var listingService = sample.ListingService(repository);
        var inbox = new IdeaInboxService(repository, listingService, clock: () => sample.Now.AddMinutes(1));

        var result = await inbox.SelectAsync(sample.Listing.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(sample.Listing.Id, result.State.ActiveListingId);
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

    private sealed record Sample(WorkspaceSnapshot Snapshot, DateTimeOffset Now, Store Store, Niche Niche, TopicGroup Root, TopicGroup Child, Listing Listing)
    {
        public ListingManagementService ListingService(TestRepository repository, Guid? nextId = null) =>
            new(repository, clock: () => Now.AddMinutes(1), newId: () => nextId ?? Guid.NewGuid());

        public static Sample Create()
        {
            var now = DateTimeOffset.UtcNow;
            var nicheId = Guid.NewGuid();
            var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
            var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
            var root = new TopicGroup(Guid.NewGuid(), store.Id, nicheId, null, "Root", null, false, now, now, "{}");
            var child = new TopicGroup(Guid.NewGuid(), store.Id, null, root.Id, "Child", null, false, now, now, "{}");
            var listing = new Listing(Guid.NewGuid(), store.Id, nicheId, child.Id, "Idea", "Description", ListingStatus.Draft, WorkflowStage.Idea, false, now, now, "{\"unknown\":\"kept\"}");
            var snapshot = WorkspaceSnapshot.FromStores([store], [niche], [root, child], [listing], [], [], [], [], []);
            return new(snapshot, now, store, niche, root, child, listing);
        }
    }
    private static Dictionary<string, string> ParseMetadata(string metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson) || metadataJson.Trim() == "{}")
        {
            return new(StringComparer.Ordinal);
        }

        using var document = JsonDocument.Parse(metadataJson);
        return document.RootElement.ValueKind == JsonValueKind.Object
            ? document.RootElement.EnumerateObject().ToDictionary(property => property.Name, property => property.Value.ToString(), StringComparer.Ordinal)
            : new(StringComparer.Ordinal);
    }
}