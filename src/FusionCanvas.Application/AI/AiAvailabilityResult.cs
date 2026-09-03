namespace FusionCanvas.Application.AI;

public sealed record AiAvailabilityResult(AiAvailabilityKind Kind, string Message)
{
    public bool IsReady => Kind == AiAvailabilityKind.Ready;

    public static AiAvailabilityResult Ready { get; } =
        new(AiAvailabilityKind.Ready, "AI is ready.");
}
