using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FusionCanvas.App.Workspace;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Views;
using FusionCanvas.App.Workflow;
using FusionCanvas.Application.StageTools;
using FusionCanvas.Application.ToolContexts;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.Workspaces.Transfer;
using FusionCanvas.Application.WorkflowNavigation;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests;

public class WorkspaceTransferViewTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);

    [AvaloniaFact]
    public async Task WorkspaceDialog_ExposesImportAndSelectedWorkspaceExport()
    {
        var workspace = NewWorkspace();
        var viewModel = CreateViewModel(workspace, new ImmediateTransferService());
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        var window = new WorkspaceManagementWindow { DataContext = viewModel };
        try
        {
            window.Show();
            window.UpdateLayout();

            var import = window.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "ImportWorkspaceButton");
            var export = window.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "ExportWorkspaceButton");
            Assert.True(import.IsEnabled);
            Assert.True(export.IsEnabled);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task WorkspaceDialog_ClickingAnotherWorkspaceChangesSelection()
    {
        var personal = NewWorkspace("Personal");
        var client = NewWorkspace("Client");
        var viewModel = CreateViewModel(
            personal,
            new ImmediateTransferService(),
            snapshot: new WorkspaceSnapshot([personal, client], [], [], [], [], [], [], [], [], []));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        var window = new WorkspaceManagementWindow { DataContext = viewModel };
        try
        {
            window.Show();
            window.UpdateLayout();

            var clientButton = window.GetVisualDescendants()
                .OfType<Button>()
                .Single(button => Equals(button.Content, client.Name));

            clientButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            await WaitForAsync(
                () => viewModel.SelectedWorkspace?.Id == client.Id,
                TestContext.Current.CancellationToken);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task TransferInProgress_DisablesActionsAndOffersCancel()
    {
        var workspace = NewWorkspace();
        var transfer = new BlockingTransferService();
        var viewModel = CreateViewModel(workspace, transfer, new FixedPicker("package.fcworkspace"));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        var window = new WorkspaceManagementWindow { DataContext = viewModel };
        try
        {
            window.Show();
            var operation = viewModel.ImportWorkspaceAsync(TestContext.Current.CancellationToken);
            await transfer.Started.Task.WaitAsync(TestContext.Current.CancellationToken);
            window.UpdateLayout();

            var import = window.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "ImportWorkspaceButton");
            var export = window.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "ExportWorkspaceButton");
            var cancel = window.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "CancelTransferButton");
            Assert.False(import.IsEnabled);
            Assert.False(export.IsEnabled);
            Assert.True(cancel.IsVisible);
            Assert.True(cancel.IsEnabled);

            cancel.Command!.Execute(cancel.CommandParameter);
            await operation;
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MainWindow_NoWorkspaceOverlayShowsImportOnlyForNoWorkspaceState()
    {
        using var emptyFixture = new TransferMainWindowFixture(WorkspaceSnapshot.Empty);
        var emptyImport = emptyFixture.Window.GetVisualDescendants()
            .OfType<Button>()
            .Single(button => button.Name == "NoWorkspaceImportButton");
        Assert.DoesNotContain(emptyImport.GetVisualAncestors(), ancestor => ancestor is Control { IsVisible: false });

        var workspace = NewWorkspace();
        using var populatedFixture = new TransferMainWindowFixture(
            new WorkspaceSnapshot([workspace], [], [], [], [], [], [], [], [], []));
        var populatedImport = populatedFixture.Window.GetVisualDescendants()
            .OfType<Button>()
            .Single(button => button.Name == "NoWorkspaceImportButton");
        Assert.Contains(populatedImport.GetVisualAncestors(), ancestor => ancestor is Control { IsVisible: false });
    }

    private static WorkspaceManagementViewModel CreateViewModel(
        FusionCanvas.Domain.Workspace.Workspace workspace,
        IWorkspaceTransferService transfer,
        IWorkspacePackagePicker? picker = null,
        WorkspaceSnapshot? snapshot = null)
    {
        var repository = new InMemoryRepository(
            snapshot ?? new WorkspaceSnapshot([workspace], [], [], [], [], [], [], [], [], []));
        return new WorkspaceManagementViewModel(
            new WorkspaceManagementService(repository, () => Now),
            transfer,
            picker ?? new NullWorkspacePackagePicker());
    }

    private static FusionCanvas.Domain.Workspace.Workspace NewWorkspace() =>
        new(Guid.NewGuid(), "Personal", null, false, Now, Now, "{}");

    private static FusionCanvas.Domain.Workspace.Workspace NewWorkspace(string name) =>
        new(Guid.NewGuid(), name, null, false, Now, Now, "{}");

    private static async Task WaitForAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10, cancellationToken);
        }

        Assert.True(condition());
    }

    private sealed class TransferMainWindowFixture : IDisposable
    {
        public TransferMainWindowFixture(WorkspaceSnapshot snapshot)
        {
            var repository = new InMemoryRepository(snapshot);
            var viewModel = new MainWindowViewModel(
                new WorkflowStageNavigatorViewModel(new WorkflowStageNavigatorService()),
                new DocumentWindowViewModel(),
                new ToolContextResolver(),
                new StageToolHostService(
                    BuiltInStageTools.CreateDefaultRegistry(),
                    new ToolContextResolver()),
                repository,
                snapshot);
            Window = new MainWindow { DataContext = viewModel };
            Window.Show();
            Window.UpdateLayout();
        }

        public MainWindow Window { get; }

        public void Dispose()
        {
            Window.Close();
            Window.DataContext = null;
        }
    }

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

    private sealed record FixedPicker(string ImportPath) : IWorkspacePackagePicker
    {
        public Task<string?> PickExportDestinationAsync(string suggestedFileName, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task<string?> PickImportPackageAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(ImportPath);
    }

    private sealed class ImmediateTransferService : IWorkspaceTransferService
    {
        public Task<WorkspaceTransferResult> ExportWorkspaceAsync(WorkspaceExportRequest request, IProgress<WorkspaceTransferProgress>? progress = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorkspaceTransferResult.Failure("not invoked"));

        public Task<WorkspaceTransferResult> ImportWorkspaceAsync(WorkspaceImportRequest request, IProgress<WorkspaceTransferProgress>? progress = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorkspaceTransferResult.Failure("not invoked"));
    }

    private sealed class BlockingTransferService : IWorkspaceTransferService
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<WorkspaceTransferResult> ExportWorkspaceAsync(WorkspaceExportRequest request, IProgress<WorkspaceTransferProgress>? progress = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorkspaceTransferResult.Failure("not invoked"));

        public async Task<WorkspaceTransferResult> ImportWorkspaceAsync(WorkspaceImportRequest request, IProgress<WorkspaceTransferProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            Started.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return WorkspaceTransferResult.CancelledResult();
            }

            return WorkspaceTransferResult.CancelledResult();
        }
    }
}
