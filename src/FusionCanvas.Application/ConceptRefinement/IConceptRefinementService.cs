namespace FusionCanvas.Application.ConceptRefinement;

public interface IConceptRefinementService
{
    Task<ConceptRefinementResult> InitializeAsync(
        Guid itemId,
        string originalIdea,
        CancellationToken cancellationToken = default);

    Task<ConceptRefinementResult> RefineAsync(
        Guid itemId,
        ConceptRefinementActionKind action,
        ConceptRefinementCorner corner,
        ConceptRefinementTriangle current,
        string originalIdea,
        string? instruction,
        CancellationToken cancellationToken = default);
}