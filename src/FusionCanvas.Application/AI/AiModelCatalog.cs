namespace FusionCanvas.Application.AI;

public sealed record AiModelCatalog(
    bool RequireZeroDataRetention,
    DateTimeOffset RetrievedAt,
    IReadOnlyList<AiModelDescriptor> Models,
    bool IsStale = false);
