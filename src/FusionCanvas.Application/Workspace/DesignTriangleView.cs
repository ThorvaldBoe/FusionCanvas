using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspace;

public sealed record DesignTriangleState(
    Guid? SelectedConceptId,
    string? Idea, string? Phrase, string? GraphicDirection,
    bool PhraseNotUsed, bool GraphicNotUsed,
    string? SelectedNode, string? ScoreJson, string? Notes,
    bool IsAvailable)
{
    public static DesignTriangleState Unavailable { get; } = new(null, null, null, null, false, false, null, null, null, false);
}

public interface IDesignTriangleViewService
{
    Task<DesignTriangleState> LoadAsync(Guid listingId, CancellationToken cancellationToken = default);
}

public sealed class DesignTriangleViewService : IDesignTriangleViewService
{
    private readonly IWorkspaceRepository _repository;

    public DesignTriangleViewService(IWorkspaceRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<DesignTriangleState> LoadAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var listing = snapshot.Listings.SingleOrDefault(l => l.Id == listingId);
        if (listing is null) return DesignTriangleState.Unavailable;

        var selectedId = GetSelectedConceptId(snapshot, listingId);
        if (selectedId is not Guid id) return DesignTriangleState.Unavailable;

        var concept = snapshot.Concepts.SingleOrDefault(c => c.Id == id);
        if (concept is null) return DesignTriangleState.Unavailable;

        return new DesignTriangleState(
            concept.Id, concept.Idea, concept.Phrase, concept.GraphicDirection,
            string.IsNullOrEmpty(concept.Phrase) && concept.MetadataJson.Contains("phrase.notUsed"),
            string.IsNullOrEmpty(concept.GraphicDirection) && concept.MetadataJson.Contains("graphic.notUsed"),
            "idea", concept.ScoreJson, concept.QualityNotes, true);
    }

    private static Guid? GetSelectedConceptId(WorkspaceSnapshot snapshot, Guid listingId)
    {
        var listing = snapshot.Listings.SingleOrDefault(l => l.Id == listingId);
        if (listing is null) return null;
        var metadata = ListingMetadataCodec.ParseMetadata(listing.MetadataJson);
        return metadata.TryGetValue("listing.selectedConceptId", out var value) && Guid.TryParse(value, out var id) ? id : null;
    }
}
