using FusionCanvas.Application.ConceptRefinement;

namespace FusionCanvas.Application.SllGeneration;

public interface ISllGenerationService
{
    Task<SllGenerationResult> GenerateAsync(
        Guid itemId,
        ConceptRefinementTriangle triangle,
        string originalIdea,
        CancellationToken cancellationToken = default);
}
