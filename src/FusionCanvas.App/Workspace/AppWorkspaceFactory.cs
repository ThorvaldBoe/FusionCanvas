using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Persistence;
using FusionCanvas.Integration.Files;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.Groups;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.Assets;
using FusionCanvas.Application.Tags;
using FusionCanvas.Application.Snowclones;
using FusionCanvas.Integration.Snowclones;

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
    ISnowcloneLibraryService SnowcloneLibrary,
    SnowcloneLibraryResult SnowcloneLibraryInitialization);

public static class AppWorkspaceFactory
{
    public const string WorkspaceDatabaseEnvironmentVariable = "FUSIONCANVAS_WORKSPACE_DB";
    public const string WorkspaceRootEnvironmentVariable = "FUSIONCANVAS_WORKSPACE_ROOT";

    public static AppWorkspaceRuntime CreateDefault()
        => Create(DefaultDatabasePath(), DefaultWorkspaceRoot(DefaultDatabasePath()));

    public static AppWorkspaceRuntime Create(string databasePath)
        => Create(databasePath, DefaultWorkspaceRoot(databasePath));

    public static AppWorkspaceRuntime Create(string databasePath, string workspaceRootPath)
    {
        var repository = new SqliteWorkspaceRepository(databasePath);
        var snowcloneRepository = new SqliteSnowcloneRepository(databasePath);
        var fileStore = new LocalWorkspaceFileStore(workspaceRootPath);
        var snapshot = repository.LoadAsync().GetAwaiter().GetResult();
        var snowcloneLibrary = new SnowcloneLibraryService(
            snowcloneRepository,
            new SnowcloneCsvCodec(),
            new EmbeddedBundledSnowcloneSource());
        var snowcloneLibraryInitialization = snowcloneLibrary
            .InitializeAsync()
            .GetAwaiter()
            .GetResult();
        return new AppWorkspaceRuntime(
            repository,
            fileStore,
            snapshot,
            new GroupManagementService(repository),
            new ItemManagementService(repository),
            new AssetManagementService(repository, fileStore),
            new TagManagementService(repository),
            new ItemInspectorService(repository),
            snowcloneLibrary,
            snowcloneLibraryInitialization);
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
