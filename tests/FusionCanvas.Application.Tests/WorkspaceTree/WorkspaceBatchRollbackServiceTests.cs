using FusionCanvas.Application.WorkspaceTree;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests.WorkspaceTree;

public sealed class WorkspaceBatchRollbackServiceTests
{
    [Fact]
    public async Task RestoreAsync_SavesOriginalSnapshot()
    {
        var original = CreateSnapshot();
        var repository = new TestRepository();
        var service = new WorkspaceBatchRollbackService(repository);

        var result = await service.RestoreAsync(original, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
        Assert.Same(original, repository.SavedSnapshot);
    }

    [Fact]
    public async Task RestoreAsync_ReportsCompensationFailure()
    {
        var repository = new TestRepository { Failure = new IOException("Injected rollback failure.") };
        var service = new WorkspaceBatchRollbackService(repository);

        var result = await service.RestoreAsync(CreateSnapshot(), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal("Injected rollback failure.", result.Error);
    }

    private static WorkspaceSnapshot CreateSnapshot() =>
        new([], [], [], [], [], [], [], [], []);

    private sealed class TestRepository : IWorkspaceRepository
    {
        public Exception? Failure { get; init; }
        public WorkspaceSnapshot? SavedSnapshot { get; private set; }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateSnapshot());

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            if (Failure is not null)
            {
                return Task.FromException(Failure);
            }

            SavedSnapshot = snapshot;
            return Task.CompletedTask;
        }
    }
}
