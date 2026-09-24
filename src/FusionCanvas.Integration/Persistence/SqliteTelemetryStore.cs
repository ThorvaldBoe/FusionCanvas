using System.Globalization;
using FusionCanvas.Application.Telemetry;
using Microsoft.Data.Sqlite;

namespace FusionCanvas.Integration.Persistence;

public sealed class SqliteTelemetryStore(string databasePath) : ITelemetryStore
{
    private readonly string _databasePath = Path.GetFullPath(databasePath ?? throw new ArgumentNullException(nameof(databasePath)));

    public async Task<WorkspaceTelemetrySettings> ReadSettingsAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT debug_mode_enabled, retention_period, show_debug_window FROM workspace_telemetry_settings WHERE workspace_id = $workspace_id;";
        command.Parameters.AddWithValue("$workspace_id", workspaceId.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return WorkspaceTelemetrySettings.Default;
        var periodValue = reader.GetInt32(1);
        var period = Enum.IsDefined(typeof(TelemetryRetentionPeriod), periodValue)
            ? (TelemetryRetentionPeriod)periodValue
            : TelemetryRetentionPeriod.OneDay;
        return new WorkspaceTelemetrySettings(reader.GetInt32(0) != 0, period, reader.GetInt32(2) != 0);
    }

    public async Task SaveSettingsAsync(Guid workspaceId, WorkspaceTelemetrySettings settings, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO workspace_telemetry_settings(workspace_id, debug_mode_enabled, retention_period, show_debug_window)
            VALUES($workspace_id, $debug_mode_enabled, $retention_period, $show_debug_window)
            ON CONFLICT(workspace_id) DO UPDATE SET
                debug_mode_enabled = excluded.debug_mode_enabled,
                retention_period = excluded.retention_period,
                show_debug_window = excluded.show_debug_window;
            """;
        command.Parameters.AddWithValue("$workspace_id", workspaceId.ToString("D"));
        command.Parameters.AddWithValue("$debug_mode_enabled", settings.DebugModeEnabled ? 1 : 0);
        command.Parameters.AddWithValue("$retention_period", (int)settings.RetentionPeriod);
        command.Parameters.AddWithValue("$show_debug_window", settings.ShowDebugWindow ? 1 : 0);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(TelemetryEntry entry, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO telemetry_entries(
                id, workspace_id, occurred_at, area, event_name, severity, outcome, message,
                metadata_json, request_body, response_body, request_details_json, response_details_json, correlation_id)
            VALUES(
                $id, $workspace_id, $occurred_at, $area, $event_name, $severity, $outcome, $message,
                $metadata_json, $request_body, $response_body, $request_details_json, $response_details_json, $correlation_id);
            """;
        BindEntry(command, entry);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TelemetryEntry>> SearchAsync(Guid workspaceId, TelemetryQuery query, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        var sql = new System.Text.StringBuilder("SELECT * FROM telemetry_entries WHERE workspace_id = $workspace_id");
        command.Parameters.AddWithValue("$workspace_id", workspaceId.ToString("D"));
        if (query.FromInclusive is { } from)
        {
            sql.Append(" AND occurred_at >= $from");
            command.Parameters.AddWithValue("$from", from.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        }
        if (query.ToExclusive is { } to)
        {
            sql.Append(" AND occurred_at < $to");
            command.Parameters.AddWithValue("$to", to.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        }
        var areas = query.Areas?.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).ToArray() ?? [];
        if (areas.Length > 0)
        {
            var names = new List<string>(areas.Length);
            for (var index = 0; index < areas.Length; index++)
            {
                var name = $"$area{index}";
                names.Add(name);
                command.Parameters.AddWithValue(name, areas[index]);
            }
            sql.Append($" AND area IN ({string.Join(",", names)})");
        }
        sql.Append(" ORDER BY occurred_at DESC, id LIMIT $limit OFFSET $offset;");
        command.Parameters.AddWithValue("$limit", query.PageSize);
        command.Parameters.AddWithValue("$offset", query.Offset);
        command.CommandText = sql.ToString();
        return await ReadEntriesAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TelemetryEntry>> ReadAllAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM telemetry_entries WHERE workspace_id = $workspace_id ORDER BY occurred_at DESC, id;";
        command.Parameters.AddWithValue("$workspace_id", workspaceId.ToString("D"));
        return await ReadEntriesAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public Task<int> DeleteAllAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
        ExecuteCountAsync("DELETE FROM telemetry_entries WHERE workspace_id = $workspace_id;", workspaceId, null, cancellationToken);

    public Task<int> DeleteExpiredAsync(Guid workspaceId, DateTimeOffset cutoff, CancellationToken cancellationToken = default) =>
        ExecuteCountAsync("DELETE FROM telemetry_entries WHERE workspace_id = $workspace_id AND occurred_at < $cutoff;", workspaceId, cutoff, cancellationToken);

    private async Task<int> ExecuteCountAsync(string sql, Guid workspaceId, DateTimeOffset? cutoff, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$workspace_id", workspaceId.ToString("D"));
        if (cutoff is { } instant) command.Parameters.AddWithValue("$cutoff", instant.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _databasePath }.ToString());
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await SqliteDatabaseSchema.EnsureAsync(connection, cancellationToken).ConfigureAwait(false);
        return connection;
    }

    private static void BindEntry(SqliteCommand command, TelemetryEntry entry)
    {
        command.Parameters.AddWithValue("$id", entry.Id.ToString("D"));
        command.Parameters.AddWithValue("$workspace_id", entry.WorkspaceId.ToString("D"));
        command.Parameters.AddWithValue("$occurred_at", entry.OccurredAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$area", entry.Area);
        command.Parameters.AddWithValue("$event_name", entry.Name);
        command.Parameters.AddWithValue("$severity", entry.Severity);
        command.Parameters.AddWithValue("$outcome", entry.Outcome);
        command.Parameters.AddWithValue("$message", entry.Message);
        command.Parameters.AddWithValue("$metadata_json", (object?)entry.MetadataJson ?? DBNull.Value);
        command.Parameters.AddWithValue("$request_body", (object?)entry.RequestBody ?? DBNull.Value);
        command.Parameters.AddWithValue("$response_body", (object?)entry.ResponseBody ?? DBNull.Value);
        command.Parameters.AddWithValue("$request_details_json", (object?)entry.RequestDetailsJson ?? DBNull.Value);
        command.Parameters.AddWithValue("$response_details_json", (object?)entry.ResponseDetailsJson ?? DBNull.Value);
        command.Parameters.AddWithValue("$correlation_id", (object?)entry.CorrelationId?.ToString("D") ?? DBNull.Value);
    }

    private static async Task<IReadOnlyList<TelemetryEntry>> ReadEntriesAsync(SqliteCommand command, CancellationToken cancellationToken)
    {
        var entries = new List<TelemetryEntry>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            entries.Add(new TelemetryEntry(
                Guid.Parse(reader.GetString(0)), Guid.Parse(reader.GetString(1)),
                DateTimeOffset.Parse(reader.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7),
                ReadNullable(reader, 8), ReadNullable(reader, 9), ReadNullable(reader, 10), ReadNullable(reader, 11), ReadNullable(reader, 12),
                reader.IsDBNull(13) ? null : Guid.Parse(reader.GetString(13))));
        }
        return entries;
    }

    private static string? ReadNullable(SqliteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
}
