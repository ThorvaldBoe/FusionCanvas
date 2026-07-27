namespace FusionCanvas.Application.Snowclones;

public sealed record SnowcloneCreateRequest(
    string Phrase,
    string Guidance,
    string? SearchText = null);
