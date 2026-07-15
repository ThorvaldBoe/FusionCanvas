using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests;

public class WorkspaceRepositoryContractTests
{
    [Fact]
    public async Task WorkspaceRepositoryContract_SavesAndLoadsWorkspaceSnapshots()
    {
        IWorkspaceRepository repository = new InMemoryWorkspaceRepository();
        var snapshot = new WorkspaceSnapshot(
            [new Store(Guid.NewGuid(), "North Star Studio", null, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}")],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            []);

        await repository.SaveAsync(snapshot, TestContext.Current.CancellationToken);
        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(snapshot, loaded);
    }

    private sealed class InMemoryWorkspaceRepository : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = WorkspaceSnapshot.Empty;

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            _snapshot = snapshot;
            return Task.CompletedTask;
        }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_snapshot);
    }
}
