namespace FusionCanvas.Application.Telemetry;

public interface ITelemetryStore
{
    Task<WorkspaceTelemetrySettings> ReadSettingsAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task SaveSettingsAsync(Guid workspaceId, WorkspaceTelemetrySettings settings, CancellationToken cancellationToken = default);
    Task AddAsync(TelemetryEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TelemetryEntry>> SearchAsync(Guid workspaceId, TelemetryQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TelemetryEntry>> ReadAllAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<int> DeleteAllAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<int> DeleteExpiredAsync(Guid workspaceId, DateTimeOffset cutoff, CancellationToken cancellationToken = default);
}
