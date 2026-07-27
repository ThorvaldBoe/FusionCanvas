namespace FusionCanvas.Application.Snowclones;

public sealed record SnowcloneUpdateRequest(
    Guid Id,
    string Phrase,
    string Guidance,
    string? SearchText = null);
