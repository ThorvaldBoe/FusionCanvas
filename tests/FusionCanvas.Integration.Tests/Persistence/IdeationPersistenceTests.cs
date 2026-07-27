using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Persistence;
using Microsoft.Data.Sqlite;

namespace FusionCanvas.Integration.Tests.Persistence;

public sealed class IdeationPersistenceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "fusioncanvas-ideation-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Rejection_RoundTripsAndNewSchemaIsVersionSeven()
    {
        var path = Path.Combine(_directory, "workspace.db");
        var repository = new SqliteWorkspaceRepository(path);
        var now = new DateTimeOffset(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);
        var store = new Store(Guid.NewGuid(), "Dog Shop", null, false, now, now, "{}");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Dogs", null, false, now, now, "{}");
        var group = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Pugs", null, false, now, now, "{}");
        var rejection = new IdeationRejection(Guid.NewGuid(), store.Id, niche.Id, null, "Weak phrase", "Too generic", IdeationMode.Snowclones, now);
        var groupRejection = new IdeationRejection(Guid.NewGuid(), store.Id, niche.Id, group.Id, "Another phrase", null, IdeationMode.Basic, now.AddMinutes(1));
        var snapshot = new WorkspaceSnapshot([store], [niche], [group], [], [], [], [], [], [])
        {
            IdeationRejections = [rejection, groupRejection]
        };

        await repository.SaveAsync(snapshot, TestContext.Current.CancellationToken);
        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal([rejection, groupRejection], loaded.IdeationRejections);
        await using var connection = new SqliteConnection($"Data Source={path}");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        Assert.Equal(7L, (long)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!);
    }

    [Fact]
    public async Task VersionSixDatabase_MigratesWithoutChangingExistingData()
    {
        var path = Path.Combine(_directory, "migrate.db");
        var repository = new SqliteWorkspaceRepository(path);
        var now = new DateTimeOffset(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);
        var store = new Store(Guid.NewGuid(), "Existing", "Preserve me", false, now, now, """{"brand":"known"}""");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Dogs", "Existing niche", false, now, now, "{}");
        await repository.SaveAsync(
            new WorkspaceSnapshot([store], [niche], [], [], [], [], [], [], []),
            TestContext.Current.CancellationToken);

        await using (var connection = new SqliteConnection($"Data Source={path}"))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "DROP TABLE ideation_rejections; PRAGMA user_version = 6;";
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(store, Assert.Single(loaded.Stores));
        Assert.Equal(niche, Assert.Single(loaded.Niches));
        Assert.Empty(loaded.IdeationRejections);
    }

    [Fact]
    public async Task VersionSixMigrationFailure_RollsBackTableAndVersion()
    {
        var path = Path.Combine(_directory, "rollback.db");
        var repository = new SqliteWorkspaceRepository(path);
        await repository.SaveAsync(new WorkspaceSnapshot([], [], [], [], [], [], [], [], []), TestContext.Current.CancellationToken);

        await using (var connection = new SqliteConnection($"Data Source={path}"))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                PRAGMA foreign_keys = OFF;
                DROP TABLE ideation_rejections;
                INSERT INTO items (
                    id, store_id, niche_id, group_id, name, description, status,
                    workflow_stage, is_archived, created_at, updated_at, metadata_json)
                VALUES (
                    '10000000-0000-0000-0000-000000000001',
                    '20000000-0000-0000-0000-000000000002',
                    NULL, NULL, 'Orphan', NULL, 0, 0, 0,
                    '2026-07-27T12:00:00.0000000+00:00',
                    '2026-07-27T12:00:00.0000000+00:00', '{}');
                PRAGMA user_version = 6;
                PRAGMA foreign_keys = ON;
                """;
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.LoadAsync(TestContext.Current.CancellationToken));

        await using var verify = new SqliteConnection($"Data Source={path}");
        await verify.OpenAsync(TestContext.Current.CancellationToken);
        await using var version = verify.CreateCommand();
        version.CommandText = "PRAGMA user_version;";
        Assert.Equal(6L, (long)(await version.ExecuteScalarAsync(TestContext.Current.CancellationToken))!);
        await using var table = verify.CreateCommand();
        table.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'ideation_rejections';";
        Assert.Equal(0L, (long)(await table.ExecuteScalarAsync(TestContext.Current.CancellationToken))!);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
