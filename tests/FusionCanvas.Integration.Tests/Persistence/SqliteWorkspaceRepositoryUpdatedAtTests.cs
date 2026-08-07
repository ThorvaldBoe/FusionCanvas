using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Persistence;
using Microsoft.Data.Sqlite;

namespace FusionCanvas.Integration.Tests.Persistence;

public sealed class SqliteWorkspaceRepositoryUpdatedAtTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "fusioncanvas-rejection-updated-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task NeverEditedRejection_RoundTripsNullUpdatedAt()
    {
        var repository = new SqliteWorkspaceRepository(Path.Combine(_directory, "workspace.db"));
        var now = new DateTimeOffset(2026, 7, 28, 9, 0, 0, TimeSpan.Zero);
        var (store, niche, _) = SeedEntities(now);
        var rejection = new IdeationRejection(Guid.NewGuid(), store.Id, niche.Id, null, "Weak phrase", null, IdeationMode.Basic, now);

        await repository.SaveAsync(
            new WorkspaceSnapshot([store], [niche], [], [], [], [], [], [], []) { IdeationRejections = [rejection] },
            TestContext.Current.CancellationToken);
        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        var persisted = Assert.Single(loaded.IdeationRejections);
        Assert.Null(persisted.UpdatedAt);
        Assert.Equal(rejection, persisted);
    }

    [Fact]
    public async Task EditedRejection_RoundTripsUpdatedAt()
    {
        var repository = new SqliteWorkspaceRepository(Path.Combine(_directory, "workspace.db"));
        var now = new DateTimeOffset(2026, 7, 28, 9, 0, 0, TimeSpan.Zero);
        var (store, niche, _) = SeedEntities(now);
        var edited = new IdeationRejection(
            Guid.NewGuid(), store.Id, niche.Id, null, "Weak phrase", "Better reason", IdeationMode.Basic, now, now.AddMinutes(3));

        await repository.SaveAsync(
            new WorkspaceSnapshot([store], [niche], [], [], [], [], [], [], []) { IdeationRejections = [edited] },
            TestContext.Current.CancellationToken);
        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        var persisted = Assert.Single(loaded.IdeationRejections);
        Assert.NotNull(persisted.UpdatedAt);
        Assert.Equal(edited.UpdatedAt, persisted.UpdatedAt);
        Assert.Equal(edited, persisted);
    }

    [Fact]
    public async Task PreVersionEightDatabase_MigratesWithNullUpdatedAtAndIntactTables()
    {
        var path = Path.Combine(_directory, "migrate.db");
        var repository = new SqliteWorkspaceRepository(path);
        var now = new DateTimeOffset(2026, 7, 28, 9, 0, 0, TimeSpan.Zero);
        var (store, niche, group) = SeedEntities(now);
        var rejection = new IdeationRejection(Guid.NewGuid(), store.Id, niche.Id, group.Id, "Captured", "Off-brand", IdeationMode.Snowclones, now);
        await repository.SaveAsync(
            new WorkspaceSnapshot([store], [niche], [group], [], [], [], [], [], []) { IdeationRejections = [rejection] },
            TestContext.Current.CancellationToken);

        await using (var connection = new SqliteConnection($"Data Source={path}"))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version = 7;";
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(store, Assert.Single(loaded.Stores));
        Assert.Equal(niche, Assert.Single(loaded.Niches));
        Assert.Equal(group, Assert.Single(loaded.Groups));
        var persisted = Assert.Single(loaded.IdeationRejections);
        Assert.Null(persisted.UpdatedAt);
        Assert.Equal(rejection.Text, persisted.Text);
        Assert.Equal(rejection.Reason, persisted.Reason);

        await using var verify = new SqliteConnection($"Data Source={path}");
        await verify.OpenAsync(TestContext.Current.CancellationToken);
        await using var versionCommand = verify.CreateCommand();
        versionCommand.CommandText = "PRAGMA user_version;";
        Assert.Equal(10L, (long)(await versionCommand.ExecuteScalarAsync(TestContext.Current.CancellationToken))!);
    }

    [Fact]
    public async Task NewDatabase_IsCreatedAtVersionEight()
    {
        var path = Path.Combine(_directory, "fresh.db");
        var repository = new SqliteWorkspaceRepository(path);

        await repository.SaveAsync(new WorkspaceSnapshot([], [], [], [], [], [], [], [], []), TestContext.Current.CancellationToken);

        await using var connection = new SqliteConnection($"Data Source={path}");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        Assert.Equal(10L, (long)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!);
        await using var column = connection.CreateCommand();
        column.CommandText = "PRAGMA table_info(ideation_rejections);";
        var columnNames = new List<string>();
        await using var reader = await column.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            columnNames.Add(reader.GetString(1));
        }
        Assert.Contains("updated_at", columnNames);
    }

    private (Store store, Niche niche, TopicGroup group) SeedEntities(DateTimeOffset now)
    {
        var store = new Store(Guid.NewGuid(), "Dog Shop", null, false, now, now, "{}");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Dogs", null, false, now, now, "{}");
        var group = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Pugs", null, false, now, now, "{}");
        return (store, niche, group);
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
