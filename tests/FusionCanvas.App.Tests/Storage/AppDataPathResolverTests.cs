using FusionCanvas.App.Storage;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Persistence;

namespace FusionCanvas.App.Tests.Storage;

public sealed class AppDataPathResolverTests
{
    [Fact]
    public void Resolve_WhenProfileIsUnset_PreservesInstalledPaths()
    {
        var localAppData = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var paths = AppDataPathResolver.Resolve(null, null, null, null, null, localAppData);

        Assert.Equal(Path.Combine(localAppData, "FusionCanvas"), paths.Root);
        Assert.Equal(Path.Combine(localAppData, "FusionCanvas", "workspace.db"), paths.WorkspaceDatabase);
        Assert.Equal(Path.Combine(localAppData, "FusionCanvas", "workspace-files"), paths.WorkspaceFiles);
        Assert.Equal(Path.Combine(localAppData, "FusionCanvas", "settings.json"), paths.Settings);
    }

    [Fact]
    public void Resolve_WhenDevelopmentProfileIsSelected_IsolatesAllDefaultPaths()
    {
        var localAppData = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var developmentRoot = Path.Combine(localAppData, "FusionCanvas-Development");

        var paths = AppDataPathResolver.Resolve("Development", null, null, null, null, localAppData);

        Assert.Equal(developmentRoot, paths.Root);
        Assert.Equal(Path.Combine(developmentRoot, "workspace.db"), paths.WorkspaceDatabase);
        Assert.Equal(Path.Combine(developmentRoot, "workspace-files"), paths.WorkspaceFiles);
        Assert.Equal(Path.Combine(developmentRoot, "settings.json"), paths.Settings);
        Assert.NotEqual(Path.Combine(localAppData, "FusionCanvas", "workspace.db"), paths.WorkspaceDatabase);
    }

    [Fact]
    public void Resolve_WhenDataRootIsExplicit_NormalizesItAndOverridesTheProfileRoot()
    {
        var localAppData = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var rootOverride = Path.Combine(localAppData, "custom", "..", "custom-data");
        var expectedRoot = Path.GetFullPath(rootOverride);

        var paths = AppDataPathResolver.Resolve("Development", rootOverride, null, null, null, localAppData);

        Assert.Equal(expectedRoot, paths.Root);
        Assert.Equal(Path.Combine(expectedRoot, "workspace.db"), paths.WorkspaceDatabase);
        Assert.Equal(Path.Combine(expectedRoot, "workspace-files"), paths.WorkspaceFiles);
        Assert.Equal(Path.Combine(expectedRoot, "settings.json"), paths.Settings);
    }

    [Fact]
    public void Resolve_WhenSpecificPathsAreOverridden_UsesThemAndKeepsOtherPathsUnderSelectedRoot()
    {
        var localAppData = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var databaseOverride = Path.Combine(localAppData, "isolated", "workspace.db");
        var workspaceFilesOverride = Path.Combine(localAppData, "isolated", "files");
        var settingsOverride = Path.Combine(localAppData, "isolated", "settings.json");

        var paths = AppDataPathResolver.Resolve(
            "Development",
            null,
            databaseOverride,
            workspaceFilesOverride,
            settingsOverride,
            localAppData);

        Assert.Equal(Path.GetFullPath(databaseOverride), paths.WorkspaceDatabase);
        Assert.Equal(Path.GetFullPath(workspaceFilesOverride), paths.WorkspaceFiles);
        Assert.Equal(Path.GetFullPath(settingsOverride), paths.Settings);
    }

    [Fact]
    public void Resolve_WhenOnlyLegacyDatabaseOverrideIsSet_DerivesWorkspaceFilesBesideDatabase()
    {
        var localAppData = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var databaseOverride = Path.Combine(localAppData, "custom-workspace", "workspace.db");

        var paths = AppDataPathResolver.Resolve(null, null, databaseOverride, null, null, localAppData);

        Assert.Equal(Path.GetFullPath(databaseOverride), paths.WorkspaceDatabase);
        Assert.Equal(Path.Combine(Path.GetDirectoryName(databaseOverride)!, "workspace-files"), paths.WorkspaceFiles);
        Assert.Equal(Path.Combine(localAppData, "FusionCanvas", "settings.json"), paths.Settings);
    }

    [Fact]
    public void Resolve_WhenProfileIsUnknown_FailsBeforeReturningAnyPaths()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            AppDataPathResolver.Resolve("Develoment", null, null, null, null, Path.GetTempPath()));

        Assert.Contains("Unknown FusionCanvas data profile 'Develoment'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DevelopmentDatabaseInitializesEmptyWithoutTouchingInstalledData()
    {
        var localAppData = Path.Combine(Path.GetTempPath(), $"FusionCanvas-paths-{Guid.NewGuid():N}");
        var installedRoot = Path.Combine(localAppData, "FusionCanvas");
        Directory.CreateDirectory(installedRoot);
        var installedMarker = Path.Combine(installedRoot, "keep.txt");
        await File.WriteAllTextAsync(installedMarker, "installed data");

        try
        {
            var paths = AppDataPathResolver.Resolve("Development", null, null, null, null, localAppData);
            var repository = new SqliteWorkspaceRepository(paths.WorkspaceDatabase, useConnectionPooling: false);
            var snapshot = await repository.LoadAsync(TestContext.Current.CancellationToken);

            Assert.Empty(snapshot.Workspaces);
            await repository.SaveAsync(WorkspaceSnapshot.Empty, TestContext.Current.CancellationToken);
            Assert.True(File.Exists(paths.WorkspaceDatabase));
            Assert.Equal("installed data", await File.ReadAllTextAsync(installedMarker));
            Assert.False(File.Exists(Path.Combine(installedRoot, "workspace.db")));
        }
        finally
        {
            if (Directory.Exists(localAppData))
            {
                Directory.Delete(localAppData, recursive: true);
            }
        }
    }
}
