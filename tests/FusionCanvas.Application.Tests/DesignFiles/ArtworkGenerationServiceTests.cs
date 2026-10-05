using FusionCanvas.Application.AI;
using FusionCanvas.Application.DesignFiles;
using FusionCanvas.Application.Telemetry;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.Application.Tests.DesignFiles;

public sealed class ArtworkGenerationServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GenerateAsync_DisposesArtworkStreamWhenFileStoreSaveSucceedsOrFails(bool failSave)
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
        var product = new StoreProduct(Guid.NewGuid(), store.Id, "Shirt", null, null, now, now, "{}");
        var offering = new FulfillmentOffering(Guid.NewGuid(), product.Id, "Provider", null, FulfillmentKind.FixedProvider, "Provider", null, now, now, "{}");
        var area = new DesignArea(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 1200, 1400, null, now, now, "{}");
        var item = new Item(Guid.NewGuid(), store.Id, null, null, "Fox", null, ItemStatus.Draft, WorkflowStage.Design, false, now, now,
            "{\"idea\":\"fox\",\"concept.idea\":\"clever fox\",\"phrase\":\"RUN WITH PURPOSE\",\"graphicDirection\":\"bold fox\"}");
        var row = new DesignVariantRow(Guid.NewGuid(), item.Id, true, 0);
        var repo = new Repo(new WorkspaceSnapshot([store], [], [], [item], [], [], [], [], [])
        {
            StoreProducts = [product],
            FulfillmentOfferings = [offering],
            DesignAreas = [area],
            ItemListingConfigurations = [new(item.Id, offering.Id)],
            DesignVariantRows = [row],
            DesignVariantRowColors = [new(row.Id, "Black")],
            DesignSlotAssignments = [new(row.Id, area.Id, null)]
        });
        var provider = new Provider();
        var files = new Files { SaveFailure = failSave ? new IOException("File store failed.") : null };
        var service = new ArtworkGenerationService(repo, files, new TestAiImageProvenanceCodec(), provider, new Normalizer(), () => now, Guid.NewGuid);
        var model = new AiModelDescriptor("image/model", "Image", null, null, ["text"], ["image"], [], null, null, null, null, true, null);
        var request = new ArtworkGenerationRequest(item.Id, area.Id, "secret", AiProfileSettings.Empty with { ModelId = model.Id }, [model],
            [new AiImageEndpointCapabilities("endpoint", model.Id, true, true, ["png"], [new(1200, 1400)], true)], false);

        if (failSave)
        {
            await Assert.ThrowsAsync<IOException>(() => service.GenerateAsync(request, TestContext.Current.CancellationToken));
        }
        else
        {
            var result = await service.GenerateAsync(request, TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded, result.Error);
            Assert.Single(repo.Snapshot.Assets);
            Assert.Equal(repo.Snapshot.Assets[0].Id, repo.Snapshot.DesignSlotAssignments.Single().AssetId);
            Assert.Single(repo.Snapshot.AssetLinks);
            Assert.Equal("test-provenance", repo.Snapshot.Assets[0].MetadataJson);
            Assert.Equal(1, provider.Calls);
            Assert.Equal(new AiImageSize(1200, 1400), provider.LastRequest!.Options!.Size);
        }

        Assert.False(Assert.IsType<MemoryStream>(files.SavedContent).CanRead);
    }

    [Fact]
    public async Task GenerateAsync_IncludesNicheContextAndPrintableArtworkConstraintInPrompt()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Coffee culture", "Specialty coffee fans", false, now, now,
            """{"audience":"Coffee enthusiasts","visualStyleGuidance":"Warm hand-drawn linework"}""");
        var product = new StoreProduct(Guid.NewGuid(), store.Id, "Shirt", null, null, now, now, "{}");
        var offering = new FulfillmentOffering(Guid.NewGuid(), product.Id, "Provider", null, FulfillmentKind.FixedProvider, "Provider", null, now, now, "{}");
        var area = new DesignArea(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 1200, 1400, null, now, now, "{}");
        var item = new Item(Guid.NewGuid(), store.Id, niche.Id, null, "Fox", null, ItemStatus.Draft, WorkflowStage.Design, false, now, now,
            "{\"idea\":\"fox\",\"concept.idea\":\"clever fox\",\"phrase\":\"RUN WITH PURPOSE\",\"graphicDirection\":\"bold fox\"}");
        var row = new DesignVariantRow(Guid.NewGuid(), item.Id, true, 0);
        var repository = new Repo(new WorkspaceSnapshot([store], [niche], [], [item], [], [], [], [], [])
        {
            StoreProducts = [product],
            FulfillmentOfferings = [offering],
            DesignAreas = [area],
            ItemListingConfigurations = [new(item.Id, offering.Id)],
            DesignVariantRows = [row],
            DesignVariantRowColors = [new(row.Id, "Black")]
        });
        var provider = new Provider();
        var service = new ArtworkGenerationService(repository, new Files(), new TestAiImageProvenanceCodec(), provider, new Normalizer(), () => now, Guid.NewGuid);
        var model = new AiModelDescriptor("image/model", "Image", null, null, ["text"], ["image"], [], 1000, null, null, null, true, null);
        var request = new ArtworkGenerationRequest(item.Id, area.Id, "secret", AiProfileSettings.Empty with { ModelId = model.Id }, [model],
            [new AiImageEndpointCapabilities("endpoint", model.Id, true, true, ["png"], [new(1200, 1400)], true)], false);

        var result = await service.GenerateAsync(request, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Error);
        var prompt = provider.LastRequest!.Prompt;
        Assert.Contains("Coffee culture", prompt, StringComparison.Ordinal);
        Assert.Contains("Coffee enthusiasts", prompt, StringComparison.Ordinal);
        Assert.Contains("flat printable artwork image only", prompt, StringComparison.Ordinal);
        Assert.Contains("Do not show it applied to a mug, shirt, garment, person, product, or in a product photograph or mockup.", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateAsync_CleansGeneratedFileAndPropagatesWorkspaceSaveCancellation()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
        var product = new StoreProduct(Guid.NewGuid(), store.Id, "Shirt", null, null, now, now, "{}");
        var offering = new FulfillmentOffering(Guid.NewGuid(), product.Id, "Provider", null, FulfillmentKind.FixedProvider, "Provider", null, now, now, "{}");
        var area = new DesignArea(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 1200, 1400, null, now, now, "{}");
        var item = new Item(Guid.NewGuid(), store.Id, null, null, "Fox", null, ItemStatus.Draft, WorkflowStage.Design, false, now, now,
            "{\"idea\":\"fox\",\"concept.idea\":\"clever fox\",\"phrase\":\"RUN WITH PURPOSE\",\"graphicDirection\":\"bold fox\"}");
        var row = new DesignVariantRow(Guid.NewGuid(), item.Id, true, 0);
        var initialSnapshot = new WorkspaceSnapshot([store], [], [], [item], [], [], [], [], [])
        {
            StoreProducts = [product],
            FulfillmentOfferings = [offering],
            DesignAreas = [area],
            ItemListingConfigurations = [new(item.Id, offering.Id)],
            DesignVariantRows = [row],
            DesignVariantRowColors = [new(row.Id, "Black")],
            DesignSlotAssignments = [new(row.Id, area.Id, null)]
        };
        var cancellation = new OperationCanceledException("Workspace save was cancelled.");
        var repo = new Repo(initialSnapshot) { SaveFailure = cancellation };
        var files = new Files();
        var provider = new Provider();
        var service = new ArtworkGenerationService(repo, files, new TestAiImageProvenanceCodec(), provider, new Normalizer(), () => now, Guid.NewGuid);
        var model = new AiModelDescriptor("image/model", "Image", null, null, ["text"], ["image"], [], null, null, null, null, true, null);
        var request = new ArtworkGenerationRequest(item.Id, area.Id, "secret", AiProfileSettings.Empty with { ModelId = model.Id }, [model],
            [new AiImageEndpointCapabilities("endpoint", model.Id, true, true, ["png"], [new(1200, 1400)], true)], false);

        var thrown = await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.GenerateAsync(request, TestContext.Current.CancellationToken));

        Assert.Same(cancellation, thrown);
        Assert.Equal("assets/generated.png", Assert.Single(files.DeletedPaths));
        Assert.Same(initialSnapshot, repo.Snapshot);
        Assert.Empty(repo.Snapshot.Assets);
        Assert.Empty(repo.Snapshot.AssetLinks);
        Assert.Null(Assert.Single(repo.Snapshot.DesignSlotAssignments).AssetId);
    }

    [Theory]
    [InlineData("present", "Failed")]
    [InlineData("delete_throws", "Failed")]
    [InlineData("missing", "AlreadyMissing")]
    [InlineData("uninspectable", "Uninspectable")]
    public async Task GenerateAsync_ReportsCleanupOutcomeWithoutReplacingWorkspaceSaveCancellation(
        string fileProbe,
        string expectedCleanupStatus)
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
        var product = new StoreProduct(Guid.NewGuid(), store.Id, "Shirt", null, null, now, now, "{}");
        var offering = new FulfillmentOffering(Guid.NewGuid(), product.Id, "Provider", null, FulfillmentKind.FixedProvider, "Provider", null, now, now, "{}");
        var area = new DesignArea(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 1200, 1400, null, now, now, "{}");
        var item = new Item(Guid.NewGuid(), store.Id, null, null, "Fox", null, ItemStatus.Draft, WorkflowStage.Design, false, now, now,
            "{\"idea\":\"fox\",\"concept.idea\":\"clever fox\",\"phrase\":\"RUN WITH PURPOSE\",\"graphicDirection\":\"bold fox\"}");
        var row = new DesignVariantRow(Guid.NewGuid(), item.Id, true, 0);
        var initialSnapshot = new WorkspaceSnapshot([store], [], [], [item], [], [], [], [], [])
        {
            StoreProducts = [product],
            FulfillmentOfferings = [offering],
            DesignAreas = [area],
            ItemListingConfigurations = [new(item.Id, offering.Id)],
            DesignVariantRows = [row],
            DesignVariantRowColors = [new(row.Id, "Black")],
            DesignSlotAssignments = [new(row.Id, area.Id, null)]
        };
        var cancellation = new OperationCanceledException("Workspace save was cancelled.");
        var repository = new Repo(initialSnapshot) { SaveFailure = cancellation };
        var files = new Files
        {
            DeleteResult = false,
            DeleteFailure = fileProbe == "delete_throws" ? new IOException("Delete failed.") : null,
            OpenReadFailure = fileProbe switch
            {
                "missing" => new FileNotFoundException(),
                "uninspectable" => new UnauthorizedAccessException(),
                _ => null
            }
        };
        var telemetry = new RecordingTelemetry();
        var service = new ArtworkGenerationService(repository, files, new TestAiImageProvenanceCodec(), new Provider(), new Normalizer(), () => now, Guid.NewGuid, telemetry);
        var model = new AiModelDescriptor("image/model", "Image", null, null, ["text"], ["image"], [], null, null, null, null, true, null);
        var request = new ArtworkGenerationRequest(item.Id, area.Id, "secret", AiProfileSettings.Empty with { ModelId = model.Id }, [model],
            [new AiImageEndpointCapabilities("endpoint", model.Id, true, true, ["png"], [new(1200, 1400)], true)], false);

        var thrown = await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.GenerateAsync(request, TestContext.Current.CancellationToken));

        Assert.Same(cancellation, thrown);
        Assert.Equal(expectedCleanupStatus, thrown.Data["FusionCanvas.ManagedWorkspaceFileCleanup.Status"]);
        Assert.Same(initialSnapshot, repository.Snapshot);
        var cleanupEvent = telemetry.Events.SingleOrDefault(value => value.MetadataJson?.Contains("file_cleanup", StringComparison.Ordinal) == true);
        if (expectedCleanupStatus is "Failed" or "Uninspectable")
        {
            Assert.NotNull(cleanupEvent);
            Assert.Equal(expectedCleanupStatus, cleanupEvent.Outcome);
        }
        else
        {
            Assert.Null(cleanupEvent);
        }
    }

    [Fact]
    public async Task GenerateAsync_RejectsIncompleteDesignTriangleBeforeProviderDispatch()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
        var product = new StoreProduct(Guid.NewGuid(), store.Id, "Shirt", null, null, now, now, "{}");
        var offering = new FulfillmentOffering(Guid.NewGuid(), product.Id, "Provider", null, FulfillmentKind.FixedProvider, "Provider", null, now, now, "{}");
        var area = new DesignArea(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 1200, 1400, null, now, now, "{}");
        var item = new Item(Guid.NewGuid(), store.Id, null, null, "Fox", null, ItemStatus.Draft, WorkflowStage.Design, false, now, now,
            "{\"idea\":\"fox\",\"phrase\":\"RUN WITH PURPOSE\",\"graphicDirection\":\"bold fox\"}");
        var row = new DesignVariantRow(Guid.NewGuid(), item.Id, true, 0);
        var repository = new Repo(new WorkspaceSnapshot([store], [], [], [item], [], [], [], [], [])
        {
            StoreProducts = [product],
            FulfillmentOfferings = [offering],
            DesignAreas = [area],
            ItemListingConfigurations = [new(item.Id, offering.Id)],
            DesignVariantRows = [row],
            DesignVariantRowColors = [new(row.Id, "Black")]
        });
        var provider = new Provider();
        var service = new ArtworkGenerationService(repository, new Files(), new TestAiImageProvenanceCodec(), provider, new Normalizer(), () => now, Guid.NewGuid);
        var model = new AiModelDescriptor("image/model", "Image", null, null, ["text"], ["image"], [], null, null, null, null, true, null);
        var request = new ArtworkGenerationRequest(item.Id, area.Id, "secret", AiProfileSettings.Empty with { ModelId = model.Id }, [model],
            [new AiImageEndpointCapabilities("endpoint", model.Id, true, true, ["png"], [new(1200, 1400)], true)], false);

        var result = await service.GenerateAsync(request, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal("Complete the Concept idea, Phrase, and Graphic direction.", result.Error);
        Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public async Task GenerateAsync_DiscardsProviderResultWhenConfigurationChanges()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
        var product = new StoreProduct(Guid.NewGuid(), store.Id, "Shirt", null, null, now, now, "{}");
        var offering = new FulfillmentOffering(Guid.NewGuid(), product.Id, "Provider", null, FulfillmentKind.FixedProvider, "Provider", null, now, now, "{}");
        var replacementOffering = offering with { Id = Guid.NewGuid(), Name = "Replacement" };
        var area = new DesignArea(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 1200, 1400, null, now, now, "{}");
        var item = new Item(Guid.NewGuid(), store.Id, null, null, "Fox", null, ItemStatus.Draft, WorkflowStage.Design, false, now, now,
            "{\"idea\":\"fox\",\"concept.idea\":\"clever fox\",\"phrase\":\"RUN WITH PURPOSE\",\"graphicDirection\":\"bold fox\"}");
        var row = new DesignVariantRow(Guid.NewGuid(), item.Id, true, 0);
        var repository = new Repo(new WorkspaceSnapshot([store], [], [], [item], [], [], [], [], [])
        {
            StoreProducts = [product],
            FulfillmentOfferings = [offering, replacementOffering],
            DesignAreas = [area],
            ItemListingConfigurations = [new(item.Id, offering.Id)],
            DesignVariantRows = [row],
            DesignVariantRowColors = [new(row.Id, "Black")]
        });
        var provider = new Provider
        {
            BeforeReturn = () => repository.Snapshot = repository.Snapshot with
            {
                ItemListingConfigurations = [new(item.Id, replacementOffering.Id)]
            }
        };
        var files = new Files();
        var service = new ArtworkGenerationService(repository, files, new TestAiImageProvenanceCodec(), provider, new Normalizer(), () => now, Guid.NewGuid);
        var model = new AiModelDescriptor("image/model", "Image", null, null, ["text"], ["image"], [], null, null, null, null, true, null);
        var request = new ArtworkGenerationRequest(item.Id, area.Id, "secret", AiProfileSettings.Empty with { ModelId = model.Id }, [model],
            [new AiImageEndpointCapabilities("endpoint", model.Id, true, true, ["png"], [new(1200, 1400)], true)], false);

        var result = await service.GenerateAsync(request, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("context changed", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(repository.Snapshot.Assets);
        Assert.Null(files.SavedContent);
    }

    [Fact]
    public async Task GenerateAsync_StopsAfterProviderCancellationBeforeNormalization()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Store", null, false, now, now, "{}");
        var product = new StoreProduct(Guid.NewGuid(), store.Id, "Shirt", null, null, now, now, "{}");
        var offering = new FulfillmentOffering(Guid.NewGuid(), product.Id, "Provider", null, FulfillmentKind.FixedProvider, "Provider", null, now, now, "{}");
        var area = new DesignArea(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 1200, 1400, null, now, now, "{}");
        var item = new Item(Guid.NewGuid(), store.Id, null, null, "Fox", null, ItemStatus.Draft, WorkflowStage.Design, false, now, now,
            "{\"idea\":\"fox\",\"concept.idea\":\"clever fox\",\"phrase\":\"RUN WITH PURPOSE\",\"graphicDirection\":\"bold fox\"}");
        var row = new DesignVariantRow(Guid.NewGuid(), item.Id, true, 0);
        var repository = new Repo(new WorkspaceSnapshot([store], [], [], [item], [], [], [], [], [])
        {
            StoreProducts = [product],
            FulfillmentOfferings = [offering],
            DesignAreas = [area],
            ItemListingConfigurations = [new(item.Id, offering.Id)],
            DesignVariantRows = [row],
            DesignVariantRowColors = [new(row.Id, "Black")]
        });
        using var cancellation = new CancellationTokenSource();
        var provider = new Provider { BeforeReturn = cancellation.Cancel };
        var files = new Files();
        var service = new ArtworkGenerationService(repository, files, new TestAiImageProvenanceCodec(), provider, new Normalizer(), () => now, Guid.NewGuid);
        var model = new AiModelDescriptor("image/model", "Image", null, null, ["text"], ["image"], [], null, null, null, null, true, null);
        var request = new ArtworkGenerationRequest(item.Id, area.Id, "secret", AiProfileSettings.Empty with { ModelId = model.Id }, [model],
            [new AiImageEndpointCapabilities("endpoint", model.Id, true, true, ["png"], [new(1200, 1400)], true)], false);

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.GenerateAsync(request, cancellation.Token));

        Assert.Empty(repository.Snapshot.Assets);
        Assert.Null(files.SavedContent);
    }

    private sealed class Repo(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; set; } = snapshot;
        public Exception? SaveFailure { get; init; }
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            if (SaveFailure is not null) return Task.FromException(SaveFailure);
            Snapshot = snapshot;
            return Task.CompletedTask;
        }
    }

    private sealed class Provider : IAiImageGenerationProvider
    {
        public int Calls { get; private set; }
        public AiImageGenerationRequest? LastRequest { get; private set; }
        public Action? BeforeReturn { get; init; }
        public Task<(AiImageGenerationResult? Result, AiImageGenerationFailure? Failure)> GenerateAsync(AiImageGenerationRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            LastRequest = request;
            BeforeReturn?.Invoke();
            return Task.FromResult<(AiImageGenerationResult?, AiImageGenerationFailure?)>((new([1, 2, 3], "image/png", "OpenRouter", request.ModelId, request.ModelId), null));
        }
    }

    private sealed class Normalizer : IRasterArtworkNormalizer
    {
        public Task<RasterArtworkNormalizationResult> NormalizeAsync(Stream source, RasterArtworkNormalizationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new RasterArtworkNormalizationResult([137, 80, 78, 71], request.TargetSize, true, []));
    }

    private sealed class Files : IWorkspaceFileOutputStore
    {
        public Stream? SavedContent { get; private set; }
        public List<string> DeletedPaths { get; } = [];
        public Exception? SaveFailure { get; init; }
        public bool DeleteResult { get; init; } = true;
        public Exception? DeleteFailure { get; init; }
        public Exception? OpenReadFailure { get; init; }
        public string WorkspaceRoot => "workspace";
        public string ResolvePath(string workspaceRelativePath) => Path.Combine(WorkspaceRoot, workspaceRelativePath);
        public Task<ManagedWorkspaceFile> ImportAsync(string sourcePath, AssetKind kind, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ManagedWorkspaceFile> SaveAsync(string fileName, AssetKind kind, Stream content, CancellationToken cancellationToken = default)
        {
            SavedContent = content;
            if (SaveFailure is not null) return Task.FromException<ManagedWorkspaceFile>(SaveFailure);
            return Task.FromResult(new ManagedWorkspaceFile(fileName, kind, "assets/generated.png", "workspace/assets/generated.png", ""));
        }
        public bool Exists(string workspaceRelativePath) => true;
        public bool TryDelete(string workspaceRelativePath)
        {
            DeletedPaths.Add(workspaceRelativePath);
            if (DeleteFailure is not null) throw DeleteFailure;
            return DeleteResult;
        }
        public Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default) =>
            OpenReadFailure is null
                ? Task.FromResult<Stream>(new MemoryStream())
                : Task.FromException<Stream>(OpenReadFailure);
        public Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RecordingTelemetry : ITelemetryRecorder
    {
        public bool IsCaptureEnabled => true;
        public List<TelemetryEventRequest> Events { get; } = [];
        public Task RecordAsync(TelemetryEventRequest request, CancellationToken cancellationToken = default)
        {
            Events.Add(request);
            return Task.CompletedTask;
        }
    }
}
