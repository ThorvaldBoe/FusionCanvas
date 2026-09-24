using FusionCanvas.Application.Telemetry;
using FusionCanvas.Integration.Persistence;
using Microsoft.Data.Sqlite;

namespace FusionCanvas.Integration.Tests.Persistence;

public sealed class SqliteTelemetryStoreTests
{
    [Fact]
    public async Task Store_MigratesSchema18WithDebugModeOffAndOneDayRetention()
    {
        using var temp = new TemporaryDirectory();
        var path = temp.GetPath("legacy.db");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString()))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version = 18;";
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }
        var store = new SqliteTelemetryStore(path);

        var settings = await store.ReadSettingsAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);
        await using var verify = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
        await verify.OpenAsync(TestContext.Current.CancellationToken);
        await using var versionCommand = verify.CreateCommand();
        versionCommand.CommandText = "PRAGMA user_version;";

        Assert.Equal(WorkspaceTelemetrySettings.Default, settings);
        Assert.Equal(19L, (long)(await versionCommand.ExecuteScalarAsync(TestContext.Current.CancellationToken))!);
    }

    [Fact]
    public async Task Store_SearchesByWorkspaceTimeAndArea_AndDeletesExpiredRecords()
    {
        using var temp = new TemporaryDirectory();
        var store = new SqliteTelemetryStore(temp.GetPath("telemetry.db"));
        var workspace = Guid.NewGuid();
        var otherWorkspace = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await store.SaveSettingsAsync(workspace, WorkspaceTelemetrySettings.Default with { DebugModeEnabled = true });
        await store.AddAsync(Entry(workspace, now.AddMinutes(-2), "Design", "old"));
        await store.AddAsync(Entry(workspace, now.AddMinutes(-1), "Design", "new"));
        await store.AddAsync(Entry(workspace, now, "Workspace", "boundary"));
        await store.AddAsync(Entry(otherWorkspace, now, "Design", "other"));

        var results = await store.SearchAsync(workspace, new TelemetryQuery(
            now.AddMinutes(-1), now, ["Design"], PageSize: 10));
        var deleted = await store.DeleteExpiredAsync(workspace, now.AddSeconds(-30));

        Assert.Equal("new", Assert.Single(results).Message);
        Assert.Equal(2, deleted);
        Assert.Equal("boundary", Assert.Single(await store.ReadAllAsync(workspace)).Message);
        Assert.Equal(WorkspaceTelemetrySettings.Default with { DebugModeEnabled = true }, await store.ReadSettingsAsync(workspace));
    }

    [Fact]
    public async Task Store_UsesNewestFirstPagingAndWorkspaceScopedDeletion()
    {
        using var temp = new TemporaryDirectory();
        var store = new SqliteTelemetryStore(temp.GetPath("telemetry.db"));
        var workspace = Guid.NewGuid();
        var otherWorkspace = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await store.AddAsync(Entry(workspace, now.AddSeconds(-3), "Workspace", "first"));
        await store.AddAsync(Entry(workspace, now.AddSeconds(-2), "Workspace", "second"));
        await store.AddAsync(Entry(workspace, now.AddSeconds(-1), "Workspace", "third"));
        await store.AddAsync(Entry(otherWorkspace, now, "Workspace", "other"));

        var page = await store.SearchAsync(workspace, new TelemetryQuery(PageSize: 2));
        var deleted = await store.DeleteAllAsync(workspace);

        Assert.Equal(["third", "second"], page.Select(entry => entry.Message));
        Assert.Equal(3, deleted);
        Assert.Equal("other", Assert.Single(await store.ReadAllAsync(otherWorkspace)).Message);
    }

    private static TelemetryEntry Entry(Guid workspace, DateTimeOffset time, string area, string message) =>
        new(Guid.NewGuid(), workspace, time, area, "Test", "Information", "Succeeded", message, null, null, null, null, null, null);

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), "FusionCanvasTelemetryTests", Guid.NewGuid().ToString("N"));
        public string GetPath(string name) => Path.Combine(_path, name);
        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(_path)) Directory.Delete(_path, recursive: true);
        }
    }
}
