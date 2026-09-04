namespace FusionCanvas.Application.AI;

public sealed record AiProviderTextRequest(
    string ApiKey,
    string ModelId,
    IReadOnlyList<AiTextMessage> Messages,
    AiProfileSettings Profile,
    bool RequireZeroDataRetention);
