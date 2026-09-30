using System.IO;
using Avalonia.Headless.XUnit;
using FusionCanvas.App.Workspace;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Application.Workspaces;

namespace FusionCanvas.App.Tests;

public class WorkspaceManagementViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void WorkspaceManagementCommand_OpensAndClosesManagementWindow()
    {
        var viewModel = NewViewModel(WorkspaceSnapshot.Empty);

        viewModel.OpenWorkspaceManagementCommand.Execute(null);
        Assert.True(viewModel.IsWorkspaceManagementOpen);

        viewModel.CloseWorkspaceManagementCommand.Execute(null);
        Assert.False(viewModel.IsWorkspaceManagementOpen);
    }

    [Fact]
    public async Task CreateAndSelectWorkspace_UpdatesActiveWorkspace()
    {
        var personal = NewWorkspace("Personal");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([personal], [], [], [], [], [], [], [], [], []));
        var viewModel = NewViewModel(repository);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.StartCreateWorkspaceCommand.Execute(null);
        viewModel.WorkspaceName = "Client";
        await viewModel.CreateWorkspaceAsync(TestContext.Current.CancellationToken);
        var personalSummary = viewModel.ActiveWorkspaces.Single(workspace => workspace.Id == personal.Id);
        await viewModel.SelectWorkspaceAsync(personalSummary, TestContext.Current.CancellationToken);

        Assert.False(viewModel.IsCreatingNewWorkspace);
        Assert.Equal("Personal", viewModel.SelectedWorkspace?.Name);
        Assert.Contains(viewModel.ActiveWorkspaces, workspace => workspace.Name == "Client");
    }

    [Fact]
    public async Task DeleteWorkspace_RequiresTypedNameAndRemovesOwnedStores()
    {
        var workspace = NewWorkspace("Client");
        var personal = NewWorkspace("Personal");
        var store = new Store(Guid.NewGuid(), workspace.Id, "Client Store", null, false, Now, Now, "{}");
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([workspace, personal], [store], [], [], [], [], [], [], [], []));
        var viewModel = NewViewModel(repository);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.RequestDeleteSelectedWorkspace();
        viewModel.DeleteConfirmationName = "Wrong";
        await viewModel.ConfirmDeleteWorkspaceAsync(TestContext.Current.CancellationToken);
        Assert.True(viewModel.DeleteWarningVisible);
        Assert.Contains("Type the workspace name", viewModel.ErrorMessage);

        viewModel.DeleteConfirmationName = "Client";
        await viewModel.ConfirmDeleteWorkspaceAsync(TestContext.Current.CancellationToken);
        var snapshot = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.False(viewModel.DeleteWarningVisible);
        Assert.DoesNotContain(snapshot.Workspaces, candidate => candidate.Id == workspace.Id);
        Assert.Contains(snapshot.Workspaces, candidate => candidate.Id == personal.Id);
        Assert.Empty(snapshot.Stores);
    }

    [AvaloniaFact]
    public async Task LoadAsyncAppliesStateOnUiThreadWhenServiceCompletesOffThread()
    {
        var workspaceId = Guid.NewGuid();
        var summary = new WorkspaceSummary(
            workspaceId,
            "Personal",
            new WorkspaceContext(),
            IsArchived: false,
            Now,
            Now);
        var service = new DeferredWorkspaceManagementService(
            new WorkspaceManagementState([summary], [], workspaceId, summary, NeedsFirstWorkspace: false));
        var viewModel = new WorkspaceManagementViewModel(service);
        var activeWorkspacesNotificationThreadId = 0;
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(WorkspaceManagementViewModel.ActiveWorkspaces))
            {
                activeWorkspacesNotificationThreadId = Environment.CurrentManagedThreadId;
            }
        };

        var uiThreadId = Environment.CurrentManagedThreadId;
        var loadTask = Task.Run(() => viewModel.LoadAsync(TestContext.Current.CancellationToken));
        await service.LoadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        service.ReleaseLoad();

        await TestSupport.HeadlessUiWait.UntilAsync(
            () => loadTask.IsCompleted,
            "workspace management load completes",
            cancellationToken: TestContext.Current.CancellationToken);
        await loadTask;

        Assert.Equal(uiThreadId, activeWorkspacesNotificationThreadId);
    }

    [Fact]
    public async Task LoadAsync_ReportsUnexpectedFailure()
    {
        var service = new FailingWorkspaceManagementService(FailingOperation.Load);
        var viewModel = new WorkspaceManagementViewModel(service);

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal("load failed", viewModel.ErrorMessage);
    }

    [Theory]
    [InlineData(FailingOperation.Create)]
    [InlineData(FailingOperation.Update)]
    [InlineData(FailingOperation.Archive)]
    [InlineData(FailingOperation.Restore)]
    [InlineData(FailingOperation.Delete)]
    [InlineData(FailingOperation.Select)]
    public async Task Commands_ObserveUnexpectedFailures(FailingOperation operation)
    {
        var activeWorkspace = NewWorkspace("Personal");
        var archivedWorkspace = NewWorkspace("Archived") with { IsArchived = true };
        var service = new FailingWorkspaceManagementService(
            operation,
            new WorkspaceManagementState(
                [ToSummary(activeWorkspace)],
                [ToSummary(archivedWorkspace)],
                activeWorkspace.Id,
                ToSummary(activeWorkspace),
                NeedsFirstWorkspace: false));
        var viewModel = new WorkspaceManagementViewModel(service);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        switch (operation)
        {
            case FailingOperation.Create:
                viewModel.StartCreateWorkspaceCommand.Execute(null);
                viewModel.CreateWorkspaceCommand.Execute(null);
                break;
            case FailingOperation.Update:
                viewModel.SaveSelectedWorkspaceCommand.Execute(null);
                break;
            case FailingOperation.Archive:
                viewModel.ArchiveSelectedWorkspaceCommand.Execute(null);
                break;
            case FailingOperation.Restore:
                viewModel.RestoreWorkspaceCommand.Execute(viewModel.ArchivedWorkspaces.Single());
                break;
            case FailingOperation.Delete:
                viewModel.RequestDeleteSelectedWorkspaceCommand.Execute(null);
                viewModel.DeleteConfirmationName = "Personal";
                viewModel.ConfirmDeleteWorkspaceCommand.Execute(null);
                break;
            case FailingOperation.Select:
                viewModel.SelectWorkspaceCommand.Execute(viewModel.ActiveWorkspaces.Single());
                break;
        }

        await TestSupport.HeadlessUiWait.UntilAsync(
            () => viewModel.ErrorMessage == $"{operation.ToString().ToLowerInvariant()} failed",
            $"{operation} failure is observed",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal($"{operation.ToString().ToLowerInvariant()} failed", viewModel.ErrorMessage);
    }

    private static WorkspaceManagementViewModel NewViewModel(WorkspaceSnapshot snapshot) =>
        NewViewModel(new InMemoryWorkspaceRepository(snapshot));

    private static WorkspaceManagementViewModel NewViewModel(InMemoryWorkspaceRepository repository) =>
        new(new WorkspaceManagementService(repository, new FusionCanvas.Integration.Workspaces.WorkspaceContextMapper(), () => Now));

    private static FusionCanvas.Domain.Workspace.Workspace NewWorkspace(string name) =>
        new(Guid.NewGuid(), name, null, false, Now, Now, "{}");

    private static WorkspaceSummary ToSummary(FusionCanvas.Domain.Workspace.Workspace workspace) =>
        new(workspace.Id, workspace.Name, new WorkspaceContext(), workspace.IsArchived, workspace.CreatedAt, workspace.UpdatedAt);

    public enum FailingOperation
    {
        Load,
        Create,
        Update,
        Archive,
        Restore,
        Delete,
        Select
    }

    private sealed class InMemoryWorkspaceRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = snapshot;

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            _snapshot = snapshot;
            return Task.CompletedTask;
        }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_snapshot);
    }

    private sealed class DeferredWorkspaceManagementService(WorkspaceManagementState state) : IWorkspaceManagementService
    {
        private readonly TaskCompletionSource _loadStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _loadReleased = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource LoadStarted => _loadStarted;

        public Guid? ActiveWorkspaceId => state.ActiveWorkspaceId;

        public async Task<WorkspaceManagementState> LoadAsync(CancellationToken cancellationToken = default)
        {
            _loadStarted.TrySetResult();
            await _loadReleased.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return state;
        }

        public void ReleaseLoad() => _loadReleased.TrySetResult();

        public Task<WorkspaceManagementResult> CreateWorkspaceAsync(WorkspaceManagementCreateRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<WorkspaceManagementResult> UpdateWorkspaceAsync(WorkspaceManagementUpdateRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<WorkspaceManagementResult> ArchiveWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<WorkspaceManagementResult> RestoreWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<WorkspaceManagementResult> DeleteWorkspaceAsync(WorkspaceManagementDeleteRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<WorkspaceManagementResult> SelectWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FailingWorkspaceManagementService : IWorkspaceManagementService
    {
        private readonly FailingOperation _operation;
        private readonly WorkspaceManagementState _state;

        public FailingWorkspaceManagementService(
            FailingOperation operation,
            WorkspaceManagementState? state = null)
        {
            _operation = operation;
            _state = state ?? new WorkspaceManagementState([], [], null, null, NeedsFirstWorkspace: true);
        }

        public Guid? ActiveWorkspaceId => _state.ActiveWorkspaceId;

        public Task<WorkspaceManagementState> LoadAsync(CancellationToken cancellationToken = default) =>
            _operation == FailingOperation.Load
                ? Task.FromException<WorkspaceManagementState>(new IOException("load failed"))
                : Task.FromResult(_state);

        public Task<WorkspaceManagementResult> CreateWorkspaceAsync(WorkspaceManagementCreateRequest request, CancellationToken cancellationToken = default) =>
            FailIf(FailingOperation.Create);

        public Task<WorkspaceManagementResult> UpdateWorkspaceAsync(WorkspaceManagementUpdateRequest request, CancellationToken cancellationToken = default) =>
            FailIf(FailingOperation.Update);

        public Task<WorkspaceManagementResult> ArchiveWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            FailIf(FailingOperation.Archive);

        public Task<WorkspaceManagementResult> RestoreWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            FailIf(FailingOperation.Restore);

        public Task<WorkspaceManagementResult> DeleteWorkspaceAsync(WorkspaceManagementDeleteRequest request, CancellationToken cancellationToken = default) =>
            FailIf(FailingOperation.Delete);

        public Task<WorkspaceManagementResult> SelectWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            FailIf(FailingOperation.Select);

        private Task<WorkspaceManagementResult> FailIf(FailingOperation expected)
        {
            if (_operation == expected)
            {
                return Task.FromException<WorkspaceManagementResult>(new IOException($"{expected.ToString().ToLowerInvariant()} failed"));
            }

            return Task.FromResult(WorkspaceManagementResult.Failure("Unexpected operation.", _state));
        }
    }
}
