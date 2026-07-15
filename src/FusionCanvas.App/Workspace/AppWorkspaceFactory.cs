using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Workspace;

namespace FusionCanvas.App.Workspace;

public sealed record AppWorkspaceRuntime(IWorkspaceRepository Repository, WorkspaceSnapshot Snapshot);

public static class AppWorkspaceFactory
{
    public static AppWorkspaceRuntime CreateDefault()
        => Create(DefaultDatabasePath());

    public static AppWorkspaceRuntime Create(string databasePath)
    {
        var repository = new SqliteWorkspaceRepository(databasePath);
        var snapshot = repository.LoadAsync().GetAwaiter().GetResult();
        return new AppWorkspaceRuntime(repository, snapshot);
    }

    private static string DefaultDatabasePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "FusionCanvas", "workspace.db");
    }
}
