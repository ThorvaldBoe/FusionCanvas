using FusionCanvas.Application.AI;
using FusionCanvas.Application.DesignFiles;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.Application.Tests.DesignFiles;

public sealed class ArtworkGenerationServiceTests
{
    [Fact]
    public async Task GenerateAsync_PersistsOneFinalAssetAndAssignsDefaultRow()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
        var product = new StoreProduct(Guid.NewGuid(), store.Id, "Shirt", null, null, now, now, "{}");
        var offering = new FulfillmentOffering(Guid.NewGuid(), product.Id, "Provider", null, FulfillmentKind.FixedProvider, "Provider", null, now, now, "{}");
        var area = new DesignArea(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 1200, 1400, null, now, now, "{}");
        var item = new Item(Guid.NewGuid(), store.Id, null, null, "Fox", null, ItemStatus.Draft, WorkflowStage.Design, false, now, now,
            "{\"idea\":\"fox\",\"concept.idea\":\"clever fox\",\"phrase\":\"RUN\",\"graphicDirection\":\"bold fox\"}");
        var row = new DesignVariantRow(Guid.NewGuid(), item.Id, true, 0);
        var repo = new Repo(new WorkspaceSnapshot([store], [], [], [item], [], [], [], [], [])
        {
            StoreProducts = [product], FulfillmentOfferings = [offering], DesignAreas = [area],
            ItemListingConfigurations = [new(item.Id, offering.Id)], DesignVariantRows = [row],
            DesignVariantRowColors = [new(row.Id, "Black")], DesignSlotAssignments = [new(row.Id, area.Id, null)]
        });
        var provider = new Provider();
        var files = new Files();
        var service = new ArtworkGenerationService(repo, files, provider, new Normalizer(), () => now, Guid.NewGuid);
        var model = new AiModelDescriptor("image/model", "Image", null, null, ["text"], ["image"], [], null, null, null, null, true, null);

        var result = await service.GenerateAsync(new(item.Id, area.Id, "secret", AiProfileSettings.Empty with { ModelId = model.Id }, [model],
            [new AiImageEndpointCapabilities("endpoint", model.Id, true, true, ["png"], [new(1200, 1400)], true)], false), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Error);
        Assert.Single(repo.Snapshot.Assets);
        Assert.Equal(repo.Snapshot.Assets[0].Id, repo.Snapshot.DesignSlotAssignments.Single().AssetId);
        Assert.Single(repo.Snapshot.AssetLinks);
        Assert.Contains("RUN", repo.Snapshot.Assets[0].MetadataJson);
        Assert.Equal(1, provider.Calls);
    }

    private sealed class Repo(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; set; } = snapshot;
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default) { Snapshot = snapshot; return Task.CompletedTask; }
    }

    private sealed class Provider : IAiImageGenerationProvider
    {
        public int Calls { get; private set; }
        public Task<(AiImageGenerationResult? Result, AiImageGenerationFailure? Failure)> GenerateAsync(AiImageGenerationRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<(AiImageGenerationResult?, AiImageGenerationFailure?)>((new([1, 2, 3], "image/png", "OpenRouter", request.ModelId, request.ModelId), null));
        }
    }

    private sealed class Normalizer : IRasterArtworkNormalizer
    {
        public Task<RasterArtworkNormalizationResult> NormalizeAsync(Stream source, RasterArtworkNormalizationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new RasterArtworkNormalizationResult([137, 80, 78, 71], request.TargetSize, true, []));
    }

    private sealed class Files : IWorkspaceFileStore
    {
        public string WorkspaceRoot => "workspace";
        public Task<ManagedWorkspaceFile> ImportAsync(string sourcePath, AssetKind kind, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ManagedWorkspaceFile> SaveAsync(string fileName, AssetKind kind, Stream content, CancellationToken cancellationToken = default) => Task.FromResult(new ManagedWorkspaceFile(fileName, kind, "assets/generated.png", "workspace/assets/generated.png", ""));
        public bool Exists(string workspaceRelativePath) => true;
        public bool TryDelete(string workspaceRelativePath) => true;
        public Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream());
        public Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
