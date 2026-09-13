using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using FusionCanvas.App.Assets;
using FusionCanvas.App.Tests.TestSupport;
using FusionCanvas.App.Tests.TestSupport.Drivers;
using FusionCanvas.Application.Assets;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Files;

namespace FusionCanvas.App.Tests;

public sealed class AssetsWindowHeadlessTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 13, 12, 0, 0, TimeSpan.Zero);

    [AvaloniaFact]
    public async Task StoreAssetJourney_ImportsRelabelsPreviewsCancelsRemovalAndRehydrates()
    {
        using var workspace = new DisposableHeadlessWorkspace();
        var sourceImage = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/FusionCanvas.App/Assets/FusionCanvasLogo_Square.png"));
        var store = new Store(Guid.NewGuid(), "North Star", null, false, Now, Now, "{}");
        var item = new Item(Guid.NewGuid(), store.Id, null, null, "Idea", null, ItemStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");
        var existing = new Asset(Guid.NewGuid(), store.Id, "existing.png", null, AssetKind.ExportedImage, "assets/existing.png", sourceImage, false, false, Now, Now, "{}");
        var texture = new Asset(Guid.NewGuid(), store.Id, "texture.png", null, AssetKind.Texture, "assets/texture.png", sourceImage, false, false, Now, Now, "{}");
        var snapshot = new WorkspaceSnapshot(
            [store], [], [], [item], [existing, texture], [], [], [],
            [new AssetLink(existing.Id, WorkspaceEntityKind.Item, item.Id)]);
        var repository = workspace.CreateRepository();
        await repository.SaveAsync(snapshot, TestContext.Current.CancellationToken);
        SeedManagedFile(workspace.WorkspacePath, existing.WorkspaceRelativePath, sourceImage);
        SeedManagedFile(workspace.WorkspacePath, texture.WorkspaceRelativePath, sourceImage);

        var picker = new DeterministicAssetFilePicker(sourceImage);
        var service = Compose(repository, workspace.WorkspacePath);
        var viewModel = new AssetsViewModel(service, picker);
        await viewModel.OpenForContextAsync(new AssetContextReference(WorkspaceEntityKind.Store, store.Id), TestContext.Current.CancellationToken);
        var window = new AssetsWindow { DataContext = viewModel };
        window.Show();
        window.UpdateLayout();
        var driver = new AssetsWindowDriver(window);

        try
        {
            Assert.Equal("North Star", viewModel.ContextTitle);
            Assert.Equal(2, driver.Rows.Count);
            Assert.False(viewModel.IsEmpty);

            driver.ImportFile();
            await HeadlessUiWait.UntilAsync(() => viewModel.HasImportPending, "import confirmation becomes visible");
            window.UpdateLayout();
            Assert.Equal("FusionCanvasLogo_Square.png", viewModel.PendingImportFileName);
            Assert.Equal(AssetKind.ExportedImage, viewModel.PendingImportPurpose!.Kind);
            driver.ConfirmImport();
            await HeadlessUiWait.UntilAsync(() => !viewModel.HasImportPending && !viewModel.IsBusy, "import completes");
            window.UpdateLayout();

            var imported = Assert.Single(driver.Rows, row => row.Name == "FusionCanvasLogo_Square.png");
            var importedId = imported.Id;
            Assert.Same(viewModel.SelectedAsset, imported);
            Assert.Equal(AssetKind.ExportedImage, imported.Purpose);
            Assert.True(imported.CanPreview);
            Assert.Equal(3, driver.Rows.Count);

            driver.SelectPurpose(imported, AssetKind.ReferenceImage);
            await HeadlessUiWait.UntilAsync(
                () => !viewModel.IsBusy && driver.Rows.Single(row => row.Id == importedId).Purpose == AssetKind.ReferenceImage,
                "purpose relabel completes");
            imported = driver.Rows.Single(row => row.Id == importedId);
            Assert.Equal(AssetKind.ReferenceImage, imported.Purpose);
            Assert.StartsWith("FusionCanvasLogo_Square-", imported.ManagedFileName, StringComparison.Ordinal);
            Assert.EndsWith(".png", imported.ManagedFileName, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("Store", imported.ContextLabel);
            Assert.Equal(importedId, viewModel.SelectedAsset!.Id);

            var preview = await driver.OpenPreviewAsync(imported);
            try
            {
                Assert.Same(imported, preview.DataContext);
                Assert.Equal("Asset preview", preview.Title);
                Assert.IsType<Bitmap>(preview.GetVisualDescendants().OfType<Image>().Single().Source);
            }
            finally
            {
                driver.ClosePreview(preview);
                await HeadlessUiWait.UntilAsync(() => !preview.IsVisible, "asset preview closes");
            }

            var managedReference = (await repository.LoadAsync(TestContext.Current.CancellationToken)).Assets.Single(asset => asset.Id == importedId).WorkspaceRelativePath;
            var managedFullPath = Path.Combine(workspace.WorkspacePath, managedReference.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(managedFullPath));
            var selectedPurpose = imported.Purpose;
            driver.RequestRemoval(imported);
            await HeadlessUiWait.UntilAsync(() => driver.IsRemovalConfirmationVisible, "removal confirmation becomes visible");
            driver.CancelRemoval();
            await HeadlessUiWait.UntilAsync(() => !driver.IsRemovalConfirmationVisible, "removal confirmation cancels");
            Assert.Contains(driver.Rows, row => row.Id == importedId && row.Purpose == selectedPurpose);
            Assert.Equal(importedId, viewModel.SelectedAsset!.Id);
            Assert.True(File.Exists(managedFullPath));

            driver.CloseSurface();
            await HeadlessUiWait.UntilAsync(() => !viewModel.IsOpen, "asset surface closes");
        }
        finally
        {
            foreach (var preview in window.OwnedWindows.OfType<AssetPreviewWindow>())
                preview.Close();
            window.Close();
        }

        var reopenedRepository = workspace.CreateRepository();
        var reopenedService = Compose(reopenedRepository, workspace.WorkspacePath);
        var reopenedViewModel = new AssetsViewModel(reopenedService, picker);
        await reopenedViewModel.OpenForContextAsync(new AssetContextReference(WorkspaceEntityKind.Store, store.Id), TestContext.Current.CancellationToken);
        var reopenedWindow = new AssetsWindow { DataContext = reopenedViewModel };
        reopenedWindow.Show();
        reopenedWindow.UpdateLayout();
        try
        {
            Assert.False(reopenedViewModel.IsBusy);
            Assert.False(reopenedViewModel.IsEmpty);
            var reopenedImported = reopenedViewModel.Assets.Single(row => row.Id == viewModel.SelectedAsset!.Id);
            Assert.Equal(AssetKind.ReferenceImage, reopenedImported.Purpose);
            Assert.Equal("FusionCanvasLogo_Square.png", reopenedImported.Name);
            var reopenedSnapshot = await reopenedRepository.LoadAsync(TestContext.Current.CancellationToken);
            var reopenedReference = reopenedSnapshot.Assets.Single(asset => asset.Id == reopenedImported.Id).WorkspaceRelativePath;
            Assert.True(File.Exists(Path.Combine(workspace.WorkspacePath, reopenedReference.Replace('/', Path.DirectorySeparatorChar))));
        }
        finally
        {
            reopenedWindow.Close();
        }
    }

    private static AssetManagementService Compose(IWorkspaceRepository repository, string workspaceRoot) =>
        new(repository, new LocalWorkspaceFileStore(workspaceRoot));

    private static void SeedManagedFile(string workspaceRoot, string relativePath, string sourcePath)
    {
        var destination = Path.Combine(workspaceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Copy(sourcePath, destination, overwrite: true);
    }

    private sealed class DeterministicAssetFilePicker(string? path) : IAssetFilePicker
    {
        public Task<string?> PickImportFileAsync(CancellationToken cancellationToken = default) => Task.FromResult(path);
    }
}
