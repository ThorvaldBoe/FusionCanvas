namespace FusionCanvas.Application.AI;

public sealed record AiTextRequest(
    AiRequestPurpose Purpose,
    IReadOnlyList<AiTextMessage> Messages);
