namespace FusionCanvas.Application.AI;

public enum AiMessageRole
{
    System,
    User,
    Assistant
}

public sealed record AiTextMessage(AiMessageRole Role, string Text);

public sealed record AiTextRequest(
    AiRequestPurpose Purpose,
    IReadOnlyList<AiTextMessage> Messages);
