namespace FusionCanvas.App.Storage;

internal static class AppDataPathResolver
{
    internal const string DataProfileEnvironmentVariable = "FUSIONCANVAS_DATA_PROFILE";
    internal const string DataRootEnvironmentVariable = "FUSIONCANVAS_DATA_ROOT";

    private const string DevelopmentProfile = "Development";

    public static AppDataPaths ResolveFromEnvironment()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Resolve(
            Environment.GetEnvironmentVariable(DataProfileEnvironmentVariable),
            Environment.GetEnvironmentVariable(DataRootEnvironmentVariable),
            Environment.GetEnvironmentVariable(Workspace.AppWorkspaceFactory.WorkspaceDatabaseEnvironmentVariable),
            Environment.GetEnvironmentVariable(Workspace.AppWorkspaceFactory.WorkspaceRootEnvironmentVariable),
            Environment.GetEnvironmentVariable(Settings.AppSettingsFactory.SettingsPathEnvironmentVariable),
            localAppData);
    }

    internal static AppDataPaths Resolve(
        string? profile,
        string? dataRootOverride,
        string? databaseOverride,
        string? workspaceFilesOverride,
        string? settingsOverride,
        string localAppData)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localAppData);

        var normalizedProfile = string.IsNullOrWhiteSpace(profile) ? null : profile.Trim();
        if (normalizedProfile is not null &&
            !string.Equals(normalizedProfile, DevelopmentProfile, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unknown FusionCanvas data profile '{normalizedProfile}'. Supported profile: {DevelopmentProfile}.");
        }

        var hasSelectedRoot = !string.IsNullOrWhiteSpace(dataRootOverride) || normalizedProfile is not null;
        var root = !string.IsNullOrWhiteSpace(dataRootOverride)
            ? Path.GetFullPath(dataRootOverride)
            : Path.Combine(
                localAppData,
                normalizedProfile is null ? "FusionCanvas" : "FusionCanvas-Development");

        var databasePath = ResolveOverride(databaseOverride) ?? Path.Combine(root, "workspace.db");
        var workspaceFilesPath = ResolveOverride(workspaceFilesOverride);
        if (workspaceFilesPath is null)
        {
            var workspaceFilesDirectory = hasSelectedRoot
                ? root
                : Path.GetDirectoryName(databasePath) ?? localAppData;
            workspaceFilesPath = Path.Combine(workspaceFilesDirectory, "workspace-files");
        }

        var settingsPath = ResolveOverride(settingsOverride) ?? Path.Combine(root, "settings.json");

        return new AppDataPaths(root, databasePath, workspaceFilesPath, settingsPath);
    }

    private static string? ResolveOverride(string? path) =>
        string.IsNullOrWhiteSpace(path) ? null : Path.GetFullPath(path);
}
