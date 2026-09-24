namespace FusionCanvas.Application.Telemetry;

public sealed record TelemetryQuery(
    DateTimeOffset? FromInclusive = null,
    DateTimeOffset? ToExclusive = null,
    IReadOnlyCollection<string>? Areas = null,
    int PageSize = 100,
    int Offset = 0);
