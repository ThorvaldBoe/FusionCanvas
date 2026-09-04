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
