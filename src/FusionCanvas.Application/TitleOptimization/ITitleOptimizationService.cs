using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.TitleOptimization;

public interface ITitleOptimizationService
{
    Task<AiAvailabilityResult> GetAvailabilityAsync(CancellationToken cancellationToken = default);

    Task<TitleOptimizationResult> OptimizeAsync(
        TitleOptimizationRequest request,
        CancellationToken cancellationToken = default);
}
