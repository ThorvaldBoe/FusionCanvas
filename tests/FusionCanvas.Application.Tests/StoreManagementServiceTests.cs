using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests;

public class StoreManagementServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateStoreAsync_CreatesFirstStoreWithMinimalConfiguration()
    {
        var repository = new InMemoryWorkspaceRepository();
        var storeId = Guid.NewGuid();
        var service = new StoreManagementService(repository, () => Now, () => storeId);

        var result = await service.CreateStoreAsync(new StoreManagementCreateRequest(" North Star Studio "), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(storeId, result.Store?.Id);
        Assert.Equal("North Star Studio", result.Store?.Name);
        Assert.False(result.Store?.IsArchived);
        Assert.Equal(storeId, result.State.ActiveStoreId);
        var saved = await repository.LoadAsync(TestContext.Current.CancellationToken);
        var store = Assert.Single(saved.Stores);
        Assert.Equal(storeId, store.Id);
        Assert.Equal("North Star Studio", store.Name);
        Assert.Equal("{}", store.MetadataJson);
    }

    [Fact]
    public async Task CreateStoreAsync_PersistsOptionalContextInMetadata()
    {
        var repository = new InMemoryWorkspaceRepository();
        var service = new StoreManagementService(repository, () => Now, () => Guid.NewGuid());
        var context = new StoreContext(
            Description: "POD brand",
            Notes: "Soft humor",
            TargetMarket: "Coffee fans",
            BrandDirection: "Warm vintage",
            PlanningContext: "Fall launch");

        var result = await service.CreateStoreAsync(new StoreManagementCreateRequest("North Star Studio", context), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(context, result.Store?.Context);
        var saved = await repository.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Contains("\"notes\":\"Soft humor\"", Assert.Single(saved.Stores).MetadataJson);
        Assert.Contains("\"targetMarket\":\"Coffee fans\"", Assert.Single(saved.Stores).MetadataJson);
        Assert.Contains("\"brandDirection\":\"Warm vintage\"", Assert.Single(saved.Stores).MetadataJson);
        Assert.Contains("\"planningContext\":\"Fall launch\"", Assert.Single(saved.Stores).MetadataJson);
    }

    [Fact]
    public async Task CreateStoreAsync_RejectsEmptyAndDuplicateActiveNames()
    {
        var existing = NewStore("North Star Studio");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([existing], [], [], [], [], [], [], [], []));
        var service = new StoreManagementService(repository);

        var empty = await service.CreateStoreAsync(new StoreManagementCreateRequest(" "), TestContext.Current.CancellationToken);
        var duplicate = await service.CreateStoreAsync(new StoreManagementCreateRequest("north star studio"), TestContext.Current.CancellationToken);

        Assert.False(empty.Succeeded);
        Assert.Contains("required", empty.Error);
        Assert.False(duplicate.Succeeded);
        Assert.Contains("already uses", duplicate.Error);
        Assert.Single((await repository.LoadAsync(TestContext.Current.CancellationToken)).Stores);
    }

    [Fact]
    public async Task UpdateStoreAsync_RenamesAndEditsContextWithoutChangingChildAssociations()
    {
        var store = NewStore("North Star Studio");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Coffee", null, false, Now, Now, "{}");
        var listing = new Listing(Guid.NewGuid(), store.Id, niche.Id, null, "Pumpkin espresso", null, ListingStatus.Draft, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [niche], [], [listing], [], [], [], [], []));
        var service = new StoreManagementService(repository, () => Now.AddMinutes(5));
        var context = new StoreContext("Updated", "Notes", "Dog owners", "Playful", "Q4 plan");

        var result = await service.UpdateStoreAsync(new StoreManagementUpdateRequest(store.Id, "North Star Gifts", context), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(store.Id, result.Store?.Id);
        Assert.Equal("North Star Gifts", result.Store?.Name);
        Assert.Equal(context, result.Store?.Context);
        var saved = await repository.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(store.Id, Assert.Single(saved.Niches).StoreId);
        Assert.Equal(store.Id, Assert.Single(saved.Listings).StoreId);
    }

    [Fact]
    public async Task ArchiveAndRestoreStoreAsync_SeparateActiveAndArchivedStores()
    {
        var store = NewStore("North Star Studio");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [], [], [], [], [], [], [], []));
        var service = new StoreManagementService(repository, () => Now.AddMinutes(1));
        await service.SelectStoreAsync(store.Id, TestContext.Current.CancellationToken);

        var archived = await service.ArchiveStoreAsync(store.Id, TestContext.Current.CancellationToken);
        var restored = await service.RestoreStoreAsync(store.Id, TestContext.Current.CancellationToken);

        Assert.True(archived.Succeeded);
        Assert.Empty(archived.State.ActiveStores);
        Assert.Equal(store.Id, Assert.Single(archived.State.ArchivedStores).Id);
        Assert.Null(archived.State.ActiveStoreId);
        Assert.True(restored.Succeeded);
        Assert.Equal(store.Id, Assert.Single(restored.State.ActiveStores).Id);
        Assert.Empty(restored.State.ArchivedStores);
    }

    [Fact]
    public async Task SelectStoreAsync_RejectsArchivedStore()
    {
        var archivedStore = NewStore("North Star Studio") with { IsArchived = true };
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([archivedStore], [], [], [], [], [], [], [], []));
        var service = new StoreManagementService(repository);

        var result = await service.SelectStoreAsync(archivedStore.Id, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("restored", result.Error);
        Assert.Null(result.State.ActiveStoreId);
    }

    [Fact]
    public async Task DeleteStoreAsync_DeletesConfirmedEmptyStore()
    {
        var empty = NewStore("Empty Studio");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([empty], [], [], [], [], [], [], [], []));
        var service = new StoreManagementService(repository);
        await service.SelectStoreAsync(empty.Id, TestContext.Current.CancellationToken);

        var result = await service.DeleteStoreAsync(new StoreManagementDeleteRequest(empty.Id, ConfirmPermanentDeletion: true), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Empty(result.State.ActiveStores);
        Assert.Null(result.State.ActiveStoreId);
        Assert.Empty((await repository.LoadAsync(TestContext.Current.CancellationToken)).Stores);
    }

    [Fact]
    public async Task DeleteStoreAsync_RequiresConfirmation()
    {
        var empty = NewStore("Empty Studio");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([empty], [], [], [], [], [], [], [], []));
        var service = new StoreManagementService(repository);

        var result = await service.DeleteStoreAsync(new StoreManagementDeleteRequest(empty.Id, ConfirmPermanentDeletion: false), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("confirmation", result.Error);
        Assert.Equal(empty.Id, Assert.Single((await repository.LoadAsync(TestContext.Current.CancellationToken)).Stores).Id);
    }

    [Fact]
    public async Task DeleteStoreAsync_BlocksStoreWithConnectedData()
    {
        var store = NewStore("North Star Studio");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Coffee", null, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([store], [niche], [], [], [], [], [], [], []));
        var service = new StoreManagementService(repository);

        var result = await service.DeleteStoreAsync(new StoreManagementDeleteRequest(store.Id, ConfirmPermanentDeletion: true), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("connected data", result.Error);
        Assert.Equal(store.Id, Assert.Single((await repository.LoadAsync(TestContext.Current.CancellationToken)).Stores).Id);
    }

    [Fact]
    public async Task LoadAsync_ReportsFirstStoreNeededForEmptyWorkspace()
    {
        var service = new StoreManagementService(new InMemoryWorkspaceRepository());

        var state = await service.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(state.NeedsFirstStore);
        Assert.Empty(state.ActiveStores);
        Assert.Empty(state.ArchivedStores);
    }

    [Fact]
    public async Task StoreManagement_IsScopedToActiveWorkspace()
    {
        var personal = NewWorkspace("Personal");
        var client = NewWorkspace("Client");
        var personalStore = NewStore("Shared Name") with { WorkspaceId = personal.Id };
        var clientStore = NewStore("Client Store") with { WorkspaceId = client.Id };
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([personal, client], [personalStore, clientStore], [], [], [], [], [], [], [], []));
        var service = new StoreManagementService(repository, () => Now, () => Guid.NewGuid());
        service.SetActiveWorkspace(client.Id);

        var state = await service.LoadAsync(TestContext.Current.CancellationToken);
        var allowedDuplicate = await service.CreateStoreAsync(new StoreManagementCreateRequest("Shared Name"), TestContext.Current.CancellationToken);
        var rejectedOtherWorkspaceStore = await service.SelectStoreAsync(personalStore.Id, TestContext.Current.CancellationToken);

        Assert.Equal(clientStore.Id, Assert.Single(state.ActiveStores).Id);
        Assert.True(allowedDuplicate.Succeeded);
        Assert.False(rejectedOtherWorkspaceStore.Succeeded);
        Assert.Contains("active workspace", rejectedOtherWorkspaceStore.Error);
    }

    private static Store NewStore(string name) =>
        new(Guid.NewGuid(), name, null, false, Now, Now, "{}");

    private static FusionCanvas.Domain.Workspace.Workspace NewWorkspace(string name) =>
        new(Guid.NewGuid(), name, null, false, Now, Now, "{}");

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
