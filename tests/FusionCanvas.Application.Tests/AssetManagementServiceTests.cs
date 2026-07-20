using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests;

public class AssetManagementServiceTests
{
    [Fact]
    public async Task LoadAsync_FiltersByContextAndComputesMissingAndStoreLabels()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var fileStore = new FakeFileStore();
        fileStore.MarkMissing(sample.LinkedAsset.WorkspaceRelativePath);
        var service = sample.Service(repository, fileStore);

        var listingState = await service.LoadAsync(sample.ListingContext);
        var storeState = await service.LoadAsync(sample.StoreContext);
        var archivedGroupState = await service.LoadAsync(new AssetContextReference(WorkspaceEntityKind.Group, sample.ArchivedGroup.Id));

        Assert.Single(listingState.Assets);
        Assert.True(listingState.Assets.Single().IsMissing);
        Assert.Null(listingState.Assets.Single().ContextLabel);
        Assert.Equal("Listing: Idea", storeState.Assets.Single(a => a.Id == sample.LinkedAsset.Id).ContextLabel);
        Assert.Equal("—", storeState.Assets.Single(a => a.Id == sample.UnlinkedAsset.Id).ContextLabel);
        Assert.Equal(2, storeState.Assets.Count);
        Assert.Null(archivedGroupState.Context);
        Assert.NotNull(archivedGroupState.Error);
        Assert.NotEmpty(archivedGroupState.Error!);
    }

    [Fact]
    public async Task ImportAsync_CopiesFileAndCreatesAssetAndLinkAtomically()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var fileStore = new FakeFileStore();
        var assetId = Guid.NewGuid();
        var service = sample.Service(repository, fileStore, nextId: assetId);

        var result = await service.ImportAssetAsync(new(sample.ListingContext, @"C:\imports\design.svg", AssetKind.Svg));

        Assert.True(result.Succeeded);
        Assert.Equal(assetId, result.Asset!.Id);
        Assert.Equal(AssetKind.Svg, result.Asset.Kind);
        var stored = repository.Snapshot.Assets.Single(asset => asset.Id == assetId);
        Assert.Equal(sample.Store.Id, stored.StoreId);
        Assert.Contains(repository.Snapshot.AssetLinks, link => link.AssetId == assetId && link.EntityKind == WorkspaceEntityKind.Listing && link.EntityId == sample.Listing.Id);
        Assert.Equal("design.svg", stored.Name);
        Assert.Equal("assets/design.svg", stored.WorkspaceRelativePath);
        Assert.Equal(2, result.State.Assets.Count);
        Assert.Contains(result.State.Assets, a => a.Id == assetId);
        Assert.Equal(1, repository.SaveCount);
        Assert.Single(fileStore.Imports);
    }

    [Theory]
    [InlineData("", AssetKind.Unknown, "A source file is required.")]
    [InlineData(@"C:\imports\missing.png", AssetKind.ExportedImage, "The source file was not found.")]
    public async Task ImportAsync_BlocksUnsupportedAndMissingSourcesBeforeCopy(string sourcePath, AssetKind kind, string expectedError)
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var fileStore = new FakeFileStore().WithMissingSource();
        var service = sample.Service(repository, fileStore);

        var result = await service.ImportAssetAsync(new(sample.ListingContext, sourcePath, kind));

        Assert.False(result.Succeeded);
        Assert.Equal(expectedError, result.Error);
        Assert.Equal(sample.Snapshot, repository.Snapshot);
        Assert.Empty(fileStore.Imports);
    }

    [Fact]
    public async Task ImportAsync_DeletesCopiedFileWhenSaveFails()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot) { FailSaves = true };
        var fileStore = new FakeFileStore();
        var service = sample.Service(repository, fileStore);

        var result = await service.ImportAssetAsync(new(sample.ListingContext, @"C:\imports\design.svg", AssetKind.Svg));

        Assert.False(result.Succeeded);
        Assert.Single(fileStore.Imports);
        Assert.Empty(fileStore.ExistingReferences);
        Assert.Equal(sample.Snapshot, repository.Snapshot);
    }

    [Fact]
    public async Task ImportAsync_RejectsArchivedOrUnavailableContext()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var fileStore = new FakeFileStore();
        var service = sample.Service(repository, fileStore);

        var archivedNiche = await service.ImportAssetAsync(new(new AssetContextReference(WorkspaceEntityKind.Niche, sample.ArchivedNiche.Id), @"C:\imports\design.svg", AssetKind.Svg));
        var missingListing = await service.ImportAssetAsync(new(new AssetContextReference(WorkspaceEntityKind.Listing, Guid.NewGuid()), @"C:\imports\design.svg", AssetKind.Svg));

        Assert.False(archivedNiche.Succeeded);
        Assert.False(missingListing.Succeeded);
        Assert.Empty(fileStore.Imports);
    }

    [Fact]
    public async Task RelabelAssetAsync_PersistsPurposeAndPreservesFileAndLink()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var fileStore = new FakeFileStore();
        var service = sample.Service(repository, fileStore);

        var result = await service.RelabelAssetAsync(new(sample.LinkedAsset.Id, AssetKind.ReferenceImage));

        Assert.True(result.Succeeded);
        var stored = repository.Snapshot.Assets.Single(asset => asset.Id == sample.LinkedAsset.Id);
        Assert.Equal(AssetKind.ReferenceImage, stored.Kind);
        Assert.Equal(sample.LinkedAsset.WorkspaceRelativePath, stored.WorkspaceRelativePath);
        Assert.Contains(repository.Snapshot.AssetLinks, link => link.AssetId == sample.LinkedAsset.Id);
    }

    [Fact]
    public async Task RemoveAssetAsync_RequiresConfirmationThenDeletesRecordLinkAndFile()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot);
        var fileStore = new FakeFileStore().Seed(sample.LinkedAsset.WorkspaceRelativePath);
        var service = sample.Service(repository, fileStore);

        var unconfirmed = await service.RemoveAssetAsync(new(sample.LinkedAsset.Id, ConfirmPermanentRemoval: false));
        var confirmed = await service.RemoveAssetAsync(new(sample.LinkedAsset.Id, ConfirmPermanentRemoval: true));

        Assert.False(unconfirmed.Succeeded);
        Assert.True(confirmed.Succeeded);
        Assert.DoesNotContain(repository.Snapshot.Assets, asset => asset.Id == sample.LinkedAsset.Id);
        Assert.DoesNotContain(repository.Snapshot.AssetLinks, link => link.AssetId == sample.LinkedAsset.Id);
        Assert.Empty(fileStore.ExistingReferences);
    }

    [Fact]
    public async Task RemoveAssetAsync_KeepsRecordAndFileWhenSaveFails()
    {
        var sample = Sample.Create();
        var repository = new TestRepository(sample.Snapshot) { FailSaves = true };
        var fileStore = new FakeFileStore().Seed(sample.LinkedAsset.WorkspaceRelativePath);
        var service = sample.Service(repository, fileStore);

        var result = await service.RemoveAssetAsync(new(sample.LinkedAsset.Id, ConfirmPermanentRemoval: true));

        Assert.False(result.Succeeded);
        Assert.Contains(repository.Snapshot.Assets, asset => asset.Id == sample.LinkedAsset.Id);
        Assert.Contains(fileStore.ExistingReferences, path => path == sample.LinkedAsset.WorkspaceRelativePath);
    }

    [Fact]
    public async Task ExtensionPolicy_SuggestsKnownKindsAndFallsBackToUnknown()
    {
        Assert.Equal(AssetKind.SourceDesign, AssetPurposePolicy.SuggestKind("file.afdesign"));
        Assert.Equal(AssetKind.ExportedImage, AssetPurposePolicy.SuggestKind("file.png"));
        Assert.Equal(AssetKind.Svg, AssetPurposePolicy.SuggestKind("file.SVG"));
        Assert.Equal(AssetKind.Font, AssetPurposePolicy.SuggestKind("file.otf"));
        Assert.Equal(AssetKind.Brush, AssetPurposePolicy.SuggestKind("file.brush"));
        Assert.Equal(AssetKind.Unknown, AssetPurposePolicy.SuggestKind("file.unknown"));
        Assert.Contains(AssetPurposePolicy.AvailablePurposes, kind => kind == AssetKind.MockupImage);
        Assert.DoesNotContain(AssetPurposePolicy.AvailablePurposes, kind => kind == AssetKind.PromptOutput);
    }

    private sealed class TestRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = snapshot;
        public int SaveCount { get; private set; }
        public bool FailSaves { get; init; }
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public Task SaveAsync(WorkspaceSnapshot value, CancellationToken cancellationToken = default)
        {
            if (FailSaves) throw new IOException("save failed");
            Snapshot = value;
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFileStore : IWorkspaceFileStore
    {
        private readonly HashSet<string> _existing = [];
        private bool _sourceMissing;

        public string WorkspaceRoot => @"C:\workspace";
        public IReadOnlyList<string> Imports { get; } = new List<string>();
        public IReadOnlyCollection<string> ExistingReferences => _existing;

        public FakeFileStore WithMissingSource()
        {
            _sourceMissing = true;
            return this;
        }

        public FakeFileStore Seed(params string[] workspaceRelativePaths)
        {
            foreach (var path in workspaceRelativePaths)
            {
                _existing.Add(path.Replace('\\', '/'));
            }
            return this;
        }

        public void MarkMissing(string workspaceRelativePath) => _existing.Remove(workspaceRelativePath.Replace('\\', '/'));

        public Task<ManagedWorkspaceFile> ImportAsync(string sourcePath, AssetKind kind, CancellationToken cancellationToken = default)
        {
            if (_sourceMissing || sourcePath.Contains("missing", StringComparison.OrdinalIgnoreCase))
            {
                throw new FileNotFoundException("The source asset file was not found.", sourcePath);
            }

            var relativePath = $"assets/{Path.GetFileName(sourcePath)}";
            _existing.Add(relativePath);
            ((List<string>)Imports).Add(sourcePath);
            return Task.FromResult(new ManagedWorkspaceFile(
                Path.GetFileName(sourcePath),
                kind,
                relativePath,
                Path.Combine(WorkspaceRoot, relativePath),
                sourcePath));
        }

        public bool Exists(string workspaceRelativePath) => _existing.Contains(workspaceRelativePath.Replace('\\', '/'));

        public bool TryDelete(string workspaceRelativePath) => _existing.Remove(workspaceRelativePath.Replace('\\', '/'));
    }

    private sealed record Sample(
        WorkspaceSnapshot Snapshot,
        DateTimeOffset Now,
        Store Store,
        Niche Niche,
        TopicGroup Group,
        Listing Listing,
        Asset LinkedAsset,
        Asset UnlinkedAsset,
        Niche ArchivedNiche,
        TopicGroup ArchivedGroup)
    {
        public AssetContextReference ListingContext => new(WorkspaceEntityKind.Listing, Listing.Id);
        public AssetContextReference StoreContext => new(WorkspaceEntityKind.Store, Store.Id);

        public AssetManagementService Service(TestRepository repository, FakeFileStore fileStore, Guid? nextId = null) =>
            new(repository, fileStore, clock: () => Now.AddMinutes(1), newId: () => nextId ?? Guid.NewGuid());

        public static Sample Create()
        {
            var now = DateTimeOffset.UtcNow;
            var nicheId = Guid.NewGuid();
            var store = new Store(Guid.NewGuid(), "North Star", null, false, now, now, "{}", nicheId);
            var niche = new Niche(nicheId, store.Id, "Coffee", null, false, now, now, "{}");
            var group = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Dogs and coffee", null, false, now, now, "{}");
            var archivedNiche = new Niche(Guid.NewGuid(), store.Id, "Archived", null, true, now, now, "{}");
            var archivedGroup = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Archived group", null, true, now, now, "{}");
            var listing = new Listing(Guid.NewGuid(), store.Id, niche.Id, group.Id, "Idea", null, ListingStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");
            var linkedAsset = new Asset(Guid.NewGuid(), store.Id, "linked.png", null, AssetKind.ExportedImage, "assets/linked.png", @"C:\imports\linked.png", false, false, now, now, "{}");
            var unlinkedAsset = new Asset(Guid.NewGuid(), store.Id, "unlinked.png", null, AssetKind.ExportedImage, "assets/unlinked.png", @"C:\imports\unlinked.png", false, false, now, now, "{}");
            var link = new AssetLink(linkedAsset.Id, WorkspaceEntityKind.Listing, listing.Id);
            var snapshot = WorkspaceSnapshot.FromStores([store], [niche, archivedNiche], [group, archivedGroup], [listing], [linkedAsset, unlinkedAsset], [], [], [], [link]);
            return new(snapshot, now, store, niche, group, listing, linkedAsset, unlinkedAsset, archivedNiche, archivedGroup);
        }
    }
}
