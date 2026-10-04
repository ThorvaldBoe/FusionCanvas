using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.DesignFiles;
using FusionCanvas.Application.ContentRisk;
using FusionCanvas.Domain.ContentRisk;

namespace FusionCanvas.Application.Tests.DesignFiles;

public class DesignFileServiceTests
{
    [Fact]
    public async Task ListForItemAsync_ReturnsOnlyPngExportedImagesLinkedToItem()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var service = new DesignFileService(repository, new FakeFileStore());

        var files = await service.ListForItemAsync(sample.Item.Id, TestContext.Current.CancellationToken);

        Assert.Single(files);
        Assert.Equal(sample.PngAsset.Id, files[0].AssetId);
    }

    [Fact]
    public async Task ImportAsync_RejectsNonPngBeforeCopy()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var fileStore = new FakeFileStore();
        var service = new DesignFileService(repository, fileStore);

        var result = await service.ImportAsync(sample.Item.Id, "C:\\imports\\design.svg", TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("PNG", result.Error);
        Assert.Empty(fileStore.Imports);
    }

    [Fact]
    public async Task ImportAsync_ReportsMissingSourceWhenFileStoreCannotFindIt()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var fileStore = new FakeFileStore { ImportFailure = new FileNotFoundException("source missing") };
        var service = new DesignFileService(repository, fileStore);

        var result = await service.ImportAsync(sample.Item.Id, sample.SourcePngPath, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal("The selected source file was not found.", result.Error);
        Assert.Empty(fileStore.Imports);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task ImportAsync_ReportsFileStoreFailure()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var fileStore = new FakeFileStore { ImportFailure = new IOException("disk full") };
        var service = new DesignFileService(repository, fileStore);

        var result = await service.ImportAsync(sample.Item.Id, sample.SourcePngPath, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal("The Design file could not be imported. disk full", result.Error);
        Assert.Empty(fileStore.Imports);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task ImportAsync_RejectsPublishedItem()
    {
        var sample = Sample.Create();
        var published = sample.Item with { Status = ItemStatus.Published, Stage = WorkflowStage.Listing };
        var repository = new TestRepository(sample.Snapshot with { Items = [published] });
        var service = new DesignFileService(repository, new FakeFileStore());

        var result = await service.ImportAsync(published.Id, sample.SourcePngPath, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("Pause", result.Error);
    }

    [Fact]
    public async Task ImportAsync_PersistsAssetAndLink()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var fileStore = new FakeFileStore();
        var assetId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var importedAt = new DateTimeOffset(2026, 9, 30, 12, 34, 56, TimeSpan.Zero);
        var reviewService = new RecordingContentRiskReviewService();
        var service = new DesignFileService(repository, fileStore, () => importedAt, () => assetId, reviewService);

        var result = await service.ImportAsync(sample.Item.Id, sample.SourcePngPath, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(1, repository.SaveCount);
        Assert.Equal(assetId, result.File!.AssetId);
        var persisted = repository.Snapshot.Assets.Single(asset => asset.Id == result.File!.AssetId);
        Assert.Equal(AssetKind.ExportedImage, persisted.Kind);
        Assert.Equal(importedAt, persisted.CreatedAt);
        Assert.Equal(importedAt, persisted.UpdatedAt);
        Assert.Equal(sample.Item.Id, repository.Snapshot.AssetLinks.Single(link => link.AssetId == persisted.Id).EntityId);
        var review = Assert.Single(repository.Snapshot.ContentRiskReviews);
        Assert.Equal(assetId, review.Target.OwnerId);
        Assert.Equal(ContentRiskReviewState.Unreviewed, review.State);
        Assert.Equal(assetId, reviewService.ImageTargets.Single().OwnerId);
    }

    [Fact]
    public async Task ImportAsync_RollsBackManagedFileOnPersistenceFailure()
    {
        var sample = Sample.Create();
        var repository = new FailingRepository(sample.Snapshot);
        var fileStore = new FakeFileStore();
        var service = new DesignFileService(repository, fileStore);

        var result = await service.ImportAsync(sample.Item.Id, sample.SourcePngPath, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(1, fileStore.Imports.Count);
        Assert.Single(fileStore.Deletes);
    }

    [Fact]
    public async Task ImportAsync_CleansManagedFileAndPropagatesPersistenceCancellation()
    {
        var sample = Sample.Create();
        var cancellation = new OperationCanceledException("Workspace save was cancelled.");
        var repository = new FailingRepository(sample.Snapshot, cancellation);
        var fileStore = new FakeFileStore();
        var service = new DesignFileService(repository, fileStore);

        var thrown = await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.ImportAsync(sample.Item.Id, sample.SourcePngPath, TestContext.Current.CancellationToken));

        Assert.Same(cancellation, thrown);
        Assert.Single(fileStore.Deletes);
        Assert.Empty(fileStore.Existing);
        Assert.Equal(sample.Snapshot, repository.Snapshot);
    }

    [Fact]
    public async Task ImportAsync_ReportsFailedCleanupWhenFileCanBeReadButExistsCheckFails()
    {
        var sample = Sample.Create();
        var cancellation = new OperationCanceledException("Workspace save was cancelled.");
        var repository = new FailingRepository(sample.Snapshot, cancellation);
        var fileStore = new FakeFileStore { FailDeletes = true, ExistsOverride = false };
        var service = new DesignFileService(repository, fileStore);

        var thrown = await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.ImportAsync(sample.Item.Id, sample.SourcePngPath, TestContext.Current.CancellationToken));

        Assert.Same(cancellation, thrown);
        Assert.Equal("Failed", thrown.Data["FusionCanvas.ManagedWorkspaceFileCleanup.Status"]);
        Assert.Single(fileStore.Deletes);
        Assert.Equal(sample.Snapshot, repository.Snapshot);
    }

    [Fact]
    public async Task RemoveAsync_ArchivesRecordsAndDeletesManagedFile()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var fileStore = new FakeFileStore();
        fileStore.Existing.Add(sample.PngAsset.WorkspaceRelativePath);
        var service = new DesignFileService(repository, fileStore);

        var result = await service.RemoveAsync(sample.Item.Id, sample.PngAsset.Id, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Error);
        Assert.DoesNotContain(repository.Snapshot.Assets, asset => asset.Id == sample.PngAsset.Id);
        Assert.DoesNotContain(repository.Snapshot.AssetLinks, link => link.AssetId == sample.PngAsset.Id);
        Assert.Single(fileStore.Deletes);
    }

    [Fact]
    public async Task RemoveAsync_RejectsRejectedItem()
    {
        var sample = Sample.Create();
        var rejected = sample.Item with { Status = ItemStatus.Rejected };
        var repository = new TestRepository(sample.Snapshot with { Items = [rejected] });
        var service = new DesignFileService(repository, new FakeFileStore());

        var result = await service.RemoveAsync(rejected.Id, sample.PngAsset.Id, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
    }

    private sealed class TestRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = snapshot;
        public WorkspaceSnapshot Snapshot => _snapshot;
        public int SaveCount { get; private set; }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(_snapshot);

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            _snapshot = snapshot;
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FailingRepository(WorkspaceSnapshot snapshot, Exception? failure = null) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot => snapshot;
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(snapshot);
        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default) => throw failure ?? new InvalidOperationException("Persistence failed.");
    }

    private sealed class FakeFileStore : IWorkspaceFileStore
    {
        public string WorkspaceRoot => "C:/workspace";
        public string ResolvePath(string workspaceRelativePath) => Path.Combine(WorkspaceRoot, workspaceRelativePath);
        public Exception? ImportFailure { get; init; }
        public bool FailDeletes { get; init; }
        public bool? ExistsOverride { get; init; }
        public List<string> Imports { get; } = [];
        public List<string> Deletes { get; } = [];
        public HashSet<string> Existing { get; } = [];

        public Task<ManagedWorkspaceFile> ImportAsync(string sourcePath, AssetKind kind, CancellationToken cancellationToken = default)
        {
            if (ImportFailure is not null)
            {
                throw ImportFailure;
            }

            Imports.Add(sourcePath);
            var relative = $"assets/{Guid.NewGuid():N}.png";
            Existing.Add(relative);
            return Task.FromResult(new ManagedWorkspaceFile(Path.GetFileName(sourcePath), kind, relative, $"C:/workspace/{relative}", sourcePath));
        }

        public bool Exists(string workspaceRelativePath) => ExistsOverride ?? Existing.Contains(workspaceRelativePath.Replace('\\', '/'));
        public bool TryDelete(string workspaceRelativePath)
        {
            Deletes.Add(workspaceRelativePath);
            if (FailDeletes)
            {
                return false;
            }

            Existing.Remove(workspaceRelativePath.Replace('\\', '/'));
            return true;
        }

        public Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes("preview");
            return Task.FromResult<Stream>(new MemoryStream(bytes));
        }

        public Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RecordingContentRiskReviewService : IContentRiskReviewService
    {
        public List<ContentRiskReviewTarget> ImageTargets { get; } = [];

        public Task<ContentRiskReview> ReviewTextAsync(ContentRiskReviewTarget target, string text, CancellationToken cancellationToken = default) =>
            Task.FromResult(ContentRiskReview.Unreviewed(target, ContentRiskFingerprint.ForText(target, text)));

        public Task<ContentRiskReview> ReviewImageAsync(ContentRiskReviewTarget target, string mediaType, byte[] imageBytes, CancellationToken cancellationToken = default)
        {
            ImageTargets.Add(target);
            return Task.FromResult(ContentRiskReview.Unreviewed(target, ContentRiskFingerprint.ForImage(target, imageBytes)));
        }

        public Task<ContentRiskReview> EnsureUnreviewedAsync(ContentRiskReviewTarget target, string fingerprint, CancellationToken cancellationToken = default) =>
            Task.FromResult(ContentRiskReview.Unreviewed(target, fingerprint));

        public Task<ContentRiskReview?> FindAsync(ContentRiskReviewTarget target, string fingerprint, CancellationToken cancellationToken = default) =>
            Task.FromResult<ContentRiskReview?>(null);
    }

    private sealed record Sample(WorkspaceSnapshot Snapshot, Item Item, Asset PngAsset, string SourcePngPath)
    {
        public static Sample Create()
        {
            var now = DateTimeOffset.UtcNow;
            var nicheId = Guid.NewGuid();
            var store = new Store(Guid.NewGuid(), "Studio", null, false, now, now, "{}", nicheId);
            var niche = new Niche(nicheId, store.Id, "Coffee", null, false, now, now, "{}");
            var item = new Item(Guid.NewGuid(), store.Id, niche.Id, null, "Mug design", null, ItemStatus.Draft, WorkflowStage.Design, false, now, now, "{}");
            var pngAsset = new Asset(Guid.NewGuid(), store.Id, "design.png", null, AssetKind.ExportedImage, "assets/design.png", null, false, false, now, now, "{}");
            var link = new AssetLink(pngAsset.Id, WorkspaceEntityKind.Item, item.Id);
            var snapshot = new WorkspaceSnapshot([store], [niche], [], [item], [pngAsset], [], [], [], [link]);
            return new(snapshot, item, pngAsset, "C:/imports/design.png");
        }
    }
}
