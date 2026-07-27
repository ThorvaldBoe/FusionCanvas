namespace FusionCanvas.Domain.Snowclones;

public sealed record Snowclone(
    Guid Id,
    string Phrase,
    string Guidance,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
