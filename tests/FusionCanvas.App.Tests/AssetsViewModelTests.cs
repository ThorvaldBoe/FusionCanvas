using FusionCanvas.App.Assets;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;

namespace FusionCanvas.App.Tests;

public class AssetsViewModelTests
{
    [Fact]
    public async Task OpenForContext_LoadsAssetsAndContextHeader()
    {
        var sample = Sample.Create();
        var service = sample.Service();
        var viewModel = new AssetsViewModel(service, new FakeFilePicker());

        await viewModel.OpenForContextAsync(sample.ItemContext);

        Assert.True(viewModel.IsOpen);
        Assert.Equal("Idea", viewModel.ContextTitle);
        Assert.Equal("Item", viewModel.ContextKindLabel);
        Assert.Single(viewModel.Assets);
        Assert.False(viewModel.HasImportPending);
        Assert.True(viewModel.CanImport);
    }

    [Fact]
    public async Task ImportFlow_PicksFilePreselectsPurposeConfirmsAndSelectsNewAsset()
    {
        var sample = Sample.Create();
        var service = sample.Service(nextId: Guid.NewGuid());
        var picker = new FakeFilePicker().WithFile(@"C:\imports\design.svg");
        var viewModel = new AssetsViewModel(service, picker);

        await viewModel.OpenForContextAsync(sample.ItemContext);
        viewModel.ImportCommand.Execute(null);
        await WaitForAsync(() => viewModel.HasImportPending);

        Assert.True(viewModel.HasImportPending);
        Assert.Equal("design.svg", viewModel.PendingImportFileName);
        Assert.Equal(AssetKind.Svg, viewModel.PendingImportPurpose!.Kind);
        Assert.False(viewModel.CanImport);

        viewModel.ConfirmImportCommand.Execute(null);
        await WaitForAsync(() => !viewModel.HasImportPending && !viewModel.IsBusy);

        Assert.False(viewModel.HasImportPending);
        Assert.Equal(2, viewModel.Assets.Count);
        Assert.NotNull(viewModel.SelectedAsset);
        Assert.Equal(AssetKind.Svg, viewModel.SelectedAsset!.Purpose);
    }

    [Fact]
    public async Task ImportCancel_ClearsPendingWithoutPersisting()
    {
        var sample = Sample.Create();
        var service = sample.Service();
        var picker = new FakeFilePicker().WithFile(@"C:\imports\design.svg");
        var viewModel = new AssetsViewModel(service, picker);

        await viewModel.OpenForContextAsync(sample.ItemContext);
        viewModel.ImportCommand.Execute(null);
        await WaitForAsync(() => viewModel.HasImportPending);
        viewModel.CancelImportCommand.Execute(null);

        Assert.False(viewModel.HasImportPending);
        Assert.Single(viewModel.Assets);
    }

    [Fact]
    public async Task PickerCancel_LeavesListUnchanged()
    {
        var sample = Sample.Create();
        var service = sample.Service();
        var picker = new FakeFilePicker();
        var viewModel = new AssetsViewModel(service, picker);

        await viewModel.OpenForContextAsync(sample.ItemContext);
        viewModel.ImportCommand.Execute(null);
        await WaitForAsync(() => !viewModel.IsBusy);

        Assert.False(viewModel.HasImportPending);
        Assert.Single(viewModel.Assets);
    }

    [Fact]
    public async Task Relabel_PersistsNewPurposeAndRevertsOnError()
    {
        var sample = Sample.Create();
        var repository = new Repository(sample.Snapshot);
        var service = sample.Service(repository);
        var viewModel = new AssetsViewModel(service, new FakeFilePicker());

        await viewModel.OpenForContextAsync(sample.ItemContext);
        var row = viewModel.Assets.Single();
        Assert.Equal(AssetKind.ExportedImage, row.Purpose);

        var referenceOption = viewModel.AvailablePurposes.Single(option => option.Kind == AssetKind.ReferenceImage);
        row.SelectedPurpose = referenceOption;
        await WaitForAsync(() => !viewModel.IsBusy);

        Assert.Equal(AssetKind.ReferenceImage, repository.Snapshot.Assets.Single(asset => asset.Id == row.Id).Kind);
        Assert.Equal(AssetKind.ReferenceImage, row.Purpose);

        repository.FailSaves = true;
        var exportOption = viewModel.AvailablePurposes.Single(option => option.Kind == AssetKind.ExportedImage);
        row.SelectedPurpose = exportOption;
        await WaitForAsync(() => !viewModel.IsBusy);

        Assert.True(viewModel.HasError);
        Assert.Equal(AssetKind.ReferenceImage, repository.Snapshot.Assets.Single(asset => asset.Id == row.Id).Kind);
    }

    [Fact]
    public async Task Removal_RequiresConfirmationCancelsAndDeletesOnConfirm()
    {
        var sample = Sample.Create();
        var repository = new Repository(sample.Snapshot);
        var service = sample.Service(repository);
        var viewModel = new AssetsViewModel(service, new FakeFilePicker());

        await viewModel.OpenForContextAsync(sample.ItemContext);
        var row = viewModel.Assets.Single();

        viewModel.RequestRemoveCommand.Execute(row);
        Assert.True(viewModel.RemovalConfirmationVisible);

        viewModel.CancelRemoveCommand.Execute(null);
        Assert.False(viewModel.RemovalConfirmationVisible);
        Assert.Equal(2, repository.Snapshot.Assets.Count);

        viewModel.RequestRemoveCommand.Execute(row);
        viewModel.ConfirmRemoveCommand.Execute(null);
        await WaitForAsync(() => !viewModel.IsBusy);

        Assert.False(viewModel.RemovalConfirmationVisible);
        Assert.Single(repository.Snapshot.Assets);
        Assert.Empty(viewModel.Assets);
    }

    [Fact]
    public async Task StoreContext_ShowsAllStoreAssetsWithLabels()
    {
        var sample = Sample.Create();
        var service = sample.Service();
        var viewModel = new AssetsViewModel(service, new FakeFilePicker());

        await viewModel.OpenForContextAsync(sample.StoreContext);

        Assert.Equal(2, viewModel.Assets.Count);
        Assert.Equal("Item: Idea", viewModel.Assets.Single(row => row.Id == sample.LinkedAsset.Id).ContextLabel);
        Assert.Equal("—", viewModel.Assets.Single(row => row.Id == sample.UnlinkedAsset.Id).ContextLabel);
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        for (var i = 0; i < 200 && !condition(); i++) await Task.Delay(10);
        Assert.True(condition());
    }

    private sealed class FakeFilePicker : IAssetFilePicker
    {
        private string? _path;

        public FakeFilePicker WithFile(string path)
        {
            _path = path;
            return this;
        }

        public Task<string?> PickImportFileAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_path);
    }

    private sealed class Repository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = snapshot;
        public WorkspaceSnapshot Snapshot => _snapshot;
        public bool FailSaves { get; set; }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(_snapshot);
        public Task SaveAsync(WorkspaceSnapshot value, CancellationToken cancellationToken = default)
        {
            if (FailSaves) throw new IOException("save failed");
            _snapshot = value;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFileStore : IWorkspaceFileStore
    {
        private readonly HashSet<string> _existing = [];

        public string WorkspaceRoot => @"C:\workspace";

        public Task<ManagedWorkspaceFile> ImportAsync(string sourcePath, AssetKind kind, CancellationToken cancellationToken = default)
        {
            var relativePath = $"assets/{Path.GetFileName(sourcePath)}";
            _existing.Add(relativePath);
            return Task.FromResult(new ManagedWorkspaceFile(Path.GetFileName(sourcePath), kind, relativePath, Path.Combine(WorkspaceRoot, relativePath), sourcePath));
        }

        public bool Exists(string workspaceRelativePath) => _existing.Contains(workspaceRelativePath.Replace('\\', '/'));
        public bool TryDelete(string workspaceRelativePath) => _existing.Remove(workspaceRelativePath.Replace('\\', '/'));

        public Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream());

        public Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed record Sample(WorkspaceSnapshot Snapshot, DateTimeOffset Now, Store Store, Item Item, Asset LinkedAsset, Asset UnlinkedAsset)
    {
        public AssetContextReference ItemContext => new(WorkspaceEntityKind.Item, Item.Id);
        public AssetContextReference StoreContext => new(WorkspaceEntityKind.Store, Store.Id);

        public AssetManagementService Service(Repository? repository = null, Guid? nextId = null) =>
            new(repository ?? new Repository(Snapshot), new FakeFileStore(), clock: () => Now.AddMinutes(1), newId: () => nextId ?? Guid.NewGuid());

        public static Sample Create()
        {
            var now = DateTimeOffset.UtcNow;
            var nicheId = Guid.NewGuid();
            var store = new Store(Guid.NewGuid(), "North Star", null, false, now, now, "{}", nicheId);
            var niche = new Niche(nicheId, store.Id, "Coffee", null, false, now, now, "{}");
            var group = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Group", null, false, now, now, "{}");
            var listing = new Item(Guid.NewGuid(), store.Id, niche.Id, group.Id, "Idea", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
            var linkedAsset = new Asset(Guid.NewGuid(), store.Id, "linked.png", null, AssetKind.ExportedImage, "assets/linked.png", @"C:\imports\linked.png", false, false, now, now, "{}");
            var unlinkedAsset = new Asset(Guid.NewGuid(), store.Id, "unlinked.png", null, AssetKind.Texture, "assets/unlinked.png", @"C:\imports\unlinked.png", false, false, now, now, "{}");
            var link = new AssetLink(linkedAsset.Id, WorkspaceEntityKind.Item, listing.Id);
            var snapshot = new WorkspaceSnapshot([store], [niche], [group], [listing], [linkedAsset, unlinkedAsset], [], [], [], [link]);
            return new(snapshot, now, store, listing, linkedAsset, unlinkedAsset);
        }
    }
}
