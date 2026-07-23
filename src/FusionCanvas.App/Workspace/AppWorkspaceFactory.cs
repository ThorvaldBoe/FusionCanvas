using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Workspace;

namespace FusionCanvas.App.Workspace;

public sealed record AppWorkspaceRuntime(
    IWorkspaceRepository Repository,
    IWorkspaceFileStore FileStore,
    WorkspaceSnapshot Snapshot,
    IGroupManagementService GroupManagement,
    IItemManagementService ItemManagement,
    IAssetManagementService AssetManagement,
    ITagManagementService TagManagement,
    IItemInspectorService ItemInspector);

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
        var fileStore = new LocalWorkspaceFileStore(workspaceRootPath);
        var snapshot = repository.LoadAsync().GetAwaiter().GetResult();
        return new AppWorkspaceRuntime(
            repository,
            fileStore,
            snapshot,
            new GroupManagementService(repository),
            new ItemManagementService(repository),
            new AssetManagementService(repository, fileStore),
            new TagManagementService(repository),
            new ItemInspectorService(repository));
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
