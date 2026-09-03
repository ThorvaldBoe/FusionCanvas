namespace FusionCanvas.Application.AI;

public interface IAiCredentialValidator
{
    Task<AiCredentialValidationResult> ValidateAsync(string apiKey, CancellationToken cancellationToken = default);
}
