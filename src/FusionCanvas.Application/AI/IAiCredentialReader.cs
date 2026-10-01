namespace FusionCanvas.Application.AI;

public interface IAiCredentialReader
{
    Task<AiCredentialReadResult> ReadAsync(CancellationToken cancellationToken = default);
}
