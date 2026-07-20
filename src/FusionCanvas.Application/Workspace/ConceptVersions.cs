using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspace;

public sealed record ConceptSummary(
    Guid Id,
    Guid StoreId,
    Guid ListingId,
    string Name,
    string? Idea,
    string? Phrase,
    string? GraphicDirection,
    string? AudienceReaction,
    string? Risks,
    string? QualityNotes,
    string? ScoreJson,
    ConceptLifecycle Lifecycle,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ConceptVersionsState(
    Guid? ActiveStoreId,
    Guid? ActiveListingId,
    ConceptSummary? SelectedConcept,
    IReadOnlyList<ConceptSummary> ActiveConcepts,
    IReadOnlyList<ConceptSummary> SupersededConcepts,
    IReadOnlyList<ConceptSummary> RejectedConcepts)
{
    public static ConceptVersionsState Empty { get; } = new(null, null, null, [], [], []);
}

public sealed record ConceptVersionsResult(
    bool Succeeded,
    string? Error,
    ConceptSummary? Concept,
    ConceptVersionsState State)
{
    public static ConceptVersionsResult Success(ConceptSummary? concept, ConceptVersionsState state) =>
        new(true, null, concept, state);

    public static ConceptVersionsResult Failure(string error, ConceptVersionsState state) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Concept operation failed." : error, null, state);
}

public sealed record ConceptCreateRequest(
    Guid ListingId,
    string? Name = null,
    string? Idea = null,
    string? Phrase = null,
    string? GraphicDirection = null,
    string? AudienceReaction = null,
    string? Risks = null,
    string? QualityNotes = null);

public sealed record ConceptEditRequest(
    Guid ConceptId,
    string? Name = null,
    string? Idea = null,
    string? Phrase = null,
    string? GraphicDirection = null,
    string? AudienceReaction = null,
    string? Risks = null,
    string? QualityNotes = null,
    string? ScoreJson = null);

public sealed record ConceptScoreRequest(Guid ConceptId, string ScoreJson);

public interface IConceptVersionsService
{
    Guid? ActiveStoreId { get; }
    Guid? ActiveListingId { get; }
    Guid? ActiveWorkspaceId { get; }

    void SetActiveWorkspace(Guid? workspaceId);
    Task<ConceptVersionsState> LoadAsync(Guid? listingId, CancellationToken cancellationToken = default);
    Task<ConceptVersionsResult> CreateAsync(ConceptCreateRequest request, CancellationToken cancellationToken = default);
    Task<ConceptVersionsResult> EditAsync(ConceptEditRequest request, CancellationToken cancellationToken = default);
    Task<ConceptVersionsResult> SupersedeAsync(ConceptCreateRequest request, CancellationToken cancellationToken = default);
    Task<ConceptVersionsResult> RejectAsync(Guid conceptId, CancellationToken cancellationToken = default);
    Task<ConceptVersionsResult> SelectAsync(Guid conceptId, CancellationToken cancellationToken = default);
    Task<ConceptVersionsResult> UpdateScoreAsync(ConceptScoreRequest request, CancellationToken cancellationToken = default);
}

public sealed class ConceptVersionsService : IConceptVersionsService
{
    private readonly IWorkspaceRepository _repository;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;
    private Guid? _activeWorkspaceId;
    private Guid? _activeStoreId;
    private Guid? _activeListingId;

    public ConceptVersionsService(
        IWorkspaceRepository repository,
        Func<DateTimeOffset>? clock = null,
        Func<Guid>? newId = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newId = newId ?? Guid.NewGuid;
    }

    public Guid? ActiveStoreId => _activeStoreId;
    public Guid? ActiveListingId => _activeListingId;
    public Guid? ActiveWorkspaceId => _activeWorkspaceId;

    public void SetActiveWorkspace(Guid? workspaceId) => _activeWorkspaceId = workspaceId;

    public async Task<ConceptVersionsState> LoadAsync(Guid? listingId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return BuildState(snapshot, listingId);
    }

    public async Task<ConceptVersionsResult> CreateAsync(ConceptCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var listing = snapshot.Listings.SingleOrDefault(candidate => candidate.Id == request.ListingId);
        if (listing is null)
        {
            return ConceptVersionsResult.Failure("Listing was not found.", BuildState(snapshot, _activeListingId));
        }

        var now = _clock();
        var name = string.IsNullOrWhiteSpace(request.Name) ? (request.Idea ?? listing.Name) : request.Name.Trim();
        var concept = new Concept(
            _newId(),
            listing.StoreId,
            listing.Id,
            name,
            null,
            request.Idea,
            request.Phrase,
            request.GraphicDirection,
            request.AudienceReaction,
            request.Risks,
            request.QualityNotes,
            null,
            ConceptLifecycle.Active,
            false,
            now,
            now,
            "{}");

        var existingActive = snapshot.Concepts.Where(c => c.ListingId == listing.Id && c.Lifecycle == ConceptLifecycle.Active).ToArray();
        var isFirst = !existingActive.Any();
        var updated = snapshot with { Concepts = [.. snapshot.Concepts, concept] };

        if (isFirst)
        {
            updated = SetSelectedConcept(updated, listing.Id, concept.Id);
        }

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return ConceptVersionsResult.Failure(saveError, BuildState(snapshot, _activeListingId));
        }

        _activeStoreId = listing.StoreId;
        _activeListingId = listing.Id;
        return ConceptVersionsResult.Success(ToSummary(concept), BuildState(updated, listing.Id));
    }

    public async Task<ConceptVersionsResult> EditAsync(ConceptEditRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Concepts.SingleOrDefault(candidate => candidate.Id == request.ConceptId);
        if (existing is null)
        {
            return ConceptVersionsResult.Failure("Concept was not found.", BuildState(snapshot, _activeListingId));
        }

        var changed = existing with
        {
            Name = string.IsNullOrWhiteSpace(request.Name) ? existing.Name : request.Name.Trim(),
            Idea = request.Idea ?? existing.Idea,
            Phrase = request.Phrase ?? existing.Phrase,
            GraphicDirection = request.GraphicDirection ?? existing.GraphicDirection,
            AudienceReaction = request.AudienceReaction ?? existing.AudienceReaction,
            Risks = request.Risks ?? existing.Risks,
            QualityNotes = request.QualityNotes ?? existing.QualityNotes,
            ScoreJson = request.ScoreJson ?? existing.ScoreJson,
            UpdatedAt = _clock()
        };
        var updated = snapshot with { Concepts = snapshot.Concepts.Select(c => c.Id == existing.Id ? changed : c).ToArray() };

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return ConceptVersionsResult.Failure(saveError, BuildState(snapshot, _activeListingId));
        }

        _activeStoreId = existing.StoreId;
        _activeListingId = existing.ListingId;
        return ConceptVersionsResult.Success(ToSummary(changed), BuildState(updated, existing.ListingId));
    }

    public async Task<ConceptVersionsResult> SupersedeAsync(ConceptCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var listing = snapshot.Listings.SingleOrDefault(candidate => candidate.Id == request.ListingId);
        if (listing is null)
        {
            return ConceptVersionsResult.Failure("Listing was not found.", BuildState(snapshot, _activeListingId));
        }

        var currentSelected = GetSelectedConceptId(snapshot, listing.Id);
        var now = _clock();
        var name = string.IsNullOrWhiteSpace(request.Name) ? (request.Idea ?? listing.Name) : request.Name.Trim();
        var newConcept = new Concept(
            _newId(),
            listing.StoreId,
            listing.Id,
            name,
            null,
            request.Idea,
            request.Phrase,
            request.GraphicDirection,
            request.AudienceReaction,
            request.Risks,
            request.QualityNotes,
            null,
            ConceptLifecycle.Active,
            false,
            now,
            now,
            "{}");

        var concepts = snapshot.Concepts.ToList();
        if (currentSelected is Guid selectedId)
        {
            var idx = concepts.FindIndex(c => c.Id == selectedId);
            if (idx >= 0)
            {
                concepts[idx] = concepts[idx] with { Lifecycle = ConceptLifecycle.Superseded, UpdatedAt = now };
            }
        }
        concepts.Add(newConcept);
        var updated = snapshot with { Concepts = concepts.ToArray() };
        updated = SetSelectedConcept(updated, listing.Id, newConcept.Id);

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return ConceptVersionsResult.Failure(saveError, BuildState(snapshot, _activeListingId));
        }

        _activeStoreId = listing.StoreId;
        _activeListingId = listing.Id;
        return ConceptVersionsResult.Success(ToSummary(newConcept), BuildState(updated, listing.Id));
    }

    public async Task<ConceptVersionsResult> RejectAsync(Guid conceptId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Concepts.SingleOrDefault(candidate => candidate.Id == conceptId);
        if (existing is null)
        {
            return ConceptVersionsResult.Failure("Concept was not found.", BuildState(snapshot, _activeListingId));
        }

        var changed = existing with { Lifecycle = ConceptLifecycle.Rejected, UpdatedAt = _clock() };
        var concepts = snapshot.Concepts.Select(c => c.Id == existing.Id ? changed : c).ToList();

        var currentSelected = GetSelectedConceptId(snapshot, existing.ListingId);
        var updated = snapshot with { Concepts = concepts.ToArray() };

        if (currentSelected == existing.Id)
        {
            updated = ClearSelectedConcept(updated, existing.ListingId);
        }

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return ConceptVersionsResult.Failure(saveError, BuildState(snapshot, _activeListingId));
        }

        _activeStoreId = existing.StoreId;
        _activeListingId = existing.ListingId;
        return ConceptVersionsResult.Success(ToSummary(changed), BuildState(updated, existing.ListingId));
    }

    public async Task<ConceptVersionsResult> SelectAsync(Guid conceptId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var concept = snapshot.Concepts.SingleOrDefault(candidate => candidate.Id == conceptId);
        if (concept is null)
        {
            return ConceptVersionsResult.Failure("Concept was not found.", BuildState(snapshot, _activeListingId));
        }

        if (concept.Lifecycle != ConceptLifecycle.Active)
        {
            return ConceptVersionsResult.Failure("Only active concepts can be selected as the current direction.", BuildState(snapshot, concept.ListingId));
        }

        var updated = SetSelectedConcept(snapshot, concept.ListingId, concept.Id);

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return ConceptVersionsResult.Failure(saveError, BuildState(snapshot, _activeListingId));
        }

        _activeStoreId = concept.StoreId;
        _activeListingId = concept.ListingId;
        return ConceptVersionsResult.Success(ToSummary(concept), BuildState(updated, concept.ListingId));
    }

    public async Task<ConceptVersionsResult> UpdateScoreAsync(ConceptScoreRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Concepts.SingleOrDefault(candidate => candidate.Id == request.ConceptId);
        if (existing is null)
        {
            return ConceptVersionsResult.Failure("Concept was not found.", BuildState(snapshot, _activeListingId));
        }

        var changed = existing with { ScoreJson = request.ScoreJson, UpdatedAt = _clock() };
        var updated = snapshot with { Concepts = snapshot.Concepts.Select(c => c.Id == existing.Id ? changed : c).ToArray() };

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return ConceptVersionsResult.Failure(saveError, BuildState(snapshot, _activeListingId));
        }

        _activeStoreId = existing.StoreId;
        _activeListingId = existing.ListingId;
        return ConceptVersionsResult.Success(ToSummary(changed), BuildState(updated, existing.ListingId));
    }

    private const string SelectedConceptKey = "listing.selectedConceptId";

    private static Guid? GetSelectedConceptId(WorkspaceSnapshot snapshot, Guid listingId)
    {
        var listing = snapshot.Listings.SingleOrDefault(l => l.Id == listingId);
        if (listing is null) return null;
        var metadata = ListingMetadataCodec.ParseMetadata(listing.MetadataJson);
        return metadata.TryGetValue(SelectedConceptKey, out var value) && Guid.TryParse(value, out var id) ? id : null;
    }

    private static WorkspaceSnapshot SetSelectedConcept(WorkspaceSnapshot snapshot, Guid listingId, Guid conceptId)
    {
        var listing = snapshot.Listings.SingleOrDefault(l => l.Id == listingId);
        if (listing is null) return snapshot;
        var metadata = ListingMetadataCodec.ParseMetadata(listing.MetadataJson);
        metadata[SelectedConceptKey] = conceptId.ToString();
        var changed = listing with { MetadataJson = ListingMetadataCodec.SerializeMetadata(metadata) };
        return snapshot with { Listings = snapshot.Listings.Select(l => l.Id == listingId ? changed : l).ToArray() };
    }

    private static WorkspaceSnapshot ClearSelectedConcept(WorkspaceSnapshot snapshot, Guid listingId)
    {
        var listing = snapshot.Listings.SingleOrDefault(l => l.Id == listingId);
        if (listing is null) return snapshot;
        var metadata = ListingMetadataCodec.ParseMetadata(listing.MetadataJson);
        metadata.Remove(SelectedConceptKey);
        var changed = listing with { MetadataJson = ListingMetadataCodec.SerializeMetadata(metadata) };
        return snapshot with { Listings = snapshot.Listings.Select(l => l.Id == listingId ? changed : l).ToArray() };
    }

    private static ConceptSummary ToSummary(Concept concept) =>
        new(concept.Id, concept.StoreId, concept.ListingId, concept.Name, concept.Idea, concept.Phrase,
            concept.GraphicDirection, concept.AudienceReaction, concept.Risks, concept.QualityNotes,
            concept.ScoreJson, concept.Lifecycle, concept.IsArchived, concept.CreatedAt, concept.UpdatedAt);

    private ConceptVersionsState BuildState(WorkspaceSnapshot snapshot, Guid? listingId)
    {
        if (listingId is Guid id)
        {
            _activeListingId = snapshot.Listings.Any(l => l.Id == id) ? id : null;
            if (_activeListingId is Guid activeId)
            {
                _activeStoreId = snapshot.Listings.Single(l => l.Id == activeId).StoreId;
            }
        }

        var listingConcepts = _activeListingId is Guid lid
            ? snapshot.Concepts.Where(c => c.ListingId == lid).ToArray()
            : [];

        var active = listingConcepts.Where(c => c.Lifecycle == ConceptLifecycle.Active).OrderBy(c => c.CreatedAt).Select(ToSummary).ToArray();
        var superseded = listingConcepts.Where(c => c.Lifecycle == ConceptLifecycle.Superseded).OrderByDescending(c => c.UpdatedAt).Select(ToSummary).ToArray();
        var rejected = listingConcepts.Where(c => c.Lifecycle == ConceptLifecycle.Rejected).OrderByDescending(c => c.UpdatedAt).Select(ToSummary).ToArray();

        var selectedId = _activeListingId is Guid lid2 ? GetSelectedConceptId(snapshot, lid2) : null;
        var selected = selectedId is Guid sid ? active.SingleOrDefault(s => s.Id == sid) : null;

        return new ConceptVersionsState(_activeStoreId, _activeListingId, selected, active, superseded, rejected);
    }

    private async Task<string?> TrySaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken)
    {
        try
        {
            await _repository.SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
            return null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return $"The concept change could not be saved. {exception.Message}";
        }
    }
}
