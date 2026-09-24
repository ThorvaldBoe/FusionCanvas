namespace FusionCanvas.Application.Telemetry;

public sealed record TelemetryEventRequest(
    string Area,
    string Name,
    string Severity,
    string Outcome,
    string Message,
    string? MetadataJson = null,
    string? RequestBody = null,
    string? ResponseBody = null,
    string? RequestDetailsJson = null,
    string? ResponseDetailsJson = null,
    Guid? CorrelationId = null);
