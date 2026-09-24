namespace FusionCanvas.Application.Telemetry;

public interface ITelemetryService
{
    event EventHandler<TelemetryEntry>? EntryRecorded;

    bool IsCaptureEnabled { get; }

    Task<WorkspaceTelemetrySettings> GetSettingsAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task SaveSettingsAsync(Guid workspaceId, WorkspaceTelemetrySettings settings, CancellationToken cancellationToken = default);
    Task RecordAsync(TelemetryEventRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TelemetryEntry>> SearchAsync(Guid workspaceId, TelemetryQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TelemetryEntry>> ReadAllAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<int> DeleteAllAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<string> ExportJsonAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<int> CleanupExpiredAsync(Guid workspaceId, CancellationToken cancellationToken = default);
}
