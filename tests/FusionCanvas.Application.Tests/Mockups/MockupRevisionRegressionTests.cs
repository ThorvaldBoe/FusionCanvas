using FusionCanvas.Application.Mockups;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests.Mockups;

public sealed class MockupRevisionRegressionTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ColorRevisionRetainsEveryActiveSourceAndApplicability()
    {
        var storeId = Guid.NewGuid();
        var blueprint = new Blueprint(Guid.NewGuid(), storeId, "Tee", null, false, Now, Now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, storeId, "Tee", null, BlueprintOfferingKind.ProviderNetwork, null, "network", null, null, false, Now, Now);
        var option = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Color, "Color", 0);
        var black = new OfferingOptionValue(Guid.NewGuid(), option.Id, offering.Id, "Black", 0);
        var white = new OfferingOptionValue(Guid.NewGuid(), option.Id, offering.Id, "White", 1);
        var template = new MockupTemplate(Guid.NewGuid(), offering.Id, null, "Front", null, 1, false, Now, Now);
        var a = new MockupTemplateSourceImage(Guid.NewGuid(), template.Id, Guid.NewGuid(), null, false, Now, Now, 100, 100);
        var b = new MockupTemplateSourceImage(Guid.NewGuid(), template.Id, Guid.NewGuid(), null, false, Now, Now, 100, 100);
        var snapshot = new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)], [new Store(storeId, "Store", null, false, Now, Now, "{}")], [], [], [], [], [], [], [], [])
        {
            Blueprints = [blueprint], BlueprintOfferings = [offering], OfferingOptions = [option], OfferingOptionValues = [black, white],
            MockupTemplates = [template], MockupTemplateSourceImages = [a, b],
            MockupTemplateSourceImageOptionValues = [new(a.Id, black.Id), new(b.Id, white.Id)],
            MockupTemplateColorVariants = []
        };
        var repo = new MemoryRepository(snapshot);
        var service = new MockupTemplateSetupService(repo, () => Now, Guid.NewGuid);
        var result = await service.AddColorAsync(new AddMockupTemplateColorRequest(storeId, template.Id, black.Id));
        Assert.True(result.Succeeded);
        var revision = repo.Snapshot.MockupTemplateRevisions.Single(value => value.RevisionNumber == 2);
        Assert.Equal(2, repo.Snapshot.MockupTemplateRevisionSourceImages.Count(value => value.RevisionId == revision.Id));
        Assert.Equal(2, repo.Snapshot.MockupTemplateRevisionSourceImageOptionValues.Count(value => repo.Snapshot.MockupTemplateRevisionSourceImages.Any(image => image.Id == value.RevisionSourceImageId && image.RevisionId == revision.Id)));
    }

    [Fact]
    public async Task AddingSourcesTwiceSnapshotsBothAndNoOpUpdateDoesNotAdvanceRevision()
    {
        var storeId = Guid.NewGuid(); var blueprint = new Blueprint(Guid.NewGuid(), storeId, "Tee", null, false, Now, Now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, storeId, "Tee", null, BlueprintOfferingKind.ProviderNetwork, null, "network", null, null, false, Now, Now);
        var option = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Color, "Color", 0);
        var black = new OfferingOptionValue(Guid.NewGuid(), option.Id, offering.Id, "Black", 0);
        var white = new OfferingOptionValue(Guid.NewGuid(), option.Id, offering.Id, "White", 1);
        var template = new MockupTemplate(Guid.NewGuid(), offering.Id, null, "Front", null, 1, false, Now, Now);
        var repo = new MemoryRepository(new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)], [new Store(storeId, "Store", null, false, Now, Now, "{}")], [], [], [], [], [], [], [], []) { Blueprints = [blueprint], BlueprintOfferings = [offering], OfferingOptions = [option], OfferingOptionValues = [black, white], MockupTemplates = [template] });
        var files = new FakeFiles(); var service = new MockupTemplateSourceImageService(repo, files, new FakeMetadata(), () => Now, Guid.NewGuid);
        await service.AddAsync(new AddLocalMockupTemplateSourceRequest(storeId, template.Id, "a.png", [black.Id]));
        await service.AddAsync(new AddLocalMockupTemplateSourceRequest(storeId, template.Id, "b.png", [white.Id]));
        var latest = repo.Snapshot.MockupTemplateRevisions.Single(value => value.RevisionNumber == 3);
        Assert.Equal(2, repo.Snapshot.MockupTemplateRevisionSourceImages.Count(value => value.RevisionId == latest.Id));
        Assert.Equal(2, repo.Snapshot.MockupTemplateRevisionSourceImageOptionValues.Count(value => repo.Snapshot.MockupTemplateRevisionSourceImages.Any(image => image.Id == value.RevisionSourceImageId && image.RevisionId == latest.Id)));
        var image = repo.Snapshot.MockupTemplateSourceImages.First();
        await service.UpdateAsync(new UpdateLocalMockupTemplateSourceRequest(storeId, template.Id, image.Id, [black.Id]));
        Assert.Equal(3, repo.Snapshot.MockupTemplates.Single().CurrentRevision);
        var count = repo.Snapshot.MockupTemplateRevisions.Count;
        await service.UpdateAsync(new UpdateLocalMockupTemplateSourceRequest(storeId, template.Id, image.Id, [black.Id]));
        Assert.Equal(count, repo.Snapshot.MockupTemplateRevisions.Count);
    }

    private sealed class MemoryRepository(WorkspaceSnapshot initial) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = initial;
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default) { Snapshot = snapshot; return Task.CompletedTask; }
    }

    private sealed class FakeMetadata : IRasterImageMetadataReader { public Task<RasterImageInfo> ReadAsync(string sourcePath, CancellationToken cancellationToken = default) => Task.FromResult(new RasterImageInfo(100, 100)); }
    private sealed class FakeFiles : IWorkspaceFileStore
    {
        private int _next;
        public string WorkspaceRoot => "workspace";
        public Task<ManagedWorkspaceFile> ImportAsync(string sourcePath, FusionCanvas.Domain.Assets.AssetKind kind, CancellationToken cancellationToken = default) { var n = ++_next; return Task.FromResult(new ManagedWorkspaceFile($"{n}.png", kind, $"assets/{n}.png", $"workspace/assets/{n}.png", sourcePath)); }
        public bool Exists(string workspaceRelativePath) => true;
        public bool TryDelete(string workspaceRelativePath) => true;
        public Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream());
        public Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
