namespace FusionCanvas.Application.AI;

public interface IAiImageGenerationProvider
{
    Task<(AiImageGenerationResult? Result, AiImageGenerationFailure? Failure)> GenerateAsync(AiImageGenerationRequest request, CancellationToken cancellationToken = default);
}
