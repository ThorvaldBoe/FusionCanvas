using FusionCanvas.App.Views;
using FusionCanvas.App.Settings;
using FusionCanvas.App.Workspace;
using FusionCanvas.App.Workflow;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Products;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.WorkflowNavigation;
using FusionCanvas.Application.ToolContexts;
using FusionCanvas.Application.StageTools;
using FusionCanvas.Application.TitleOptimization;
using FusionCanvas.Application.Assets;
using FusionCanvas.Application.Catalog;
using FusionCanvas.Application.DesignFiles;
using FusionCanvas.Application.Groups;
using FusionCanvas.Application.Ideation;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.Items.Import;
using FusionCanvas.Application.Mockups;
using FusionCanvas.Application.Niches;
using FusionCanvas.Application.Products;
using FusionCanvas.Application.Catalog.Compatibility;
using FusionCanvas.Application.Stores;
using FusionCanvas.Application.Tags;
using FusionCanvas.Application.AI;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Ideation;
using FusionCanvas.Integration.Files;
using FusionCanvas.Integration.SllGeneration;

namespace FusionCanvas.App.Tests.TestSupport;

internal static class SampleWorkspace
{
    internal static readonly Guid StoreNodeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    internal static readonly Guid NicheNodeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    internal static readonly Guid TopicNodeId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    internal static readonly Guid IdeaNodeId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    internal static readonly Guid DesignNodeId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    internal static readonly Guid ListingNodeId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    internal static WorkspaceSnapshot Create()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(StoreNodeId, "North Star Studio", null, false, now, now, """{"brand":"North Star"}""");
        var niche = new Niche(NicheNodeId, store.Id, "Coffee", null, false, now, now, """{"tone":"warm"}""");
        var topic = new TopicGroup(TopicNodeId, store.Id, niche.Id, null, "Dogs and coffee", null, false, now, now, """{"humor":"gentle"}""");
        var idea = new Item(IdeaNodeId, store.Id, niche.Id, topic.Id, "Morning coffee idea", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, """{"phrase":"But first, walkies"}""");
        var design = new Item(DesignNodeId, store.Id, niche.Id, topic.Id, "Retro mug design", null, ItemStatus.Draft, WorkflowStage.Design, false, now, now, "{}");
        var item = new Item(ListingNodeId, store.Id, niche.Id, topic.Id, "Espresso listing draft", null, ItemStatus.Draft, WorkflowStage.Listing, false, now, now, "{}");

        var product = new StoreProduct(Guid.NewGuid(), store.Id, "Gildan 64000", "Blank tee", null, now, now, "{}");
        var offering = new FulfillmentOffering(Guid.NewGuid(), product.Id, "Printful", null, FulfillmentKind.FixedProvider, "Printful", null, now, now, "{}");
        var variant = new ProductVariant(Guid.NewGuid(), offering.Id, [new VariantOption("Color", "Black")], now, now);
        var area = new DesignArea(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 3000, 4500, [variant.Id], now, now, "{}");

        return new WorkspaceSnapshot(
            [store],
            [niche],
            [topic],
            [idea, design, item],
            [],
            [],
            [],
            [],
            [])
        {
            StoreProducts = [product],
            FulfillmentOfferings = [offering],
            ProductVariants = [variant],
            DesignAreas = [area],
        };
    }
}

internal sealed class InMemoryWorkspaceRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
{
    private WorkspaceSnapshot _snapshot = snapshot;

    internal WorkspaceSnapshot Snapshot => _snapshot;

    public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        _snapshot = snapshot;
        return Task.CompletedTask;
    }

    public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_snapshot);
}

internal static class MainWindowViewModelFactory
{
    internal static MainWindowViewModel CreateSample(
        ITitleOptimizationService? titleOptimization = null)
    {
        var snapshot = SampleWorkspace.Create();
        var repository = new InMemoryWorkspaceRepository(snapshot);
        return CreateFromSnapshot(snapshot, repository, titleOptimization);
    }

    internal static MainWindowViewModel CreateFromSnapshot(
        WorkspaceSnapshot snapshot,
        IWorkspaceRepository repository,
        ITitleOptimizationService? titleOptimization = null,
        IIdeationService? ideationService = null,
        IIdeationAccessStatus? ideationAccessStatus = null,
        IWorkspaceRepository? workspaceSnapshotRepository = null,
        SettingsViewModel? settings = null,
        CancellationToken cancellationToken = default,
        IItemInspectorService? itemInspectorService = null)
    {
        var fileStore = new EmptyWorkspaceFileStore();
        var itemManagement = new ItemManagementService(repository);
        var mockupTemplateSetup = new MockupTemplateSetupService(repository);
        var providerCatalog = new UnavailableProviderCatalogCandidateSource();
        var accessStatus = ideationAccessStatus ?? new DisabledIdeationAccessStatus();
        var services = new MainWindowApplicationServices(
            new StoreManagementService(repository, new FusionCanvas.Integration.Stores.StoreContextMapper()),
            new NicheManagementService(repository),
            new TagManagementService(repository),
            new LegacyCatalogCompatibilityService(repository),
            new CatalogSetupService(repository),
            mockupTemplateSetup,
            new OfferingManagementService(repository, providerCatalog),
            providerCatalog,
            new MockupTemplateSourceImageService(repository, fileStore, new RasterImageMetadataReader(), mockupTemplateSetup),
            new GroupManagementService(repository),
            itemManagement,
            new ItemCsvExportService(),
            new ItemCsvImportService(repository),
            new AssetManagementService(repository, fileStore),
            itemInspectorService ?? new ItemInspectorService(repository),
            ideationService ?? new IdeationService(
                repository,
                itemManagement,
                DisabledIdeaGenerator.Instance,
                EmptySnowcloneCatalog.Instance,
                accessStatus),
            new DesignStageService(repository, fileStore),
            new SllDocumentCodec(),
            null);

        return new(
            new WorkflowStageNavigatorViewModel(new WorkflowStageNavigatorService()),
            new DocumentWindow.DocumentWindowViewModel(),
            new ToolContextResolver(),
            new StageToolHostService(BuiltInStageTools.CreateDefaultRegistry(), new ToolContextResolver()),
            workspaceSnapshotRepository ?? repository,
            new WorkspaceManagementService(repository, new FusionCanvas.Integration.Workspaces.WorkspaceContextMapper()),
            snapshot,
            services,
            ideationAccessStatus: accessStatus,
            titleOptimizationService: titleOptimization,
            settings: settings,
            cancellationToken: cancellationToken);
    }

    private sealed class EmptyWorkspaceFileStore : IWorkspaceFileStore
    {
        public string WorkspaceRoot => string.Empty;

        public Task<ManagedWorkspaceFile> ImportAsync(string sourcePath, AssetKind kind, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The test workspace file store is not configured.");

        public bool Exists(string workspaceRelativePath) => false;

        public bool TryDelete(string workspaceRelativePath) => false;

        public Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The test workspace file store is not configured.");

        public Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The test workspace file store is not configured.");
    }

    private sealed class DisabledIdeationAccessStatus : IIdeationAccessStatus
    {
        public IdeationAccessAvailability GetAvailability() =>
            IdeationAccessAvailability.Unavailable("AI services were not supplied.");
    }

    private sealed class DisabledIdeaGenerator : IIdeaGenerator
    {
        public static DisabledIdeaGenerator Instance { get; } = new();

        public Task<IdeaGenerationResult> GenerateAsync(
            IdeationGenerationContext context,
            int requestIndex,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(IdeaGenerationResult.Failure(
                AiTextFailureKind.NotConfigured,
                "AI services were not supplied."));
    }

    private sealed class EmptySnowcloneCatalog : ISnowcloneCatalog
    {
        public static EmptySnowcloneCatalog Instance { get; } = new();

        public Task<SnowcloneCatalogResult> GetSelectionsAsync(
            int count,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(SnowcloneCatalogResult.Failure("Snowclone services were not supplied."));
    }
}
