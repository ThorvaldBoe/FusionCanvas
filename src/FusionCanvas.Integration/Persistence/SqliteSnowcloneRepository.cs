using FusionCanvas.Application.Snowclones;
using FusionCanvas.Domain.Snowclones;
using Microsoft.Data.Sqlite;

namespace FusionCanvas.Integration.Persistence;

public sealed class SqliteSnowcloneRepository(string databasePath) : ISnowcloneRepository
{
    private readonly string _databasePath = databasePath;

    public async Task<SnowcloneLibrarySnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        EnsureDatabaseDirectory();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await SqliteDatabaseSchema.EnsureAsync(connection, cancellationToken);

        var snowclones = new List<Snowclone>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT id, phrase, guidance, created_at, updated_at
                FROM snowclones
                ORDER BY phrase COLLATE NOCASE, id;
                """;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                snowclones.Add(new Snowclone(
                    Guid.Parse(reader.GetString(0)),
                    reader.GetString(1),
                    reader.GetString(2),
                    DateTimeOffset.Parse(reader.GetString(3)),
                    DateTimeOffset.Parse(reader.GetString(4))));
            }
        }

        var starterInitialized = false;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT starter_initialized
                FROM snowclone_library_state
                WHERE singleton_id = 1;
                """;
            var value = await command.ExecuteScalarAsync(cancellationToken);
            starterInitialized = value is not null && Convert.ToInt32(value) == 1;
        }

        return new SnowcloneLibrarySnapshot(snowclones, starterInitialized);
    }

    public async Task SaveAsync(
        SnowcloneLibrarySnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ValidateSnapshot(snapshot);

        EnsureDatabaseDirectory();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await SqliteDatabaseSchema.EnsureAsync(connection, cancellationToken);
        await using var transaction =
            (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await using (var delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM snowclones;";
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var snowclone in snapshot.Snowclones)
        {
            await using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO snowclones (
                    id, phrase, normalized_phrase, guidance, created_at, updated_at)
                VALUES (
                    $id, $phrase, $normalized_phrase, $guidance, $created_at, $updated_at);
                """;
            insert.Parameters.AddWithValue("$id", snowclone.Id.ToString());
            insert.Parameters.AddWithValue("$phrase", snowclone.Phrase);
            insert.Parameters.AddWithValue(
                "$normalized_phrase",
                SnowcloneTemplatePolicy.CreateDuplicateKey(snowclone.Phrase));
            insert.Parameters.AddWithValue("$guidance", snowclone.Guidance);
            insert.Parameters.AddWithValue("$created_at", snowclone.CreatedAt.ToString("O"));
            insert.Parameters.AddWithValue("$updated_at", snowclone.UpdatedAt.ToString("O"));
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var state = connection.CreateCommand())
        {
            state.Transaction = transaction;
            state.CommandText = """
                INSERT INTO snowclone_library_state (singleton_id, starter_initialized)
                VALUES (1, $starter_initialized)
                ON CONFLICT(singleton_id) DO UPDATE SET
                    starter_initialized = excluded.starter_initialized;
                """;
            state.Parameters.AddWithValue("$starter_initialized", snapshot.StarterLibraryInitialized ? 1 : 0);
            await state.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        await command.ExecuteNonQueryAsync(cancellationToken);
        return connection;
    }

    private void EnsureDatabaseDirectory()
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(_databasePath))!;
        Directory.CreateDirectory(directory);
    }

    private static void ValidateSnapshot(SnowcloneLibrarySnapshot snapshot)
    {
        var ids = new HashSet<Guid>();
        var duplicateKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var snowclone in snapshot.Snowclones)
        {
            if (snowclone.Id == Guid.Empty || !ids.Add(snowclone.Id))
            {
                throw new InvalidOperationException("Snowclone identities must be non-empty and unique.");
            }

            var validation = SnowcloneTemplatePolicy.Validate(snowclone.Phrase, snowclone.Guidance);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(
                    $"Snowclone {snowclone.Id} is invalid: {validation.Error}");
            }

            if (!duplicateKeys.Add(validation.DuplicateKey))
            {
                throw new InvalidOperationException("Snowclone phrases must be unique after normalization.");
            }

            if (snowclone.UpdatedAt < snowclone.CreatedAt)
            {
                throw new InvalidOperationException("Snowclone update time cannot precede creation time.");
            }
        }
    }
}
