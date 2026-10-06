using FusionCanvas.Application.Mockups;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests.Mockups;

public sealed class MockupOutputResolutionSettingsServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task LoadAsync_MissingValueUsesDefault()
    {
        var store = new Store(Guid.NewGuid(), "Store", null, false, Now, Now, "{\"brand\":\"North Star\"}");
        var repository = new MemoryRepository(new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)], [store], [], [], [], [], [], [], [], []));

        var result = await new MockupOutputResolutionSettingsService(repository, () => Now).LoadAsync(store.Id);

        Assert.Equal(2000, result.Policy.MaximumLongEdgePixels);
        Assert.False(result.IsReadOnly);
    }

    [Fact]
    public async Task SaveAsync_PreservesExistingMetadataAndRoundTrips()
    {
        var store = new Store(Guid.NewGuid(), "Store", null, false, Now, Now, "{\"brand\":\"North Star\"}");
        var repository = new MemoryRepository(new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)], [store], [], [], [], [], [], [], [], []));
        var service = new MockupOutputResolutionSettingsService(repository, () => Now);

        var saved = await service.SaveAsync(store.Id, 1600);
        var loaded = await service.LoadAsync(store.Id);

        Assert.True(saved.Succeeded);
        Assert.Equal(1600, loaded.Policy.MaximumLongEdgePixels);
        Assert.Contains("North Star", repository.Snapshot.Stores.Single().MetadataJson);
        Assert.Contains("mockupMaximumLongEdgePixels", repository.Snapshot.Stores.Single().MetadataJson);
    }

    [Fact]
    public async Task SaveAsync_RejectsInvalidValueAndLeavesStoreUnchanged()
    {
        var store = new Store(Guid.NewGuid(), "Store", null, false, Now, Now, "{}");
        var repository = new MemoryRepository(new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)], [store], [], [], [], [], [], [], [], []));
        var service = new MockupOutputResolutionSettingsService(repository, () => Now);

        var result = await service.SaveAsync(store.Id, 0);

        Assert.False(result.Succeeded);
        Assert.Equal("{}", repository.Snapshot.Stores.Single().MetadataJson);
    }

    private sealed class MemoryRepository : IWorkspaceRepository
    {
        public MemoryRepository(WorkspaceSnapshot snapshot) => Snapshot = snapshot;

        public WorkspaceSnapshot Snapshot { get; private set; }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            Snapshot = snapshot;
            return Task.CompletedTask;
        }
    }
}
