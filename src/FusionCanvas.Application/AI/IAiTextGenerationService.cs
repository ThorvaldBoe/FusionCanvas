namespace FusionCanvas.Application.AI;

public interface IAiTextGenerationService
{
    Task<AiAvailabilityResult> GetAvailabilityAsync(
        AiRequestPurpose purpose,
        CancellationToken cancellationToken = default);

    Task<AiTextResult> GenerateAsync(
        AiTextRequest request,
        CancellationToken cancellationToken = default);
}
