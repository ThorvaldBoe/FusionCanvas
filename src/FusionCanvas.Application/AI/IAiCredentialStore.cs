namespace FusionCanvas.Application.AI;

public interface IAiCredentialStore : IAiCredentialReader
{
    Task<AiCredentialOperationResult> SaveAsync(string apiKey, CancellationToken cancellationToken = default);
    Task<AiCredentialOperationResult> RemoveAsync(CancellationToken cancellationToken = default);
}
