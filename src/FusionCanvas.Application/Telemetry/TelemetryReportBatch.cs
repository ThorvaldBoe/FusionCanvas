namespace FusionCanvas.Application.Telemetry;

public sealed record TelemetryReportBatch(IReadOnlyList<TelemetryEntry> Entries, bool IsTruncated);
