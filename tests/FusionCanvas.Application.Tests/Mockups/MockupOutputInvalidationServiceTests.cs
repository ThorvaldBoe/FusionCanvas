using System.Text.Json;
using FusionCanvas.Application.Mockups;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests.Mockups;

public sealed class MockupOutputInvalidationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Precise_invalidation_removes_only_outputs_using_changed_design_asset()
    {
        var itemId = Guid.NewGuid();
        var changedDesignId = Guid.NewGuid();
        var otherDesignId = Guid.NewGuid();
        var matchingOutput = Output(itemId, Guid.NewGuid(), changedDesignId, "matching.png");
        var retainedOutput = Output(itemId, Guid.NewGuid(), otherDesignId, "retained.png");
        var fixture = CreateFixture(itemId, matchingOutput, retainedOutput);

        var result = await fixture.Service.InvalidateAsync(itemId, new HashSet<Guid> { changedDesignId });

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.RemovedCount);
        Assert.DoesNotContain(matchingOutput.Id, fixture.Repository.Snapshot.Assets.Select(asset => asset.Id));
        Assert.Contains(retainedOutput.Id, fixture.Repository.Snapshot.Assets.Select(asset => asset.Id));
        var updatedItem = Assert.Single(fixture.Repository.Snapshot.Items);
        Assert.Contains("1 generated mockup", updatedItem.MetadataJson);
        Assert.Contains(matchingOutput.WorkspaceRelativePath, fixture.Files.DeletedPaths);
    }

    [Fact]
    public async Task Item_wide_invalidation_removes_all_outputs_for_ambiguous_design_change()
    {
        var itemId = Guid.NewGuid();
        var first = Output(itemId, Guid.NewGuid(), Guid.NewGuid(), "first.png");
        var second = Output(itemId, Guid.NewGuid(), Guid.NewGuid(), "second.png");
        var fixture = CreateFixture(itemId, first, second);

        var result = await fixture.Service.InvalidateAsync(itemId, invalidateAll: true);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.RemovedCount);
        Assert.DoesNotContain(fixture.Repository.Snapshot.AssetLinks, link => link.EntityId == itemId);
        Assert.Equal(2, fixture.Files.DeletedPaths.Count);
    }

    private static Fixture CreateFixture(Guid itemId, params Asset[] outputs)
    {
        var storeId = Guid.NewGuid();
        var item = new Item(itemId, storeId, null, null, "Listing", null, ItemStatus.Draft, WorkflowStage.Listing, false, Now, Now, "{}");
        var snapshot = new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)],
            [new Store(storeId, "Store", null, false, Now, Now, "{}")], [], [], [], [], [], [], [], [])
        {
            Items = [item],
            Assets = outputs,
            AssetLinks = outputs.Select(asset => new AssetLink(asset.Id, WorkspaceEntityKind.Item, itemId)).ToArray()
        };
        var repository = new MemoryRepository(snapshot);
        var files = new MemoryFiles();
        return new(repository, files, new MockupOutputInvalidationService(repository, files));
    }

    private static Asset Output(Guid itemId, Guid assetId, Guid designAssetId, string name) =>
        new(assetId, Guid.NewGuid(), name, null, AssetKind.MockupImage, $"mockups/{name}", null, false, false, Now, Now,
            JsonSerializer.Serialize(new { itemId, designAssetId }));

    private sealed record Fixture(MemoryRepository Repository, MemoryFiles Files, MockupOutputInvalidationService Service);

    private sealed class MemoryRepository(WorkspaceSnapshot initial) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = initial;
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            Snapshot = snapshot;
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryFiles : IWorkspaceFileOutputStore
    {
        public string WorkspaceRoot => "workspace";
        public HashSet<string> DeletedPaths { get; } = [];
        public string ResolvePath(string workspaceRelativePath) => workspaceRelativePath;
        public bool Exists(string workspaceRelativePath) => true;
        public bool TryDelete(string workspaceRelativePath) => DeletedPaths.Add(workspaceRelativePath);
        public Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream());
        public Task<ManagedWorkspaceFile> SaveAsync(string fileName, AssetKind kind, Stream content, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ManagedWorkspaceFile> ImportAsync(string sourcePath, AssetKind kind, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
