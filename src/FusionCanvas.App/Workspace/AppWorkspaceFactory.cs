using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Persistence;
using FusionCanvas.Integration.Files;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.Groups;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.Assets;
using FusionCanvas.Application.Tags;
using FusionCanvas.Application.Ideation;
using FusionCanvas.Application.Snowclones;
using FusionCanvas.Application.RejectedPhrases;
using FusionCanvas.Integration.Snowclones;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.ConceptRefinement;
using FusionCanvas.Application.SllGeneration;
using FusionCanvas.Application.Products;
using FusionCanvas.Integration.AI;

namespace FusionCanvas.App.Workspace;

public sealed record AppWorkspaceRuntime(
    IWorkspaceRepository Repository,
    IWorkspaceFileStore FileStore,
    WorkspaceSnapshot Snapshot,
    IGroupManagementService GroupManagement,
    IItemManagementService ItemManagement,
    IAssetManagementService AssetManagement,
    ITagManagementService TagManagement,
    IItemInspectorService ItemInspector,
    IIdeationService Ideation,
    IIdeationAccessStatus IdeationAccess,
    ISnowcloneLibraryService SnowcloneLibrary,
    IRejectedPhraseManagementService RejectedPhrases,
    SnowcloneLibraryResult SnowcloneLibraryInitialization,
    IConceptRefinementService ConceptRefinement,
    IConceptRefinementAccessStatus ConceptRefinementAccess,
    ISllGenerationService SllGeneration,
    ISllAccessStatus SllGenerationAccess,
    IProductSupplierSetupService ProductSupplierSetup);

public static class AppWorkspaceFactory
{
    public const string WorkspaceDatabaseEnvironmentVariable = "FUSIONCANVAS_WORKSPACE_DB";
    public const string WorkspaceRootEnvironmentVariable = "FUSIONCANVAS_WORKSPACE_ROOT";

    public static AppWorkspaceRuntime CreateDefault(IAiTextGenerationService ai)
        => Create(DefaultDatabasePath(), DefaultWorkspaceRoot(DefaultDatabasePath()), ai);

    public static AppWorkspaceRuntime Create(string databasePath, IAiTextGenerationService ai)
        => Create(databasePath, DefaultWorkspaceRoot(databasePath), ai);

    public static AppWorkspaceRuntime Create(
        string databasePath,
        string workspaceRootPath,
        IAiTextGenerationService ai)
    {
        ArgumentNullException.ThrowIfNull(ai);
        var repository = new SqliteWorkspaceRepository(databasePath);
        var snowcloneRepository = new SqliteSnowcloneRepository(databasePath);
        var fileStore = new LocalWorkspaceFileStore(workspaceRootPath);
        var snapshot = StartupTaskRunner.Run(() => repository.LoadAsync());
        var itemManagement = new ItemManagementService(repository);
        var ideationAccess = new ConfiguredIdeationAccessStatus(ai);
        var snowcloneLibrary = new SnowcloneLibraryService(
            snowcloneRepository,
            new SnowcloneCsvCodec(),
            new EmbeddedBundledSnowcloneSource());
        var snowcloneLibraryInitialization = StartupTaskRunner.Run(
            () => snowcloneLibrary.InitializeAsync());
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
        return new AppWorkspaceRuntime(
            repository,
            fileStore,
            snapshot,
            new GroupManagementService(repository),
            itemManagement,
            new AssetManagementService(repository, fileStore),
            new TagManagementService(repository),
            new ItemInspectorService(repository),
            new IdeationService(
                repository,
                itemManagement,
                new AiIdeaGenerator(ai, guidanceSource),
                new PersistedSnowcloneCatalog(snowcloneLibrary),
                ideationAccess),
            ideationAccess,
            snowcloneLibrary,
            rejectedPhrases,
            snowcloneLibraryInitialization,
            conceptRefinement,
            conceptRefinementAccess,
            sllGeneration,
            sllGenerationAccess,
            new ProductSupplierSetupService(repository));
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
