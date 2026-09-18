namespace FusionCanvas.Application.AI;

public sealed record AiImageUsage(long? InputTokens = null, long? OutputTokens = null, decimal? Cost = null);
