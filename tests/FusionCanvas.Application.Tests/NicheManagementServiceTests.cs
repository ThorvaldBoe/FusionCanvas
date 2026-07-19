using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests;

public class NicheManagementServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateNicheAsync_CreatesFirstNicheInActiveStoreWithMinimalConfiguration()
    {
        var store = NewStore("North Star Studio");
        var nicheId = Guid.NewGuid();
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], []));
        var service = new NicheManagementService(repository, () => Now, () => nicheId);

        var result = await service.CreateNicheAsync(new NicheManagementCreateRequest(store.Id, " Coffee Lovers "), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(nicheId, result.Niche?.Id);
        Assert.Equal(store.Id, result.Niche?.StoreId);
        Assert.Equal("Coffee Lovers", result.Niche?.Name);
        Assert.Equal(nicheId, result.State.ActiveNicheId);
        Assert.False(result.State.NeedsFirstNiche);
        var saved = await repository.LoadAsync(TestContext.Current.CancellationToken);
        var niche = Assert.Single(saved.Niches);
        Assert.Equal(nicheId, niche.Id);
        Assert.Equal(store.Id, niche.StoreId);
        Assert.Equal("{}", niche.MetadataJson);
    }

    [Fact]
    public async Task CreateNicheAsync_PersistsOptionalContextInMetadata()
    {
        var store = NewStore("North Star Studio");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], []));
        var service = new NicheManagementService(repository, () => Now, () => Guid.NewGuid());
        var context = new NicheContext(
            Description: "Warm drink designs",
            Audience: "Coffee fans",
            HumorStyle: "Gentle",
            VisualStyleGuidance: "Vintage badges",
            Constraints: "Avoid trademarked chains",
            Risks: "Overused caffeine jokes",
            ResearchNotes: "Fall performs well",
            Notes: "Try cozy textures");

        var result = await service.CreateNicheAsync(new NicheManagementCreateRequest(store.Id, "Coffee", context), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(context, result.Niche?.Context);
        var saved = await repository.LoadAsync(TestContext.Current.CancellationToken);
        var metadata = Assert.Single(saved.Niches).MetadataJson;
        Assert.Contains("\"audience\":\"Coffee fans\"", metadata);
        Assert.Contains("\"humorStyle\":\"Gentle\"", metadata);
        Assert.Contains("\"visualStyleGuidance\":\"Vintage badges\"", metadata);
        Assert.Contains("\"constraints\":\"Avoid trademarked chains\"", metadata);
        Assert.Contains("\"risks\":\"Overused caffeine jokes\"", metadata);
        Assert.Contains("\"researchNotes\":\"Fall performs well\"", metadata);
        Assert.Contains("\"notes\":\"Try cozy textures\"", metadata);
    }

    [Fact]
    public async Task CreateNicheAsync_RejectsMissingArchivedAndDuplicateStoreScopedNames()
    {
        var active = NewStore("North Star Studio");
        var other = NewStore("Second Studio");
        var archived = NewStore("Paused Studio") with { IsArchived = true };
        var existing = NewNiche(active.Id, "Coffee");
        var sameNameOtherStore = NewNiche(other.Id, "Coffee");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([active, other, archived], [existing, sameNameOtherStore], [], [], [], [], [], [], []));
        var service = new NicheManagementService(repository);

        var empty = await service.CreateNicheAsync(new NicheManagementCreateRequest(active.Id, " "), TestContext.Current.CancellationToken);
        var duplicate = await service.CreateNicheAsync(new NicheManagementCreateRequest(active.Id, "coffee"), TestContext.Current.CancellationToken);
        var reused = await service.CreateNicheAsync(new NicheManagementCreateRequest(other.Id, "Coffee 2"), TestContext.Current.CancellationToken);
        var archivedStore = await service.CreateNicheAsync(new NicheManagementCreateRequest(archived.Id, "Cats"), TestContext.Current.CancellationToken);
        var missingStore = await service.CreateNicheAsync(new NicheManagementCreateRequest(Guid.NewGuid(), "Cats"), TestContext.Current.CancellationToken);

        Assert.False(empty.Succeeded);
        Assert.Contains("required", empty.Error);
        Assert.False(duplicate.Succeeded);
        Assert.Contains("already uses", duplicate.Error);
        Assert.True(reused.Succeeded);
        Assert.False(archivedStore.Succeeded);
        Assert.Contains("Archived stores", archivedStore.Error);
        Assert.False(missingStore.Succeeded);
        Assert.Contains("store is required", missingStore.Error);
    }

    [Fact]
    public async Task UpdateNicheAsync_RenamesAndEditsContextWithoutChangingChildAssociations()
    {
        var store = NewStore("North Star Studio");
        var niche = NewNiche(store.Id, "Coffee");
        var group = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Launch", null, false, Now, Now, "{}");
        var listing = new Listing(Guid.NewGuid(), store.Id, niche.Id, null, "Pumpkin espresso", null, ListingStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [niche], [group], [listing], [], [], [], [], []));
        var service = new NicheManagementService(repository, () => Now.AddMinutes(5));
        var context = new NicheContext("Updated", "Baristas", "Dry", "Retro", "No logos", "Crowded", "Check Etsy", "Notes");

        var result = await service.UpdateNicheAsync(new NicheManagementUpdateRequest(niche.Id, "Coffee Gifts", context), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(niche.Id, result.Niche?.Id);
        Assert.Equal("Coffee Gifts", result.Niche?.Name);
        Assert.Equal(context, result.Niche?.Context);
        var saved = await repository.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(niche.Id, Assert.Single(saved.Groups).NicheId);
        Assert.Equal(niche.Id, Assert.Single(saved.Listings).NicheId);
    }

    [Fact]
    public async Task ArchiveRestoreAndSelectNicheAsync_SeparateActiveAndArchivedNiches()
    {
        var store = NewStore("North Star Studio");
        var niche = NewNiche(store.Id, "Coffee");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [niche], [], [], [], [], [], [], []));
        var service = new NicheManagementService(repository);
        await service.SelectNicheAsync(niche.Id, TestContext.Current.CancellationToken);

        var archived = await service.ArchiveNicheAsync(niche.Id, TestContext.Current.CancellationToken);
        var selectedArchived = await service.SelectNicheAsync(niche.Id, TestContext.Current.CancellationToken);
        var restored = await service.RestoreNicheAsync(niche.Id, TestContext.Current.CancellationToken);

        Assert.True(archived.Succeeded);
        Assert.Empty(archived.State.ActiveNiches);
        Assert.Equal(niche.Id, Assert.Single(archived.State.ArchivedNiches).Id);
        Assert.Null(archived.State.ActiveNicheId);
        Assert.False(selectedArchived.Succeeded);
        Assert.Contains("restored", selectedArchived.Error);
        Assert.True(restored.Succeeded);
        Assert.Equal(niche.Id, Assert.Single(restored.State.ActiveNiches).Id);
    }

    [Fact]
    public async Task RestoreNicheAsync_BlocksActiveNameConflictInSameStore()
    {
        var store = NewStore("North Star Studio");
        var active = NewNiche(store.Id, "Coffee");
        var archived = NewNiche(store.Id, "coffee") with { IsArchived = true };
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [active, archived], [], [], [], [], [], [], []));
        var service = new NicheManagementService(repository);

        var result = await service.RestoreNicheAsync(archived.Id, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("already uses", result.Error);
        Assert.True((await repository.LoadAsync(TestContext.Current.CancellationToken)).Niches.Single(niche => niche.Id == archived.Id).IsArchived);
    }

    [Fact]
    public async Task DeleteNicheAsync_DeletesConfirmedEmptyNicheAndRequiresConfirmation()
    {
        var store = NewStore("North Star Studio");
        var niche = NewNiche(store.Id, "Coffee");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [niche], [], [], [], [], [], [], []));
        var service = new NicheManagementService(repository);

        var unconfirmed = await service.DeleteNicheAsync(new NicheManagementDeleteRequest(niche.Id, ConfirmPermanentDeletion: false), TestContext.Current.CancellationToken);
        var confirmed = await service.DeleteNicheAsync(new NicheManagementDeleteRequest(niche.Id, ConfirmPermanentDeletion: true), TestContext.Current.CancellationToken);

        Assert.False(unconfirmed.Succeeded);
        Assert.Contains("confirmation", unconfirmed.Error);
        Assert.True(confirmed.Succeeded);
        Assert.Empty((await repository.LoadAsync(TestContext.Current.CancellationToken)).Niches);
    }

    [Fact]
    public async Task DeleteNicheAsync_BlocksNicheWithConnectedData()
    {
        var store = NewStore("North Star Studio");
        var niche = NewNiche(store.Id, "Coffee");
        var listing = new Listing(Guid.NewGuid(), store.Id, niche.Id, null, "Espresso", null, ListingStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");
        var tag = new Tag(Guid.NewGuid(), store.Id, "seasonal", null, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [niche], [], [listing], [], [], [tag], [new ListingTag(listing.Id, tag.Id)], []));
        var service = new NicheManagementService(repository);

        var result = await service.DeleteNicheAsync(new NicheManagementDeleteRequest(niche.Id, ConfirmPermanentDeletion: true), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("connected data", result.Error);
        Assert.Equal(niche.Id, Assert.Single((await repository.LoadAsync(TestContext.Current.CancellationToken)).Niches).Id);
    }

    [Fact]
    public async Task LoadAsync_FiltersNichesToSelectedStore()
    {
        var first = NewStore("North Star Studio");
        var second = NewStore("Second Studio");
        var firstNiche = NewNiche(first.Id, "Coffee");
        var secondNiche = NewNiche(second.Id, "Cats");
        var archived = NewNiche(first.Id, "Dogs") with { IsArchived = true };
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([first, second], [firstNiche, secondNiche, archived], [], [], [], [], [], [], []));
        var service = new NicheManagementService(repository);

        var state = await service.LoadAsync(first.Id, TestContext.Current.CancellationToken);

        Assert.Equal(first.Id, state.ActiveStoreId);
        Assert.Equal(firstNiche.Id, Assert.Single(state.ActiveNiches).Id);
        Assert.Equal(archived.Id, Assert.Single(state.ArchivedNiches).Id);
        Assert.DoesNotContain(state.ActiveNiches, niche => niche.Id == secondNiche.Id);
    }

    private static Store NewStore(string name) =>
        new(Guid.NewGuid(), name, null, false, Now, Now, "{}");

    private static Niche NewNiche(Guid storeId, string name) =>
        new(Guid.NewGuid(), storeId, name, null, false, Now, Now, "{}");

    private sealed class InMemoryWorkspaceRepository(WorkspaceSnapshot? snapshot = null) : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = snapshot ?? WorkspaceSnapshot.Empty;

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            _snapshot = snapshot;
            return Task.CompletedTask;
        }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_snapshot);
    }
}
