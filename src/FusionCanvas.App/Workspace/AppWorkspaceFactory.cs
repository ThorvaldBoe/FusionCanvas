using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Persistence;
using FusionCanvas.Integration.Files;
using FusionCanvas.Integration.Packages;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.Workspaces.Transfer;
using FusionCanvas.Application.Mockups;
using FusionCanvas.Application.Catalog;
using FusionCanvas.Application.DesignFiles;
using FusionCanvas.Application.Groups;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.Assets;
using FusionCanvas.Application.Tags;
using FusionCanvas.Application.Ideation;
using FusionCanvas.Application.Snowclones;
using FusionCanvas.Application.RejectedPhrases;
using FusionCanvas.Integration.Snowclones;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.Telemetry;
using FusionCanvas.Application.ConceptRefinement;
using FusionCanvas.Application.SllGeneration;
using FusionCanvas.Application.Products;
using FusionCanvas.Application.Catalog.Compatibility;
using FusionCanvas.Application.Items.Import;
using FusionCanvas.Application.TitleOptimization;
using FusionCanvas.Application.Niches;
using FusionCanvas.Application.ContentRisk;
using FusionCanvas.Application.Stores;
using FusionCanvas.Application.WorkspaceTree;
using FusionCanvas.Integration.AI;
using FusionCanvas.Integration.SllGeneration;
using FusionCanvas.Integration.Mockups;
using FusionCanvas.Application.Listings;
using FusionCanvas.Application.Stores.Printify;
using FusionCanvas.Integration.Stores.Printify;
using FusionCanvas.App.StageTools;

namespace FusionCanvas.App.Workspace;

public static class AppWorkspaceFactory
{
    public const string WorkspaceDatabaseEnvironmentVariable = "FUSIONCANVAS_WORKSPACE_DB";
    public const string WorkspaceRootEnvironmentVariable = "FUSIONCANVAS_WORKSPACE_ROOT";

    public static string ResolveDefaultDatabasePath() => DefaultDatabasePath();

    public static AppWorkspaceRuntime CreateDefault(
        IAiTextGenerationService ai,
        IAiImageGenerationProvider? artworkProvider = null,
        ITelemetryRecorder? telemetry = null,
        Guid? initialActiveWorkspaceId = null,
        Guid? initialActiveStoreId = null,
        CancellationToken cancellationToken = default)
        => Create(DefaultDatabasePath(), DefaultWorkspaceRoot(DefaultDatabasePath()), ai, artworkProvider, telemetry, initialActiveWorkspaceId, initialActiveStoreId, cancellationToken);

    public static AppWorkspaceRuntime Create(
        string databasePath,
        IAiTextGenerationService ai,
        IAiImageGenerationProvider? artworkProvider = null,
        ITelemetryRecorder? telemetry = null,
        Guid? initialActiveWorkspaceId = null,
        Guid? initialActiveStoreId = null,
        CancellationToken cancellationToken = default)
        => Create(databasePath, DefaultWorkspaceRoot(databasePath), ai, artworkProvider, telemetry, initialActiveWorkspaceId, initialActiveStoreId, cancellationToken);

    public static AppWorkspaceRuntime Create(
        string databasePath,
        string workspaceRootPath,
        IAiTextGenerationService ai,
        IAiImageGenerationProvider? artworkProvider = null,
        ITelemetryRecorder? telemetry = null,
        Guid? initialActiveWorkspaceId = null,
        Guid? initialActiveStoreId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ai);
        var repository = new SqliteWorkspaceRepository(databasePath);
        var contentRiskReviews = new ContentRiskReviewService(repository, new ConfiguredAiContentRiskAnalyzer(ai));
        Func<string, IWorkspaceRepository> packageRepositoryFactory =
            static path => new SqliteWorkspaceRepository(path, useConnectionPooling: false);
        var snowcloneRepository = new SqliteSnowcloneRepository(databasePath);
        var fileStore = new LocalWorkspaceFileStore(workspaceRootPath);
        var printifyCredentials = new NativeStorePrintifyCredentialStore();
        var printifyListingFactory = new PrintifyListingLifecycleServiceFactory(
            repository,
            printifyCredentials,
            fileStore,
            PrintifyListingClient.CreateHttpClient());
        var useUiTestPrintify = Environment.GetEnvironmentVariable(Program.UiTestModeEnvironmentVariable) == "1"
            && Environment.GetEnvironmentVariable(Program.UiTestPrintifyImportEnvironmentVariable) == "1";
        IPrintifyListingImportClient printifyImportClient = useUiTestPrintify
            ? new UiTestPrintifyListingImportClient()
            : new PrintifyListingImportClient(PrintifyListingImportClient.CreateApiHttpClient(), PrintifyListingImportClient.CreateImageHttpClient(), telemetry);
        IPrintifyCatalogClient printifyCatalogClient = useUiTestPrintify
            ? new UiTestPrintifyCatalogClient()
            : new PrintifyCatalogClient(PrintifyCatalogClient.CreateHttpClient(), telemetry);
        IStorePrintifyCredentialStore printifyImportCredentials = useUiTestPrintify ? new UiTestPrintifyCredentialStore() : printifyCredentials;
        var workspaceTransfer = new WorkspaceTransferService(
            repository,
            fileStore,
            new ZipWorkspacePackageWriter(packageRepositoryFactory),
            new ZipWorkspacePackageReader(packageRepositoryFactory));
        var rasterImageMetadata = new RasterImageMetadataReader();
        var mockupSourceImageContent = new LocalMockupSourceImageContentReader();
        var mockupPlacementPreviewReader = new LocalMockupPlacementPreviewReader();
        var mockupSourceMetadataAssistance = new MockupSourceMetadataAssistanceService(ai, mockupSourceImageContent);
        var snapshot = StartupTaskRunner.Run(
            token => repository.LoadAsync(token),
            cancellationToken);
        var itemManagement = new ItemManagementService(repository);
        var groupManagement = new GroupManagementService(repository);
        var assetManagement = new AssetManagementService(repository, fileStore);
        var tagManagement = new TagManagementService(repository);
        var itemInspector = new ItemInspectorService(repository, contentRiskReviews: contentRiskReviews);
        var storeManagement = new StoreManagementService(
            repository,
            new FusionCanvas.Integration.Stores.StoreContextMapper(),
            initialActiveWorkspaceId: initialActiveWorkspaceId,
            initialActiveStoreId: initialActiveStoreId);
        var nicheManagement = new NicheManagementService(repository);
        var productSupplierSetup = new LegacyCatalogCompatibilityService(repository);
        var catalogSetup = new CatalogSetupService(repository);
        var mockupTemplateSetup = new MockupTemplateSetupService(repository);
        var providerCatalog = new UnavailableProviderCatalogCandidateSource();
        var offeringManagement = new OfferingManagementService(repository, providerCatalog);
        var mockupTemplateSourceImages = new MockupTemplateSourceImageService(repository, fileStore, rasterImageMetadata, mockupTemplateSetup);
        var itemCsvExport = new ItemCsvExportService();
        var itemCsvImport = new ItemCsvImportService(repository);
        var aiImageProvenanceCodec = new AiImageProvenanceCodec();
        var mockupInvalidation = new MockupOutputInvalidationService(repository, fileStore);
        var designStage = new DesignStageService(repository, fileStore, aiImageProvenanceCodec, contentRiskReviews: contentRiskReviews, mockupInvalidation: mockupInvalidation);
        var globalColorRemoval = new GlobalColorRemovalService(
            repository,
            fileStore,
            fileStore,
            new ImageSharpGlobalColorRemovalProcessor(),
            contentRiskReviews);
        var sllDocumentCodec = new SllDocumentCodec();
        var nichePopulation = new NichePopulationService(ai);
        var ideationAccess = new ConfiguredIdeationAccessStatus(ai);
        var snowcloneLibrary = new SnowcloneLibraryService(
            snowcloneRepository,
            new SnowcloneCsvCodec(),
            new EmbeddedBundledSnowcloneSource());
        var snowcloneLibraryInitialization = StartupTaskRunner.Run(
            token => snowcloneLibrary.InitializeAsync(cancellationToken: token),
            cancellationToken);
        var rejectedPhrases = new RejectedPhraseManagementService(repository);
        var conceptRefinementAccess = new ConfiguredConceptRefinementAccessStatus(ai);
        var guidanceSource = new EmbeddedDesignTriangleGuidanceSource();
        var conceptRefinement = new ConceptRefinementService(
            repository,
            ai,
            guidanceSource);
        var sllGenerationAccess = new ConfiguredSllAccessStatus(ai);
        var sllGeneration = new SllGenerationService(
            repository,
            ai,
            guidanceSource);
        var titleOptimization = new TitleOptimizationService(repository, ai);
        var ideation = new IdeationService(
            repository,
            itemManagement,
            new AiIdeaGenerator(ai, guidanceSource),
            new PersistedSnowcloneCatalog(snowcloneLibrary),
            ideationAccess);
        var workspaceBatchRollback = new WorkspaceBatchRollbackService(repository);
        var mockupOutputResolutionSettings = new MockupOutputResolutionSettingsService(repository);
        var mainWindowServices = new MainWindowApplicationServices(
            storeManagement,
            nicheManagement,
            tagManagement,
            productSupplierSetup,
            catalogSetup,
            mockupTemplateSetup,
            offeringManagement,
            providerCatalog,
            mockupTemplateSourceImages,
            groupManagement,
            itemManagement,
            itemCsvExport,
            itemCsvImport,
            assetManagement,
            itemInspector,
            ideation,
            designStage,
            sllDocumentCodec,
            nichePopulation,
            workspaceBatchRollback,
            rasterImageMetadata,
            mockupSourceMetadataAssistance,
            mockupSourceImageContent,
            mockupPlacementPreviewReader,
            globalColorRemoval,
            mockupOutputResolutionSettings,
            printifyListingFactory,
            new WorkspaceListingProjectionSource(fileStore, rasterImageMetadata),
            new FusionCanvas.Integration.Stores.StoreContextMapper(),
            new FusionCanvas.Application.Listings.ManualListingDetailsService(repository),
            new PrintifyListingImportService(storeManagement, printifyImportCredentials, printifyImportClient, repository, fileStore, printifyCatalogClient));
        return new AppWorkspaceRuntime(
            repository,
            new WorkspaceManagementService(
                repository,
                new FusionCanvas.Integration.Workspaces.WorkspaceContextMapper(),
                initialActiveWorkspaceId: initialActiveWorkspaceId),
            fileStore,
            workspaceTransfer,
            rasterImageMetadata,
            snapshot,
            groupManagement,
            itemManagement,
            assetManagement,
            tagManagement,
            itemInspector,
            ideation,
            ideationAccess,
            snowcloneLibrary,
            rejectedPhrases,
            snowcloneLibraryInitialization,
            conceptRefinement,
            conceptRefinementAccess,
            sllGeneration,
            sllGenerationAccess,
            titleOptimization,
            productSupplierSetup,
            itemCsvImport,
            sllDocumentCodec,
            new MockupGenerationService(repository, fileStore, mockupTemplateSetup, new ImageSharpMockupRasterCompositor(), invalidation: mockupInvalidation),
            mainWindowServices,
            artworkProvider is null ? null : new ArtworkGenerationService(repository, fileStore, aiImageProvenanceCodec, artworkProvider, new ImageSharpArtworkNormalizer(), telemetry: telemetry, contentRiskReviews: contentRiskReviews));
    }

    private static string DefaultDatabasePath()
    {
        var overridePath = Environment.GetEnvironmentVariable(WorkspaceDatabaseEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return Path.GetFullPath(overridePath);
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "FusionCanvas", "workspace.db");
    }

    private static string DefaultWorkspaceRoot(string databasePath)
    {
        var overridePath = Environment.GetEnvironmentVariable(WorkspaceRootEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return Path.GetFullPath(overridePath);
        }

        var directory = Path.GetDirectoryName(databasePath);
        return string.IsNullOrWhiteSpace(directory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FusionCanvas", "workspace-files")
            : Path.Combine(directory, "workspace-files");
    }
}
