using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Workspace;
using Microsoft.Data.Sqlite;

namespace FusionCanvas.Integration.Tests;

public class AssetManagementPersistenceTests
{
    [Fact]
    public async Task ImportRelabelAndRemoveRoundTripThroughSqliteAndManagedStorage()
    {
        using var directory = new TestDirectory();
        var repository = new SqliteWorkspaceRepository(Path.Combine(directory.Path, "workspace.db"));
        var fileStore = new LocalWorkspaceFileStore(Path.Combine(directory.Path, "workspace-files"));
        var now = DateTimeOffset.UtcNow;
        var nicheId = Guid.NewGuid();
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
        var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
        var group = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Group", null, false, now, now, "{}");
        var listing = new Item(Guid.NewGuid(), store.Id, niche.Id, group.Id, "Idea", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        await repository.SaveAsync(new WorkspaceSnapshot([store], [niche], [group], [listing], [], [], [], [], []), TestContext.Current.CancellationToken);

        var sourcePath = Path.Combine(directory.Path, "source.svg");
        await File.WriteAllTextAsync(sourcePath, "<svg />", TestContext.Current.CancellationToken);
        var assetId = Guid.NewGuid();
        var service = new AssetManagementService(repository, fileStore, clock: () => now.AddMinutes(1), newId: () => assetId);

        var context = new AssetContextReference(WorkspaceEntityKind.Item, listing.Id);
        var imported = await service.ImportAssetAsync(new(context, sourcePath, AssetKind.Svg), TestContext.Current.CancellationToken);
        var reloadedAfterImport = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(imported.Succeeded, imported.Error);
        Assert.Equal(assetId, imported.Asset!.Id);
        Assert.Single(reloadedAfterImport.Assets);
        Assert.Single(reloadedAfterImport.AssetLinks);
        var stored = reloadedAfterImport.Assets.Single();
        Assert.Equal("source.svg", stored.Name);
        Assert.Equal(AssetKind.Svg, stored.Kind);
        Assert.True(fileStore.Exists(stored.WorkspaceRelativePath));
        Assert.True(File.Exists(Path.Combine(fileStore.WorkspaceRoot, stored.WorkspaceRelativePath)));

        var relabeled = await service.RelabelAssetAsync(new(assetId, AssetKind.ReferenceImage), TestContext.Current.CancellationToken);
        var reloadedAfterRelabel = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(relabeled.Succeeded, relabeled.Error);
        Assert.Equal(AssetKind.ReferenceImage, relabeled.Asset!.Kind);
        Assert.Equal(AssetKind.ReferenceImage, reloadedAfterRelabel.Assets.Single().Kind);
        Assert.True(fileStore.Exists(stored.WorkspaceRelativePath));

        var removed = await service.RemoveAssetAsync(new(assetId, ConfirmPermanentRemoval: true), TestContext.Current.CancellationToken);
        var reloadedAfterRemoval = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(removed.Succeeded, removed.Error);
        Assert.Empty(reloadedAfterRemoval.Assets);
        Assert.Empty(reloadedAfterRemoval.AssetLinks);
        Assert.False(fileStore.Exists(stored.WorkspaceRelativePath));
    }

    [Fact]
    public async Task FailedSaveLeavesDatabaseAndManagedFileUnchanged()
    {
        using var directory = new TestDirectory();
        var inner = new SqliteWorkspaceRepository(Path.Combine(directory.Path, "workspace.db"));
        var fileStore = new LocalWorkspaceFileStore(Path.Combine(directory.Path, "workspace-files"));
        var now = DateTimeOffset.UtcNow;
        var nicheId = Guid.NewGuid();
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}", nicheId);
        var niche = new Niche(nicheId, store.Id, "Niche", null, false, now, now, "{}");
        var listing = new Item(Guid.NewGuid(), store.Id, niche.Id, null, "Idea", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
        await inner.SaveAsync(new WorkspaceSnapshot([store], [niche], [], [listing], [], [], [], [], []), TestContext.Current.CancellationToken);
        var service = new AssetManagementService(new FailingRepository(inner), fileStore);

        var sourcePath = Path.Combine(directory.Path, "source.png");
        await File.WriteAllTextAsync(sourcePath, "bytes", TestContext.Current.CancellationToken);
        var result = await service.ImportAssetAsync(new(new AssetContextReference(WorkspaceEntityKind.Item, listing.Id), sourcePath, AssetKind.ExportedImage), TestContext.Current.CancellationToken);
        var reloaded = await inner.LoadAsync(TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Empty(reloaded.Assets);
        Assert.Empty(reloaded.AssetLinks);
        Assert.Empty(Directory.GetFiles(Path.Combine(directory.Path, "workspace-files"), "*", SearchOption.AllDirectories));
    }

    private sealed class FailingRepository(IWorkspaceRepository inner) : IWorkspaceRepository
    {
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => inner.LoadAsync(cancellationToken);
        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default) => throw new IOException("save failed");
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fusioncanvas-asset-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(Path, true);
        }
    }
}
