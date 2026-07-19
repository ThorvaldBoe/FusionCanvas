using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Workspace;

namespace FusionCanvas.App.Workspace;

public sealed record AppWorkspaceRuntime(
    IWorkspaceRepository Repository,
    WorkspaceSnapshot Snapshot,
    IGroupManagementService GroupManagement,
    IListingManagementService ListingManagement,
    ITagManagementService TagManagement);

public static class AppWorkspaceFactory
{
    public const string WorkspaceDatabaseEnvironmentVariable = "FUSIONCANVAS_WORKSPACE_DB";

    public static AppWorkspaceRuntime CreateDefault()
        => Create(DefaultDatabasePath());

    public static AppWorkspaceRuntime Create(string databasePath)
    {
        var repository = new SqliteWorkspaceRepository(databasePath);
        var snapshot = repository.LoadAsync().GetAwaiter().GetResult();
        return new AppWorkspaceRuntime(
            repository,
            snapshot,
            new GroupManagementService(repository),
            new ListingManagementService(repository),
            new TagManagementService(repository));
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
}
