using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspace;

public sealed record DesignSummary(
    Guid Id,
    Guid StoreId,
    Guid ListingId,
    Guid? ImplementedConceptId,
    string Name,
    string? Description,
    string? SourceMethod,
    string? Notes,
    DesignApprovalState ApprovalState,
    bool IsFinalSelected,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record DesignRecordsState(
    Guid? ActiveStoreId,
    Guid? ActiveListingId,
    IReadOnlyList<DesignSummary> ActiveDesigns,
    IReadOnlyList<DesignSummary> RejectedDesigns,
    IReadOnlyList<Guid> FinalSelectedIds)
{
    public static DesignRecordsState Empty { get; } = new(null, null, [], [], []);
}

public sealed record DesignRecordsResult(
    bool Succeeded,
    string? Error,
    DesignSummary? Design,
    DesignRecordsState State)
{
    public static DesignRecordsResult Success(DesignSummary? design, DesignRecordsState state) =>
        new(true, null, design, state);

    public static DesignRecordsResult Failure(string error, DesignRecordsState state) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Design operation failed." : error, null, state);
}

public sealed record DesignCreateRequest(
    Guid ListingId,
    string Name,
    Guid? ImplementedConceptId = null,
    string? SourceMethod = null,
    string? Notes = null);

public sealed record DesignEditRequest(
    Guid DesignId,
    string? Name = null,
    string? Notes = null,
    DesignApprovalState? ApprovalState = null);

public interface IDesignRecordsService
{
    Guid? ActiveStoreId { get; }
    Guid? ActiveListingId { get; }

    void SetActiveWorkspace(Guid? workspaceId);
    Task<DesignRecordsState> LoadAsync(Guid? listingId, CancellationToken cancellationToken = default);
    Task<DesignRecordsResult> CreateAsync(DesignCreateRequest request, CancellationToken cancellationToken = default);
    Task<DesignRecordsResult> EditAsync(DesignEditRequest request, CancellationToken cancellationToken = default);
    Task<DesignRecordsResult> PromoteFinalAsync(Guid designId, CancellationToken cancellationToken = default);
    Task<DesignRecordsResult> DemoteFinalAsync(Guid designId, CancellationToken cancellationToken = default);
    Task<DesignRecordsResult> RejectAsync(Guid designId, CancellationToken cancellationToken = default);
}

public sealed class DesignRecordsService : IDesignRecordsService
{
    private readonly IWorkspaceRepository _repository;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;
    private Guid? _activeWorkspaceId;
    private Guid? _activeStoreId;
    private Guid? _activeListingId;

    public DesignRecordsService(
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

    public void SetActiveWorkspace(Guid? workspaceId) => _activeWorkspaceId = workspaceId;

    public async Task<DesignRecordsState> LoadAsync(Guid? listingId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return BuildState(snapshot, listingId);
    }

    public async Task<DesignRecordsResult> CreateAsync(DesignCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var listing = snapshot.Listings.SingleOrDefault(candidate => candidate.Id == request.ListingId);
        if (listing is null)
        {
            return DesignRecordsResult.Failure("Listing was not found.", BuildState(snapshot, _activeListingId));
        }

        if (request.ImplementedConceptId is Guid conceptId)
        {
            var concept = snapshot.Concepts.SingleOrDefault(c => c.Id == conceptId);
            if (concept is null || concept.ListingId != listing.Id)
            {
                return DesignRecordsResult.Failure("The implemented concept must belong to the same listing.", BuildState(snapshot, _activeListingId));
            }
        }

        var now = _clock();
        var design = new Design(
            _newId(),
            listing.StoreId,
            listing.Id,
            request.ImplementedConceptId,
            request.Name.Trim(),
            null,
            request.SourceMethod,
            request.Notes,
            DesignApprovalState.Draft,
            false,
            now,
            now,
            "{}");

        var updated = snapshot with { Designs = [.. snapshot.Designs, design] };

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return DesignRecordsResult.Failure(saveError, BuildState(snapshot, _activeListingId));
        }

        _activeStoreId = listing.StoreId;
        _activeListingId = listing.Id;
        return DesignRecordsResult.Success(ToSummary(updated, design), BuildState(updated, listing.Id));
    }

    public async Task<DesignRecordsResult> EditAsync(DesignEditRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Designs.SingleOrDefault(candidate => candidate.Id == request.DesignId);
        if (existing is null)
        {
            return DesignRecordsResult.Failure("Design was not found.", BuildState(snapshot, _activeListingId));
        }

        var changed = existing with
        {
            Name = string.IsNullOrWhiteSpace(request.Name) ? existing.Name : request.Name.Trim(),
            Notes = request.Notes ?? existing.Notes,
            ApprovalState = request.ApprovalState ?? existing.ApprovalState,
            UpdatedAt = _clock()
        };
        var updated = snapshot with { Designs = snapshot.Designs.Select(d => d.Id == existing.Id ? changed : d).ToArray() };

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return DesignRecordsResult.Failure(saveError, BuildState(snapshot, _activeListingId));
        }

        _activeStoreId = existing.StoreId;
        _activeListingId = existing.ListingId;
        return DesignRecordsResult.Success(ToSummary(updated, changed), BuildState(updated, existing.ListingId));
    }

    public async Task<DesignRecordsResult> PromoteFinalAsync(Guid designId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var design = snapshot.Designs.SingleOrDefault(candidate => candidate.Id == designId);
        if (design is null)
        {
            return DesignRecordsResult.Failure("Design was not found.", BuildState(snapshot, _activeListingId));
        }

        var finalIds = GetFinalSelectedIds(snapshot, design.ListingId);
        if (!finalIds.Contains(designId))
        {
            finalIds = [.. finalIds, designId];
        }
        var updated = SetFinalSelectedIds(snapshot, design.ListingId, finalIds);

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return DesignRecordsResult.Failure(saveError, BuildState(snapshot, _activeListingId));
        }

        _activeStoreId = design.StoreId;
        _activeListingId = design.ListingId;
        return DesignRecordsResult.Success(ToSummary(updated, design), BuildState(updated, design.ListingId));
    }

    public async Task<DesignRecordsResult> DemoteFinalAsync(Guid designId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var design = snapshot.Designs.SingleOrDefault(candidate => candidate.Id == designId);
        if (design is null)
        {
            return DesignRecordsResult.Failure("Design was not found.", BuildState(snapshot, _activeListingId));
        }

        var finalIds = GetFinalSelectedIds(snapshot, design.ListingId).Where(id => id != designId).ToArray();
        var updated = SetFinalSelectedIds(snapshot, design.ListingId, finalIds);

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return DesignRecordsResult.Failure(saveError, BuildState(snapshot, _activeListingId));
        }

        _activeStoreId = design.StoreId;
        _activeListingId = design.ListingId;
        return DesignRecordsResult.Success(ToSummary(updated, design), BuildState(updated, design.ListingId));
    }

    public async Task<DesignRecordsResult> RejectAsync(Guid designId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Designs.SingleOrDefault(candidate => candidate.Id == designId);
        if (existing is null)
        {
            return DesignRecordsResult.Failure("Design was not found.", BuildState(snapshot, _activeListingId));
        }

        var changed = existing with { ApprovalState = DesignApprovalState.Rejected, UpdatedAt = _clock() };
        var updated = snapshot with { Designs = snapshot.Designs.Select(d => d.Id == existing.Id ? changed : d).ToArray() };

        var finalIds = GetFinalSelectedIds(snapshot, existing.ListingId).Where(id => id != designId).ToArray();
        updated = SetFinalSelectedIds(updated, existing.ListingId, finalIds);

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return DesignRecordsResult.Failure(saveError, BuildState(snapshot, _activeListingId));
        }

        _activeStoreId = existing.StoreId;
        _activeListingId = existing.ListingId;
        return DesignRecordsResult.Success(ToSummary(updated, changed), BuildState(updated, existing.ListingId));
    }

    private const string FinalSelectedKey = "listing.finalDesignIds";

    private static IReadOnlyList<Guid> GetFinalSelectedIds(WorkspaceSnapshot snapshot, Guid listingId)
    {
        var listing = snapshot.Listings.SingleOrDefault(l => l.Id == listingId);
        if (listing is null) return [];
        var metadata = ListingMetadataCodec.ParseMetadata(listing.MetadataJson);
        if (!metadata.TryGetValue(FinalSelectedKey, out var value)) return [];
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToArray();
    }

    private static WorkspaceSnapshot SetFinalSelectedIds(WorkspaceSnapshot snapshot, Guid listingId, IReadOnlyList<Guid> ids)
    {
        var listing = snapshot.Listings.SingleOrDefault(l => l.Id == listingId);
        if (listing is null) return snapshot;
        var metadata = ListingMetadataCodec.ParseMetadata(listing.MetadataJson);
        if (ids.Count == 0)
        {
            metadata.Remove(FinalSelectedKey);
        }
        else
        {
            metadata[FinalSelectedKey] = string.Join(",", ids);
        }
        var changed = listing with { MetadataJson = ListingMetadataCodec.SerializeMetadata(metadata) };
        return snapshot with { Listings = snapshot.Listings.Select(l => l.Id == listingId ? changed : l).ToArray() };
    }

    private static DesignSummary ToSummary(WorkspaceSnapshot snapshot, Design design)
    {
        var finalIds = GetFinalSelectedIds(snapshot, design.ListingId);
        return new DesignSummary(
            design.Id, design.StoreId, design.ListingId, design.ImplementedConceptId,
            design.Name, design.Description, design.SourceMethod, design.Notes,
            design.ApprovalState, finalIds.Contains(design.Id), design.IsArchived,
            design.CreatedAt, design.UpdatedAt);
    }

    private DesignRecordsState BuildState(WorkspaceSnapshot snapshot, Guid? listingId)
    {
        if (listingId is Guid id && snapshot.Listings.Any(l => l.Id == id))
        {
            _activeListingId = id;
            _activeStoreId = snapshot.Listings.Single(l => l.Id == id).StoreId;
        }

        var listingDesigns = _activeListingId is Guid lid
            ? snapshot.Designs.Where(d => d.ListingId == lid).ToArray()
            : [];

        var active = listingDesigns.Where(d => d.ApprovalState != DesignApprovalState.Rejected).OrderBy(d => d.CreatedAt).Select(d => ToSummary(snapshot, d)).ToArray();
        var rejected = listingDesigns.Where(d => d.ApprovalState == DesignApprovalState.Rejected).OrderByDescending(d => d.UpdatedAt).Select(d => ToSummary(snapshot, d)).ToArray();
        var finalIds = GetFinalSelectedIds(snapshot, _activeListingId ?? Guid.Empty);

        return new DesignRecordsState(_activeStoreId, _activeListingId, active, rejected, finalIds);
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
            return $"The design change could not be saved. {exception.Message}";
        }
    }
}
