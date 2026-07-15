using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests;

public class WorkspaceManagementServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateWorkspaceAsync_CreatesAndSelectsWorkspace()
    {
        var repository = new InMemoryWorkspaceRepository();
        var workspaceId = Guid.NewGuid();
        var service = new WorkspaceManagementService(repository, () => Now, () => workspaceId);

        var result = await service.CreateWorkspaceAsync(new WorkspaceManagementCreateRequest(" Client Work ", new WorkspaceContext("Retainer", "Q4")));

        Assert.True(result.Succeeded);
        Assert.Equal(workspaceId, result.Workspace?.Id);
        Assert.Equal("Client Work", result.Workspace?.Name);
        Assert.Equal(workspaceId, result.State.ActiveWorkspaceId);
        var workspace = Assert.Single((await repository.LoadAsync()).Workspaces);
        Assert.Equal(workspaceId, workspace.Id);
        Assert.Contains("\"notes\":\"Q4\"", workspace.MetadataJson);
    }

    [Fact]
    public async Task WorkspaceLifecycle_RequiresTypedConfirmationForNonEmptyDeleteAndRestoresArchives()
    {
        var active = NewWorkspace("Client");
        var archived = NewWorkspace("Personal") with { IsArchived = true };
        var store = new Store(Guid.NewGuid(), active.Id, "Client Store", null, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([active, archived], [store], [], [], [], [], [], [], [], []));
        var service = new WorkspaceManagementService(repository, () => Now.AddMinutes(1));

        var deleteWithStore = await service.DeleteWorkspaceAsync(new WorkspaceManagementDeleteRequest(active.Id, ConfirmPermanentDeletion: true));
        var restored = await service.RestoreWorkspaceAsync(archived.Id);
        var archivedActive = await service.ArchiveWorkspaceAsync(active.Id);

        Assert.False(deleteWithStore.Succeeded);
        Assert.Contains("Type the workspace name", deleteWithStore.Error);
        Assert.True(restored.Succeeded);
        Assert.True(archivedActive.Succeeded);
        Assert.Contains(archivedActive.State.ArchivedWorkspaces, workspace => workspace.Id == active.Id);
    }

    [Fact]
    public async Task DeleteWorkspaceAsync_WithTypedConfirmationDeletesWorkspaceAndOwnedData()
    {
        var workspace = NewWorkspace("Client");
        var otherWorkspace = NewWorkspace("Personal");
        var store = new Store(Guid.NewGuid(), workspace.Id, "Client Store", null, false, Now, Now, "{}");
        var otherStore = new Store(Guid.NewGuid(), otherWorkspace.Id, "Personal Store", null, false, Now, Now, "{}");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Client Niche", null, false, Now, Now, "{}");
        var listing = new Listing(Guid.NewGuid(), store.Id, niche.Id, null, "Client Listing", null, ListingStatus.Draft, false, Now, Now, "{}");
        var tag = new Tag(Guid.NewGuid(), store.Id, "Client Tag", null, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot(
            [workspace, otherWorkspace],
            [store, otherStore],
            [niche],
            [],
            [listing],
            [],
            [],
            [tag],
            [new ListingTag(listing.Id, tag.Id)],
            []));
        var service = new WorkspaceManagementService(repository, () => Now.AddMinutes(1));

        var wrongName = await service.DeleteWorkspaceAsync(new WorkspaceManagementDeleteRequest(workspace.Id, true, "client"));
        var deleted = await service.DeleteWorkspaceAsync(new WorkspaceManagementDeleteRequest(workspace.Id, true, workspace.Name));
        var snapshot = await repository.LoadAsync();

        Assert.False(wrongName.Succeeded);
        Assert.True(deleted.Succeeded);
        Assert.DoesNotContain(snapshot.Workspaces, candidate => candidate.Id == workspace.Id);
        Assert.DoesNotContain(snapshot.Stores, candidate => candidate.Id == store.Id);
        Assert.Empty(snapshot.Niches);
        Assert.Empty(snapshot.Listings);
        Assert.Empty(snapshot.Tags);
        Assert.Empty(snapshot.ListingTags);
        Assert.Contains(snapshot.Stores, candidate => candidate.Id == otherStore.Id);
    }

    [Fact]
    public async Task DeleteWorkspaceAsync_BlocksDeletingLastWorkspace()
    {
        var workspace = NewWorkspace("Personal");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([workspace], [], [], [], [], [], [], [], [], []));
        var service = new WorkspaceManagementService(repository, () => Now.AddMinutes(1));

        var result = await service.DeleteWorkspaceAsync(new WorkspaceManagementDeleteRequest(workspace.Id, true, workspace.Name));

        Assert.False(result.Succeeded);
        Assert.Contains("At least one workspace", result.Error);
        Assert.Single((await repository.LoadAsync()).Workspaces);
    }

    [Fact]
    public async Task SelectWorkspaceAsync_RejectsArchivedWorkspace()
    {
        var archived = NewWorkspace("Archived") with { IsArchived = true };
        var service = new WorkspaceManagementService(new InMemoryWorkspaceRepository(new WorkspaceSnapshot([archived], [], [], [], [], [], [], [], [], [])));

        var result = await service.SelectWorkspaceAsync(archived.Id);

        Assert.False(result.Succeeded);
        Assert.Contains("restored", result.Error);
        Assert.Null(result.State.ActiveWorkspaceId);
    }

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
