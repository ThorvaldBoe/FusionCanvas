using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.Niches;

public interface INichePopulationService
{
    Task<AiAvailabilityResult> GetAvailabilityAsync(CancellationToken cancellationToken = default);

    Task<NichePopulationResult> PopulateAsync(
        NichePopulationRequest request,
        CancellationToken cancellationToken = default);
}
