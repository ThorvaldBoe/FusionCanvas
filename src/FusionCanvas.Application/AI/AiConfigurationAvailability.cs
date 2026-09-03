namespace FusionCanvas.Application.AI;

public enum AiConfigurationAvailability
{
    Ready,
    MissingModel,
    ModelUnavailable,
    PrivacyIncompatible,
    InvalidParameters
}
