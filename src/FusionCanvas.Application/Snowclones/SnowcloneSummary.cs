namespace FusionCanvas.Application.Snowclones;

public sealed record SnowcloneSummary(
    Guid Id,
    string Phrase,
    string Guidance,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
