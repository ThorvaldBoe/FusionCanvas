namespace FusionCanvas.Application.AI;

public sealed record AiModelEndpointCatalog(
    string ModelId,
    bool RequireZeroDataRetention,
    DateTimeOffset RetrievedAt,
    IReadOnlyList<AiModelEndpointDescriptor> Endpoints,
    bool IsStale = false);
