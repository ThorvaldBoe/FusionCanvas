using FusionCanvas.Application.ArtworkStyles;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.ArtworkStyles;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.Application.Tests.ArtworkStyles;

public sealed class ArtworkStyleManagementServiceTests
{
    [Fact]
    public async Task SaveAsync_CreatesAndUpdatesWorkspaceStyleWithOrderedReferences()
    {
        var (service, repository, files, workspaceId) = Create();
        var created = await service.SaveAsync(workspaceId, null, "  Watercolor ", "Muted painted look", [new(SourcePath: "first.png"), new(SourcePath: "second.jpg")], TestContext.Current.CancellationToken);
        Assert.True(created.Succeeded, created.Error);
        var style = Assert.Single(created.State!.Styles);
        Assert.Equal("Watercolor", style.Title);
        Assert.Equal(new[] { 0, 1 }, created.State.References[style.Id].Select(value => value.Reference.SortOrder));
        var originalPaths = created.State.References[style.Id].Select(value => value.Reference.WorkspaceRelativePath).ToArray();

        var updated = await service.SaveAsync(workspaceId, style.Id, "Watercolor", "Soft wash", [new(ExistingWorkspaceRelativePath: originalPaths[0]), new(SourcePath: "third.webp")], TestContext.Current.CancellationToken);

        Assert.True(updated.Succeeded, updated.Error);
        Assert.Equal(style.Id, Assert.Single(repository.Snapshot.ArtworkStyles).Id);
        Assert.Equal("Soft wash", repository.Snapshot.ArtworkStyles[0].Description);
        Assert.False(files.Exists(originalPaths[1]));
        Assert.True(files.Exists(originalPaths[0]));
        Assert.Equal(new[] { originalPaths[0], "styles/3.webp" }, repository.Snapshot.ArtworkStyleReferences.OrderBy(value => value.SortOrder).Select(value => value.WorkspaceRelativePath));
    }

    [Fact]
    public async Task SaveAsync_RejectsBlankDuplicateAndReferenceFreeDrafts()
    {
        var (service, _, _, workspaceId) = Create();
        var first = await service.SaveAsync(workspaceId, null, "Watercolor", "Muted", [new(SourcePath: "first.png")], TestContext.Current.CancellationToken);
        Assert.True(first.Succeeded, first.Error);

        Assert.False((await service.SaveAsync(workspaceId, null, " ", "desc", [new(SourcePath: "other.png")], TestContext.Current.CancellationToken)).Succeeded);
        Assert.False((await service.SaveAsync(workspaceId, null, "watercolor", "desc", [new(SourcePath: "other.png")], TestContext.Current.CancellationToken)).Succeeded);
        Assert.False((await service.SaveAsync(workspaceId, null, "Ink", "desc", [], TestContext.Current.CancellationToken)).Succeeded);
    }

    [Fact]
    public async Task SaveAsync_CleansImportedFilesWhenRepositoryWriteFails()
    {
        var (service, repository, files, workspaceId) = Create();
        repository.FailSave = true;

        var result = await service.SaveAsync(workspaceId, null, "Ink", "Bold print", [new(SourcePath: "ink.png")], TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Empty(repository.Snapshot.ArtworkStyles);
        Assert.False(files.Exists("styles/1.png"));
    }

    [Fact]
    public async Task DeleteAsync_ResetsSelectedItemsAndPreservesOtherMetadata()
    {
        var (service, repository, files, workspaceId) = Create(withItem: true);
        var saved = await service.SaveAsync(workspaceId, null, "Watercolor", "Muted", [new(SourcePath: "first.png")], TestContext.Current.CancellationToken);
        var styleId = saved.SavedStyleId!.Value;
        var item = repository.Snapshot.Items.Single();
        var metadata = ItemMetadataCodec.ParseMetadata(item.MetadataJson);
        metadata[ItemMetadataCodec.ArtworkStyleIdKey] = styleId.ToString();
        metadata["other"] = "preserve";
        var referencePath = repository.Snapshot.ArtworkStyleReferences.Single().WorkspaceRelativePath;
        var sharedAsset = new Asset(Guid.NewGuid(), item.StoreId, "Shared", null, AssetKind.ReferenceImage, referencePath, null, false, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");
        await repository.SaveAsync(repository.Snapshot with { Items = [item with { MetadataJson = ItemMetadataCodec.SerializeMetadata(metadata) }], Assets = [sharedAsset] }, TestContext.Current.CancellationToken);
        Assert.Equal(1, (await service.LoadAsync(workspaceId, TestContext.Current.CancellationToken)).AssignedItemCounts[styleId]);

        var deleted = await service.DeleteAsync(workspaceId, styleId, TestContext.Current.CancellationToken);

        Assert.True(deleted.Succeeded, deleted.Error);
        Assert.Equal(1, deleted.AffectedItemCount);
        var after = ItemMetadataCodec.ParseMetadata(repository.Snapshot.Items.Single().MetadataJson);
        Assert.False(after.ContainsKey(ItemMetadataCodec.ArtworkStyleIdKey));
        Assert.Equal("preserve", after["other"]);
        Assert.Empty(repository.Snapshot.ArtworkStyles);
        Assert.True(files.Exists(referencePath));
    }

    private static (ArtworkStyleManagementService Service, MemoryRepository Repository, MemoryFileStore Files, Guid WorkspaceId) Create(bool withItem = false)
    {
        var now = DateTimeOffset.UtcNow;
        var workspaceId = Guid.NewGuid();
        var store = new Store(Guid.NewGuid(), workspaceId, "Store", null, false, now, now, "{}");
        var item = new Item(Guid.NewGuid(), store.Id, null, null, "Item", null, ItemStatus.Draft, WorkflowStage.Design, false, now, now, "{}");
        var snapshot = WorkspaceSnapshot.Empty with
        {
            Workspaces = [new Workspace(workspaceId, "Workspace", null, false, now, now, "{}")],
            Stores = [store],
            Items = withItem ? [item] : []
        };
        var repository = new MemoryRepository(snapshot);
        var files = new MemoryFileStore();
        return (new ArtworkStyleManagementService(repository, files), repository, files, workspaceId);
    }

    private sealed class MemoryRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = snapshot;
        public bool FailSave { get; set; }
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default) { if (FailSave) throw new IOException("write failed"); Snapshot = snapshot; return Task.CompletedTask; }
    }

    private sealed class MemoryFileStore : IWorkspaceFileStore
    {
        private readonly HashSet<string> _paths = new(StringComparer.OrdinalIgnoreCase);
        private int _next;
        public string WorkspaceRoot => "workspace";
        public string ResolvePath(string workspaceRelativePath) => Path.Combine(WorkspaceRoot, workspaceRelativePath);
        public Task<ManagedWorkspaceFile> ImportAsync(string sourcePath, AssetKind kind, CancellationToken cancellationToken = default)
        {
            var extension = Path.GetExtension(sourcePath);
            var path = $"styles/{++_next}{extension}";
            _paths.Add(path);
            return Task.FromResult(new ManagedWorkspaceFile(Path.GetFileName(sourcePath), kind, path, ResolvePath(path), sourcePath));
        }
        public bool Exists(string workspaceRelativePath) => _paths.Contains(workspaceRelativePath);
        public bool TryDelete(string workspaceRelativePath) => _paths.Remove(workspaceRelativePath);
        public Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream());
        public Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
