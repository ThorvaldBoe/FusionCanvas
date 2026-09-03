namespace FusionCanvas.Application.AI;

public sealed record AiCredentialOperationResult(bool Succeeded, string? Message = null)
{
    public static AiCredentialOperationResult Success { get; } = new(true);
    public static AiCredentialOperationResult Failed(string message) => new(false, message);
}
