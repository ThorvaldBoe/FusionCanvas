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
using FusionCanvas.Application.SllGeneration;
using FusionCanvas.Application.Tags;
using FusionCanvas.Application.WorkspaceTree;
using FusionCanvas.Application.Listings;

namespace FusionCanvas.App.Workspace;

public sealed record MainWindowApplicationServices(
    IStoreManagementService StoreManagement,
    INicheManagementService NicheManagement,
    ITagManagementService TagManagement,
    ILegacyCatalogCompatibilityService LegacyCatalogCompatibility,
    ICatalogSetupService CatalogSetup,
    IMockupTemplateSetupService MockupTemplateSetup,
    IOfferingManagementService OfferingManagement,
    IProviderCatalogCandidateSource ProviderCatalog,
    IMockupTemplateSourceImageService MockupTemplateSourceImages,
    IGroupManagementService GroupManagement,
    IItemManagementService ItemManagement,
    IItemCsvExportService ItemCsvExport,
    IItemCsvImportService ItemCsvImport,
    IAssetManagementService AssetManagement,
    IItemInspectorService ItemInspector,
    IIdeationService Ideation,
    IDesignStageService DesignStage,
    ISllDocumentCodec SllDocumentCodec,
    INichePopulationService? NichePopulation,
    IWorkspaceBatchRollbackService WorkspaceBatchRollback,
    IRasterImageMetadataReader? RasterImageMetadataReader = null,
    IMockupSourceMetadataAssistanceService? MockupSourceMetadataAssistance = null,
    IMockupSourceImageContentReader? MockupSourceImageContentReader = null,
    IMockupPlacementPreviewReader? MockupPlacementPreviewReader = null,
    IGlobalColorRemovalService? GlobalColorRemoval = null,
    IListingLifecycleServiceFactory? ListingLifecycle = null,
    IListingProjectionSource? ListingProjectionSource = null,
    IStoreContextMapper? StoreContextMapper = null);
