namespace FusionCanvas.Application.AI;

public enum AiAvailabilityKind
{
    Checking,
    Ready,
    MissingCredential,
    CredentialUnavailable,
    MissingModel,
    InvalidConfiguration
}

public sealed record AiAvailabilityResult(AiAvailabilityKind Kind, string Message)
{
    public bool IsReady => Kind == AiAvailabilityKind.Ready;

    public static AiAvailabilityResult Ready { get; } =
        new(AiAvailabilityKind.Ready, "AI is ready.");
}
