using FusionCanvas.Application.Snowclones;
using FusionCanvas.Domain.Snowclones;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Persistence;
using Microsoft.Data.Sqlite;

namespace FusionCanvas.Integration.Tests.Persistence;

public sealed class SqliteSnowcloneRepositoryTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-01-02T03:04:05Z");

    [Fact]
    public async Task LoadAsync_FreshDatabaseCreatesCurrentVersionEmptyLibrary()
    {
        using var tempDirectory = new TemporaryDirectory();
        var path = tempDirectory.GetPath("library.db");
        var repository = new SqliteSnowcloneRepository(path);

        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Empty(loaded.Snowclones);
        Assert.False(loaded.StarterLibraryInitialized);
        Assert.Equal(8, await ReadUserVersionAsync(path));
    }

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsSnowclonesAndStarterMarker()
    {
        using var tempDirectory = new TemporaryDirectory();
        var path = tempDirectory.GetPath("library.db");
        var repository = new SqliteSnowcloneRepository(path);
        var snowclone = Snowclone("Easily distracted by {X}", "Replace X.");

        await repository.SaveAsync(
            new SnowcloneLibrarySnapshot([snowclone], true),
            TestContext.Current.CancellationToken);
        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(snowclone, Assert.Single(loaded.Snowclones));
        Assert.True(loaded.StarterLibraryInitialized);
    }

    [Fact]
    public async Task LoadAsync_MigratesPopulatedVersionFiveToCurrentWithoutChangingWorkspace()
    {
        using var tempDirectory = new TemporaryDirectory();
        var path = tempDirectory.GetPath("workspace.db");
        var workspaceRepository = new SqliteWorkspaceRepository(path);
        var workspace = WorkspaceSnapshot.Empty with
        {
            Workspaces =
            [
                new Workspace(
                    Guid.NewGuid(),
                    "Existing workspace",
                    "Preserve me",
                    false,
                    Now,
                    Now,
                    "{}")
            ]
        };
        await workspaceRepository.SaveAsync(workspace, TestContext.Current.CancellationToken);
        await DowngradeToVersionFiveAsync(path);

        var snowcloneRepository = new SqliteSnowcloneRepository(path);
        var library = await snowcloneRepository.LoadAsync(TestContext.Current.CancellationToken);
        var reloadedWorkspace = await workspaceRepository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Empty(library.Snowclones);
        Assert.False(library.StarterLibraryInitialized);
        Assert.Equal(workspace.Workspaces, reloadedWorkspace.Workspaces);
        Assert.Equal(8, await ReadUserVersionAsync(path));
    }

    [Fact]
    public async Task WorkspaceSave_PreservesSnowcloneLibrary()
    {
        using var tempDirectory = new TemporaryDirectory();
        var path = tempDirectory.GetPath("shared.db");
        var snowcloneRepository = new SqliteSnowcloneRepository(path);
        var workspaceRepository = new SqliteWorkspaceRepository(path);
        var snowclone = Snowclone("Shared {X}", "Guidance");
        await snowcloneRepository.SaveAsync(
            new SnowcloneLibrarySnapshot([snowclone], true),
            TestContext.Current.CancellationToken);

        await workspaceRepository.SaveAsync(
            WorkspaceSnapshot.Empty with
            {
                Workspaces =
                [
                    new Workspace(Guid.NewGuid(), "Workspace", null, false, Now, Now, "{}")
                ]
            },
            TestContext.Current.CancellationToken);

        var reloaded = await snowcloneRepository.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(snowclone, Assert.Single(reloaded.Snowclones));
        Assert.True(reloaded.StarterLibraryInitialized);
    }

    [Fact]
    public async Task SnowcloneSave_PreservesWorkspaceContent()
    {
        using var tempDirectory = new TemporaryDirectory();
        var path = tempDirectory.GetPath("shared.db");
        var workspaceRepository = new SqliteWorkspaceRepository(path);
        var snowcloneRepository = new SqliteSnowcloneRepository(path);
        var workspace = new Workspace(Guid.NewGuid(), "Workspace", "Keep", false, Now, Now, "{}");
        await workspaceRepository.SaveAsync(
            WorkspaceSnapshot.Empty with { Workspaces = [workspace] },
            TestContext.Current.CancellationToken);

        await snowcloneRepository.SaveAsync(
            new SnowcloneLibrarySnapshot([Snowclone("Global {X}", "Guidance")], true),
            TestContext.Current.CancellationToken);

        var reloaded = await workspaceRepository.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(workspace, Assert.Single(reloaded.Workspaces));
    }

    [Fact]
    public async Task SaveAsync_InvalidDuplicateSnapshotLeavesConfirmedLibrary()
    {
        using var tempDirectory = new TemporaryDirectory();
        var path = tempDirectory.GetPath("library.db");
        var repository = new SqliteSnowcloneRepository(path);
        var confirmed = Snowclone("Unique {X}", "Confirmed");
        await repository.SaveAsync(
            new SnowcloneLibrarySnapshot([confirmed], true),
            TestContext.Current.CancellationToken);
        var duplicate = Snowclone(" unique {x} ", "Duplicate");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.SaveAsync(
                new SnowcloneLibrarySnapshot([confirmed, duplicate], true),
                TestContext.Current.CancellationToken));

        var reloaded = await repository.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(confirmed, Assert.Single(reloaded.Snowclones));
    }

    [Fact]
    public async Task Repositories_CanOpenInEitherOrder()
    {
        using var firstDirectory = new TemporaryDirectory();
        var firstPath = firstDirectory.GetPath("first.db");
        await new SqliteWorkspaceRepository(firstPath).SaveAsync(
            WorkspaceSnapshot.Empty,
            TestContext.Current.CancellationToken);
        var firstLibrary = await new SqliteSnowcloneRepository(firstPath)
            .LoadAsync(TestContext.Current.CancellationToken);

        using var secondDirectory = new TemporaryDirectory();
        var secondPath = secondDirectory.GetPath("second.db");
        var secondLibrary = await new SqliteSnowcloneRepository(secondPath)
            .LoadAsync(TestContext.Current.CancellationToken);
        var secondWorkspace = await new SqliteWorkspaceRepository(secondPath)
            .LoadAsync(TestContext.Current.CancellationToken);

        Assert.Empty(firstLibrary.Snowclones);
        Assert.Empty(secondLibrary.Snowclones);
        Assert.Empty(secondWorkspace.Workspaces);
        Assert.Equal(8, await ReadUserVersionAsync(firstPath));
        Assert.Equal(8, await ReadUserVersionAsync(secondPath));
    }

    [Fact]
    public async Task LoadAsync_NewerSchemaVersionIsRefused()
    {
        using var tempDirectory = new TemporaryDirectory();
        var path = tempDirectory.GetPath("newer.db");
        await using (var connection = new SqliteConnection($"Data Source={path}"))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version = 9;";
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new SqliteSnowcloneRepository(path)
                .LoadAsync(TestContext.Current.CancellationToken));

        Assert.Contains("requires a newer FusionCanvas version", error.Message);
    }

    private static Snowclone Snowclone(string phrase, string guidance) =>
        new(Guid.NewGuid(), phrase, guidance, Now, Now);

    private static async Task DowngradeToVersionFiveAsync(string path)
    {
        SqliteConnection.ClearAllPools();
        await using var connection = new SqliteConnection($"Data Source={path}");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DROP TABLE snowclones;
            DROP TABLE snowclone_library_state;
            PRAGMA user_version = 5;
            """;
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<int> ReadUserVersionAsync(string path)
    {
        await using var connection = new SqliteConnection($"Data Source={path}");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return Convert.ToInt32(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory();

        public string GetPath(string fileName) => Path.Combine(_directory.FullName, fileName);

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            _directory.Delete(recursive: true);
        }
    }
}
