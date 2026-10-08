using FusionCanvas.Application.DesignFiles;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests.DesignFiles;

public sealed class GlobalColorRemovalServiceTests
{
    [Fact]
    public async Task ApplyAsync_CreatesDerivedAssetAndPreservesSource()
    {
        var fixture = Fixture.Create();
        var repository = new MemoryRepository(fixture.Snapshot);
        var files = new MemoryFiles();
        var processor = new StubProcessor
        {
            ApplyResult = new GlobalColorRemovalRasterResult([1, 2, 3], 2, 2, 2, 4)
        };
        var service = new GlobalColorRemovalService(
            repository,
            files,
            files,
            processor,
            clock: () => fixture.Now,
            newId: () => fixture.DerivedAssetId);

        var result = await service.ApplyAsync(
            fixture.Item.Id,
            fixture.SourceAsset.Id,
            new GlobalColorRemovalParameters(new(0, 0, 0), 0.1),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(fixture.DerivedAssetId, result.Asset!.AssetId);
        Assert.Equal(2, result.MatchedPixelCount);
        Assert.Contains(fixture.Snapshot.Assets, asset => asset.Id == fixture.SourceAsset.Id);
        var derived = Assert.Single(repository.Snapshot.Assets, asset => asset.Id == fixture.DerivedAssetId);
        Assert.Equal(AssetKind.ExportedImage, derived.Kind);
        Assert.Contains(repository.Snapshot.AssetLinks, link => link.AssetId == fixture.DerivedAssetId && link.EntityId == fixture.Item.Id);
        Assert.Equal("assets/design - color removed.png", result.Asset.WorkspaceRelativePath);
        Assert.Equal(1, files.SaveCount);
    }

    [Fact]
    public async Task ApplyAsync_ReplacesTheSourceInTheItemDesignSlot()
    {
        var fixture = Fixture.Create();
        var rowId = Guid.NewGuid();
        var designAreaId = Guid.NewGuid();
        var repository = new MemoryRepository(fixture.Snapshot with
        {
            DesignVariantRows = [new DesignVariantRow(rowId, fixture.Item.Id, true, 0)],
            DesignSlotAssignments = [new DesignSlotAssignment(rowId, designAreaId, fixture.SourceAsset.Id)]
        });
        var files = new MemoryFiles();
        var service = new GlobalColorRemovalService(
            repository,
            files,
            files,
            new StubProcessor
            {
                ApplyResult = new GlobalColorRemovalRasterResult([1, 2, 3], 2, 2, 2, 4)
            },
            clock: () => fixture.Now,
            newId: () => fixture.DerivedAssetId);

        var result = await service.ApplyAsync(
            fixture.Item.Id,
            fixture.SourceAsset.Id,
            new GlobalColorRemovalParameters(new(0, 0, 0), 0.1),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var assignment = Assert.Single(repository.Snapshot.DesignSlotAssignments);
        Assert.Equal(fixture.DerivedAssetId, assignment.AssetId);
        Assert.Contains(repository.Snapshot.Assets, asset => asset.Id == fixture.SourceAsset.Id);
    }

    [Fact]
    public async Task CheckAvailabilityAsync_ReturnsActionableFailureWhenRasterValidationFails()
    {
        var fixture = Fixture.Create();
        var processor = new StubProcessor
        {
            ValidationFailure = new InvalidDataException("The selected file is not a supported raster image.")
        };
        var service = new GlobalColorRemovalService(
            new MemoryRepository(fixture.Snapshot),
            new MemoryFiles(),
            new MemoryFiles(),
            processor);

        var result = await service.CheckAvailabilityAsync(
            fixture.Item.Id,
            fixture.SourceAsset.Id,
            TestContext.Current.CancellationToken);

        Assert.False(result.Available);
        Assert.Contains("supported raster image", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyAsync_RejectsWhenAllVisibleArtworkWouldBeRemoved()
    {
        var fixture = Fixture.Create();
        var repository = new MemoryRepository(fixture.Snapshot);
        var files = new MemoryFiles();
        var service = new GlobalColorRemovalService(
            repository,
            files,
            files,
            new StubProcessor
            {
                ApplyResult = new GlobalColorRemovalRasterResult([1], 1, 1, 4, 4)
            });

        var result = await service.ApplyAsync(
            fixture.Item.Id,
            fixture.SourceAsset.Id,
            new GlobalColorRemovalParameters(new(0, 0, 0), 1),
            TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("all visible artwork", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, files.SaveCount);
        Assert.Equal(fixture.Snapshot, repository.Snapshot);
    }

    [Fact]
    public async Task ApplyAsync_CleansOutputWhenWorkspacePersistenceFails()
    {
        var fixture = Fixture.Create();
        var repository = new MemoryRepository(fixture.Snapshot) { SaveFailure = new IOException("database unavailable") };
        var files = new MemoryFiles();
        var service = new GlobalColorRemovalService(
            repository,
            files,
            files,
            new StubProcessor
            {
                ApplyResult = new GlobalColorRemovalRasterResult([1], 1, 1, 1, 2)
            });

        var result = await service.ApplyAsync(
            fixture.Item.Id,
            fixture.SourceAsset.Id,
            new GlobalColorRemovalParameters(new(0, 0, 0), 0),
            TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("could not be added", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("assets/design - color removed.png", files.DeletedPaths);
        Assert.Equal(fixture.Snapshot, repository.Snapshot);
    }

    [Fact]
    public async Task ApplyAsync_RejectsReadOnlyItemBeforeWriting()
    {
        var fixture = Fixture.Create();
        var readOnly = fixture.Item with { Status = ItemStatus.Published, Stage = WorkflowStage.Listing };
        var repository = new MemoryRepository(fixture.Snapshot with { Items = [readOnly] });
        var files = new MemoryFiles();
        var service = new GlobalColorRemovalService(
            repository,
            files,
            files,
            new StubProcessor
            {
                ApplyResult = new GlobalColorRemovalRasterResult([1], 1, 1, 1, 2)
            });

        var result = await service.ApplyAsync(
            readOnly.Id,
            fixture.SourceAsset.Id,
            new GlobalColorRemovalParameters(new(0, 0, 0), 0),
            TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("Pause", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, files.SaveCount);
    }

    [Fact]
    public async Task ApplyAsync_CancellationBeforeOutputDoesNotMutateWorkspace()
    {
        var fixture = Fixture.Create();
        var repository = new MemoryRepository(fixture.Snapshot);
        var files = new MemoryFiles();
        var service = new GlobalColorRemovalService(
            repository,
            files,
            files,
            new StubProcessor
            {
                ApplyResult = new GlobalColorRemovalRasterResult([1], 1, 1, 1, 2)
            });
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.ApplyAsync(
            fixture.Item.Id,
            fixture.SourceAsset.Id,
            new GlobalColorRemovalParameters(new(0, 0, 0), 0),
            cancellation.Token));

        Assert.Equal(0, files.SaveCount);
        Assert.Equal(fixture.Snapshot, repository.Snapshot);
    }

    [Fact]
    public void Parameters_AllowExactBoundsAndRejectOutOfRangeTolerance()
    {
        _ = new GlobalColorRemovalParameters(new(1, 2, 3), 0);
        _ = new GlobalColorRemovalParameters(new(1, 2, 3), 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => new GlobalColorRemovalParameters(new(1, 2, 3), -0.01));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GlobalColorRemovalParameters(new(1, 2, 3), 1.01));
    }

    private sealed class StubProcessor : IGlobalColorRemovalProcessor
    {
        public GlobalColorRemovalRasterResult ApplyResult { get; init; } = new([], 1, 1, 0, 0);
        public Exception? ValidationFailure { get; init; }

        public Task ValidateAsync(Stream source, CancellationToken cancellationToken = default) =>
            ValidationFailure is null
                ? Task.CompletedTask
                : Task.FromException(ValidationFailure);

        public Task<GlobalColorRemovalRasterPreview> PreviewAsync(Stream source, GlobalColorRemovalParameters parameters, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GlobalColorRemovalRasterPreview([], 1, 1, 0, 0));

        public Task<GlobalColorRemovalRasterResult> ApplyAsync(Stream source, GlobalColorRemovalParameters parameters, CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplyResult);

        public Task<GlobalColorRemovalColor?> SampleAsync(Stream source, int x, int y, CancellationToken cancellationToken = default) =>
            Task.FromResult<GlobalColorRemovalColor?>(new(0, 0, 0));
    }

    private sealed class MemoryRepository(WorkspaceSnapshot initial) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = initial;
        public Exception? SaveFailure { get; init; }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            if (SaveFailure is not null)
            {
                return Task.FromException(SaveFailure);
            }

            Snapshot = snapshot;
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryFiles : IWorkspaceFileStore, IWorkspaceFileOutputStore
    {
        public string WorkspaceRoot => "workspace";
        public int SaveCount { get; private set; }
        public List<string> DeletedPaths { get; } = [];

        public string ResolvePath(string workspaceRelativePath) => Path.Combine(WorkspaceRoot, workspaceRelativePath);

        public bool Exists(string workspaceRelativePath) => true;

        public bool TryDelete(string workspaceRelativePath)
        {
            DeletedPaths.Add(workspaceRelativePath);
            return true;
        }

        public Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream([1, 2, 3]));

        public Task<ManagedWorkspaceFile> SaveAsync(string fileName, AssetKind kind, Stream content, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(new ManagedWorkspaceFile(fileName, kind, $"assets/{fileName}", ResolvePath($"assets/{fileName}"), string.Empty));
        }

        public Task<ManagedWorkspaceFile> ImportAsync(string sourcePath, AssetKind kind, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed record Fixture(
        DateTimeOffset Now,
        Guid DerivedAssetId,
        Item Item,
        Asset SourceAsset,
        WorkspaceSnapshot Snapshot)
    {
        public static Fixture Create()
        {
            var now = new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
            var store = new Store(Guid.NewGuid(), "Studio", null, false, now, now, "{}");
            var item = new Item(Guid.NewGuid(), store.Id, null, null, "Design", null, ItemStatus.Draft, WorkflowStage.Design, false, now, now, "{}");
            var source = new Asset(Guid.NewGuid(), store.Id, "design.png", null, AssetKind.ExportedImage, "assets/design.png", null, false, false, now, now, "{}");
            var snapshot = new WorkspaceSnapshot(
                [store],
                [],
                [],
                [item],
                [source],
                [],
                [],
                [],
                [new AssetLink(source.Id, WorkspaceEntityKind.Item, item.Id)]);
            return new(now, Guid.NewGuid(), item, source, snapshot);
        }
    }
}
