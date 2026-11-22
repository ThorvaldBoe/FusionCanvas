using Microsoft.Data.Sqlite;

namespace FusionCanvas.Integration.Persistence;

internal static class SqliteDatabaseSchema
{
    internal const int CurrentVersion = 26;

    internal static Task EnsureAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken) =>
        SqliteWorkspaceRepository.EnsureSchemaCoreAsync(connection, CurrentVersion, cancellationToken);
}
