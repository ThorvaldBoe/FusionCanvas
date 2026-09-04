namespace FusionCanvas.Application.AI;

public sealed record AiCredentialReadResult(
    AiCredentialStateKind State,
    string? Secret = null,
    string? Message = null)
{
    public static AiCredentialReadResult NotFound { get; } = new(AiCredentialStateKind.NotFound);
    public static AiCredentialReadResult Available(string secret) => new(AiCredentialStateKind.Available, secret);
    public static AiCredentialReadResult Failure(AiCredentialStateKind state, string message) => new(state, Message: message);
}
