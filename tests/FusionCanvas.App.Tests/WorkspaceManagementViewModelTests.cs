using FusionCanvas.App.Workspace;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

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
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([personal], [], [], [], [], [], [], [], [], [], []));
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
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot([workspace, personal], [store], [], [], [], [], [], [], [], [], []));
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

    private static WorkspaceManagementViewModel NewViewModel(WorkspaceSnapshot snapshot) =>
        NewViewModel(new InMemoryWorkspaceRepository(snapshot));

    private static WorkspaceManagementViewModel NewViewModel(InMemoryWorkspaceRepository repository) =>
        new(new WorkspaceManagementService(repository, () => Now));

    private static FusionCanvas.Domain.Workspace.Workspace NewWorkspace(string name) =>
        new(Guid.NewGuid(), name, null, false, Now, Now, "{}");

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
}
