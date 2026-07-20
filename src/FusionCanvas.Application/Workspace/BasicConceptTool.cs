using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspace;

public interface IConceptRefinementProvider
{
    Task<ConceptRefinementResult> ImproveNodeAsync(ConceptRefinementRequest request, CancellationToken cancellationToken = default);
    Task<ConceptScoreResult> ScoreAsync(Guid conceptId, CancellationToken cancellationToken = default);
}

public sealed record ConceptRefinementRequest(
    Guid ListingId, Guid ConceptId, string SelectedNode,
    string? Idea, string? Phrase, string? GraphicDirection,
    string? StoreContext, string? NicheContext, string? TopicPath);

public sealed record ConceptRefinementResult(bool Succeeded, string? Suggestion, string? Error);

public sealed record ConceptScoreResult(bool Succeeded, string? ScoreJson, string? Error);

public interface IBasicConceptToolService
{
    Task<ConceptRefinementResult> RequestAiImprovementAsync(Guid listingId, string selectedNode, CancellationToken cancellationToken = default);
    Task<bool> AdvanceToDesignAsync(Guid listingId, CancellationToken cancellationToken = default);
}

public sealed class BasicConceptToolService : IBasicConceptToolService
{
    private readonly IConceptVersionsService _conceptVersions;
    private readonly IConceptRefinementProvider? _provider;

    public BasicConceptToolService(IConceptVersionsService conceptVersions, IConceptRefinementProvider? provider = null)
    {
        _conceptVersions = conceptVersions ?? throw new ArgumentNullException(nameof(conceptVersions));
        _provider = provider;
    }

    public async Task<ConceptRefinementResult> RequestAiImprovementAsync(Guid listingId, string selectedNode, CancellationToken cancellationToken = default)
    {
        if (_provider is null)
        {
            return new ConceptRefinementResult(false, null, "An AI concept-refinement provider must be configured.");
        }

        var state = await _conceptVersions.LoadAsync(listingId, cancellationToken).ConfigureAwait(false);
        if (state.SelectedConcept is null)
        {
            return new ConceptRefinementResult(false, null, "No concept is selected.");
        }

        var concept = state.SelectedConcept;
        return await _provider.ImproveNodeAsync(new ConceptRefinementRequest(
            listingId, concept.Id, selectedNode,
            concept.Idea, concept.Phrase, concept.GraphicDirection,
            null, null, null), cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> AdvanceToDesignAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        var state = await _conceptVersions.LoadAsync(listingId, cancellationToken).ConfigureAwait(false);
        return state.SelectedConcept is not null;
    }
}
