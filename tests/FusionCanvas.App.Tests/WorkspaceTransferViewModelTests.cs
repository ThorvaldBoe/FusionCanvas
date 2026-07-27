using FusionCanvas.App.Workspace;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.Workspaces.Transfer;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests;

public class WorkspaceTransferViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TransferBusyState_DisablesMutationsAndCancelStopsOperation()
    {
        var workspace = NewWorkspace("Personal");
        var transfer = new FakeTransferService { WaitForCancellation = true };
        var viewModel = NewViewModel(workspace, transfer, new FakePicker(importPath: "package.fcworkspace"));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        var operation = viewModel.ImportWorkspaceAsync(TestContext.Current.CancellationToken);
        await transfer.Started.Task.WaitAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.IsTransferRunning);
        Assert.False(viewModel.CanImportWorkspace);
        Assert.False(viewModel.CanSaveSelectedWorkspace);
        Assert.False(viewModel.CanDeleteSelectedWorkspace);

        viewModel.CancelTransferCommand.Execute(null);
        await operation;

        Assert.False(viewModel.IsTransferRunning);
    }

    [Fact]
    public async Task ExportSuccess_SurfacesCompletionSummary()
    {
        var workspace = NewWorkspace("Personal");
        var transfer = new FakeTransferService
        {
            ExportResult = WorkspaceTransferResult.Success(workspace.Id, Summary(workspace.Name))
        };
        var viewModel = NewViewModel(workspace, transfer, new FakePicker(exportPath: "export.fcworkspace"));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        await viewModel.ExportSelectedWorkspaceAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.HasTransferSummary);
        Assert.Contains("1 records", viewModel.TransferSummary);
        Assert.Contains("1 files", viewModel.TransferSummary);
    }

    [Fact]
    public async Task ImportFailure_OpensManagementSurfaceAndKeepsError()
    {
        var transfer = new FakeTransferService
        {
            ImportResult = WorkspaceTransferResult.Failure("Package is invalid.")
        };
        var viewModel = NewViewModel(null, transfer, new FakePicker(importPath: "bad.fcworkspace"));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        await viewModel.ImportWorkspaceAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.IsWorkspaceManagementOpen);
        Assert.Equal("Package is invalid.", viewModel.ErrorMessage);
        Assert.True(viewModel.ShouldShowNoWorkspaceState);
    }

    [Fact]
    public async Task ImportSuccess_ReloadsAndSelectsRestoredWorkspace()
    {
        var imported = NewWorkspace("Restored");
        var repository = new InMemoryRepository(WorkspaceSnapshot.Empty);
        var transfer = new FakeTransferService
        {
            ImportResult = WorkspaceTransferResult.Success(imported.Id, Summary(imported.Name)),
            BeforeImportResult = () => repository.SaveAsync(
                new WorkspaceSnapshot([imported], [], [], [], [], [], [], [], [], []),
                TestContext.Current.CancellationToken)
        };
        var viewModel = new WorkspaceManagementViewModel(
            new WorkspaceManagementService(repository, () => Now),
            transfer,
            new FakePicker(importPath: "restored.fcworkspace"));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        await viewModel.ImportWorkspaceAsync(TestContext.Current.CancellationToken);

        Assert.Equal(imported.Id, viewModel.SelectedWorkspace?.Id);
        Assert.False(viewModel.ShouldShowNoWorkspaceState);
    }

    [Fact]
    public async Task ArchivedWorkspace_CanBeReviewedAndExportedWithoutRestoring()
    {
        var active = NewWorkspace("Active");
        var archived = NewWorkspace("Archived") with { IsArchived = true };
        var repository = new InMemoryRepository(
            new WorkspaceSnapshot([active, archived], [], [], [], [], [], [], [], [], []));
        var viewModel = new WorkspaceManagementViewModel(
            new WorkspaceManagementService(repository, () => Now),
            new FakeTransferService(),
            new FakePicker(exportPath: "archived.fcworkspace"));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.ReviewArchivedWorkspaceCommand.Execute(viewModel.ArchivedWorkspaces.Single());

        Assert.Equal(archived.Id, viewModel.SelectedWorkspace?.Id);
        Assert.True(viewModel.CanExportSelectedWorkspace);
        Assert.True(viewModel.CanRestoreSelectedWorkspace);
        Assert.False(viewModel.ShouldShowNoWorkspaceState);
    }

    private static WorkspaceManagementViewModel NewViewModel(
        FusionCanvas.Domain.Workspace.Workspace? workspace,
        IWorkspaceTransferService transfer,
        IWorkspacePackagePicker picker)
    {
        var snapshot = workspace is null
            ? WorkspaceSnapshot.Empty
            : new WorkspaceSnapshot([workspace], [], [], [], [], [], [], [], [], []);
        var repository = new InMemoryRepository(snapshot);
        return new WorkspaceManagementViewModel(
            new WorkspaceManagementService(repository, () => Now),
            transfer,
            picker);
    }

    private static WorkspaceTransferSummary Summary(string name) =>
        new(
            new Dictionary<string, int> { ["workspaces"] = 1 },
            1,
            0,
            0,
            [],
            [],
            0,
            name,
            name,
            []);

    private static FusionCanvas.Domain.Workspace.Workspace NewWorkspace(string name) =>
        new(Guid.NewGuid(), name, null, false, Now, Now, "{}");

    private sealed class InMemoryRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
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

    private sealed record FakePicker(string? exportPath = null, string? importPath = null) : IWorkspacePackagePicker
    {
        public Task<string?> PickExportDestinationAsync(string suggestedFileName, CancellationToken cancellationToken = default) =>
            Task.FromResult(exportPath);

        public Task<string?> PickImportPackageAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(importPath);
    }

    private sealed class FakeTransferService : IWorkspaceTransferService
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool WaitForCancellation { get; init; }

        public WorkspaceTransferResult ExportResult { get; init; } = WorkspaceTransferResult.Failure("not configured");

        public WorkspaceTransferResult ImportResult { get; init; } = WorkspaceTransferResult.Failure("not configured");

        public Func<Task>? BeforeImportResult { get; init; }

        public Task<WorkspaceTransferResult> ExportWorkspaceAsync(
            WorkspaceExportRequest request,
            IProgress<WorkspaceTransferProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            progress?.Report(new WorkspaceTransferProgress("Exporting", 1, 1));
            return Task.FromResult(ExportResult);
        }

        public async Task<WorkspaceTransferResult> ImportWorkspaceAsync(
            WorkspaceImportRequest request,
            IProgress<WorkspaceTransferProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Started.TrySetResult();
            if (WaitForCancellation)
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return WorkspaceTransferResult.CancelledResult();
                }
            }

            if (BeforeImportResult is not null)
            {
                await BeforeImportResult();
            }

            return ImportResult;
        }
    }
}
