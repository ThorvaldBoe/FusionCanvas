namespace FusionCanvas.Application.AI;

public interface IAiCredentialStore
{
    Task<AiCredentialReadResult> ReadAsync(CancellationToken cancellationToken = default);
    Task<AiCredentialOperationResult> SaveAsync(string apiKey, CancellationToken cancellationToken = default);
    Task<AiCredentialOperationResult> RemoveAsync(CancellationToken cancellationToken = default);
}
