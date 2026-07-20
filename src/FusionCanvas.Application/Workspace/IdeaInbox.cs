using System.Text.Json;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspace;

public sealed record IdeaInboxSummary(
    Guid Id,
    Guid StoreId,
    string Name,
    string? Audience,
    string? PhraseFragments,
    string? VisualDirection,
    bool IsRejected,
    string? RejectedReason,
    string DisplayPath,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record IdeaInboxState(
    Guid? ActiveStoreId,
    Guid? ActiveListingId,
    IdeaInboxSummary? ActiveIdea,
    IReadOnlyList<IdeaInboxSummary> Ideas,
    bool NeedsCaptureGuidance)
{
    public static IdeaInboxState Empty { get; } = new(null, null, null, [], true);
}

public sealed record IdeaInboxResult(
    bool Succeeded,
    string? Error,
    IdeaInboxSummary? Idea,
    IdeaInboxState State)
{
    public static IdeaInboxResult Success(IdeaInboxSummary? idea, IdeaInboxState state) =>
        new(true, null, idea, state);

    public static IdeaInboxResult Failure(string error, IdeaInboxState state) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Idea inbox operation failed." : error, null, state);
}

public sealed record IdeaInboxContext(
    string? Audience = null,
    string? PhraseFragments = null,
    string? VisualDirection = null);

public sealed record IdeaInboxCaptureRequest(ListingTopicReference Topic, string Name, IdeaInboxContext? Context = null);
public sealed record IdeaInboxEditContextRequest(Guid ListingId, IdeaInboxContext Context);
public sealed record IdeaInboxRejectRequest(Guid ListingId, string? Reason = null);

public interface IIdeaInboxService
{
    Guid? ActiveStoreId { get; }
    Guid? ActiveListingId { get; }

    void SetActiveWorkspace(Guid? workspaceId);
    Task<IdeaInboxState> LoadAsync(Guid? storeId, CancellationToken cancellationToken = default);
    Task<IdeaInboxResult> CaptureAsync(IdeaInboxCaptureRequest request, CancellationToken cancellationToken = default);
    Task<IdeaInboxResult> EditContextAsync(IdeaInboxEditContextRequest request, CancellationToken cancellationToken = default);
    Task<IdeaInboxResult> PromoteAsync(Guid listingId, WorkflowStage targetStage, CancellationToken cancellationToken = default);
    Task<IdeaInboxResult> ArchiveAsync(Guid listingId, CancellationToken cancellationToken = default);
    Task<IdeaInboxResult> RejectAsync(IdeaInboxRejectRequest request, CancellationToken cancellationToken = default);
    Task<IdeaInboxResult> RestoreAsync(Guid listingId, CancellationToken cancellationToken = default);
    Task<IdeaInboxResult> SelectAsync(Guid listingId, CancellationToken cancellationToken = default);
}

public sealed class IdeaInboxService : IIdeaInboxService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IListingManagementService _listingManagement;
    private readonly Func<DateTimeOffset> _clock;
    private Guid? _activeWorkspaceId;
    private Guid? _activeStoreId;
    private Guid? _activeListingId;

    public IdeaInboxService(
        IWorkspaceRepository repository,
        IListingManagementService listingManagement,
        Func<DateTimeOffset>? clock = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _listingManagement = listingManagement ?? throw new ArgumentNullException(nameof(listingManagement));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public Guid? ActiveStoreId => _activeStoreId;
    public Guid? ActiveListingId => _activeListingId;

    public void SetActiveWorkspace(Guid? workspaceId)
    {
        _activeWorkspaceId = workspaceId;
        _listingManagement.SetActiveWorkspace(workspaceId);
    }

    public async Task<IdeaInboxState> LoadAsync(Guid? storeId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return BuildState(snapshot, storeId);
    }

    public async Task<IdeaInboxResult> CaptureAsync(IdeaInboxCaptureRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = request.Context is null
            ? null
            : new ListingContext(Metadata: BuildIdeaMetadata(request.Context));
        var createRequest = new ListingManagementCreateRequest(request.Topic, request.Name, context);
        var result = await _listingManagement.CreateListingAsync(createRequest, cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return IdeaInboxResult.Failure(result.Error!, BuildStateFromListingState(result.State));
        }

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var idea = ToSummary(snapshot, result.Listing!.Id);
        return IdeaInboxResult.Success(idea, BuildState(snapshot, result.Listing.StoreId));
    }

    public async Task<IdeaInboxResult> EditContextAsync(IdeaInboxEditContextRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Listings.SingleOrDefault(candidate => candidate.Id == request.ListingId);
        if (existing is null)
        {
            return IdeaInboxResult.Failure("Idea was not found.", BuildState(snapshot, _activeStoreId));
        }

        if (existing.Stage != WorkflowStage.Idea)
        {
            return IdeaInboxResult.Failure("Only idea-stage listings can be edited from the inbox.", BuildState(snapshot, existing.StoreId));
        }

        var metadata = ListingMetadataCodec.ParseMetadata(existing.MetadataJson);
        ListingMetadataCodec.SetOptional(metadata, ListingMetadataCodec.IdeaAudienceKey, request.Context.Audience);
        ListingMetadataCodec.SetOptional(metadata, ListingMetadataCodec.IdeaPhraseFragmentsKey, request.Context.PhraseFragments);
        ListingMetadataCodec.SetOptional(metadata, ListingMetadataCodec.IdeaVisualDirectionKey, request.Context.VisualDirection);

        var changed = existing with
        {
            MetadataJson = ListingMetadataCodec.SerializeMetadata(metadata),
            UpdatedAt = _clock()
        };
        var updated = snapshot with { Listings = snapshot.Listings.Select(candidate => candidate.Id == existing.Id ? changed : candidate).ToArray() };

        try
        {
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return IdeaInboxResult.Failure($"The idea edit could not be saved. {exception.Message}", BuildState(snapshot, existing.StoreId));
        }

        _activeStoreId = existing.StoreId;
        _activeListingId = existing.Id;
        return IdeaInboxResult.Success(ToSummary(updated, existing.Id), BuildState(updated, existing.StoreId));
    }

    public async Task<IdeaInboxResult> PromoteAsync(Guid listingId, WorkflowStage targetStage, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(targetStage) || targetStage == WorkflowStage.Idea)
        {
            return IdeaInboxResult.Failure("Promotion requires a target stage beyond Idea.", await LoadAsync(_activeStoreId, cancellationToken));
        }

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Listings.SingleOrDefault(candidate => candidate.Id == listingId);
        if (existing is null)
        {
            return IdeaInboxResult.Failure("Idea was not found.", BuildState(snapshot, _activeStoreId));
        }

        if (existing.Stage != WorkflowStage.Idea)
        {
            return IdeaInboxResult.Failure("Only idea-stage listings can be promoted from the inbox.", BuildState(snapshot, existing.StoreId));
        }

        var result = await _listingManagement.MoveListingStageAsync(new ListingManagementMoveStageRequest(listingId, targetStage), cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return IdeaInboxResult.Failure(result.Error!, BuildStateFromListingState(result.State));
        }

        var updated = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return IdeaInboxResult.Success(null, BuildState(updated, existing.StoreId));
    }

    public async Task<IdeaInboxResult> ArchiveAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        var result = await _listingManagement.ArchiveListingAsync(listingId, cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return IdeaInboxResult.Failure(result.Error!, BuildStateFromListingState(result.State));
        }

        var updated = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return IdeaInboxResult.Success(null, BuildState(updated, result.Listing?.StoreId ?? _activeStoreId));
    }

    public async Task<IdeaInboxResult> RejectAsync(IdeaInboxRejectRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Listings.SingleOrDefault(candidate => candidate.Id == request.ListingId);
        if (existing is null)
        {
            return IdeaInboxResult.Failure("Idea was not found.", BuildState(snapshot, _activeStoreId));
        }

        if (existing.Stage != WorkflowStage.Idea)
        {
            return IdeaInboxResult.Failure("Only idea-stage listings can be rejected from the inbox.", BuildState(snapshot, existing.StoreId));
        }

        var metadata = ListingMetadataCodec.ParseMetadata(existing.MetadataJson);
        metadata[ListingMetadataCodec.IdeaRejectedKey] = "true";
        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            metadata[ListingMetadataCodec.IdeaRejectedReasonKey] = request.Reason.Trim();
        }
        else
        {
            metadata.Remove(ListingMetadataCodec.IdeaRejectedReasonKey);
        }

        var changed = existing with
        {
            MetadataJson = ListingMetadataCodec.SerializeMetadata(metadata),
            UpdatedAt = _clock()
        };
        var preArchive = snapshot with { Listings = snapshot.Listings.Select(candidate => candidate.Id == existing.Id ? changed : candidate).ToArray() };

        try
        {
            await _repository.SaveAsync(preArchive, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return IdeaInboxResult.Failure($"The reject marker could not be saved. {exception.Message}", BuildState(snapshot, existing.StoreId));
        }

        var archiveResult = await _listingManagement.ArchiveListingAsync(existing.Id, cancellationToken).ConfigureAwait(false);
        if (!archiveResult.Succeeded)
        {
            return IdeaInboxResult.Failure(archiveResult.Error!, BuildState(preArchive, existing.StoreId));
        }

        var updated = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return IdeaInboxResult.Success(null, BuildState(updated, existing.StoreId));
    }

    public async Task<IdeaInboxResult> RestoreAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Listings.SingleOrDefault(candidate => candidate.Id == listingId);
        if (existing is null)
        {
            return IdeaInboxResult.Failure("Idea was not found.", BuildState(snapshot, _activeStoreId));
        }

        var metadata = ListingMetadataCodec.ParseMetadata(existing.MetadataJson);
        var hadRejectedMarker = metadata.Remove(ListingMetadataCodec.IdeaRejectedKey);
        metadata.Remove(ListingMetadataCodec.IdeaRejectedReasonKey);

        if (hadRejectedMarker)
        {
            var changed = existing with
            {
                MetadataJson = ListingMetadataCodec.SerializeMetadata(metadata),
                UpdatedAt = _clock()
            };
            var cleared = snapshot with { Listings = snapshot.Listings.Select(candidate => candidate.Id == existing.Id ? changed : candidate).ToArray() };
            try
            {
                await _repository.SaveAsync(cleared, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return IdeaInboxResult.Failure($"The reject marker could not be cleared. {exception.Message}", BuildState(snapshot, existing.StoreId));
            }
        }

        var result = await _listingManagement.RestoreListingAsync(new ListingManagementRestoreRequest(listingId), cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return IdeaInboxResult.Failure(result.Error!, BuildState(snapshot, existing.StoreId));
        }

        var updated = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return IdeaInboxResult.Success(ToSummary(updated, listingId), BuildState(updated, existing.StoreId));
    }

    public async Task<IdeaInboxResult> SelectAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var listing = snapshot.Listings.SingleOrDefault(candidate => candidate.Id == listingId);
        if (listing is null)
        {
            return IdeaInboxResult.Failure("Idea was not found.", BuildState(snapshot, _activeStoreId));
        }

        if (listing.Stage != WorkflowStage.Idea || !ListingHierarchy.IsEffectivelyActive(snapshot, listing))
        {
            return IdeaInboxResult.Failure("Only active idea-stage listings can be selected in the inbox.", BuildState(snapshot, listing.StoreId));
        }

        _activeStoreId = listing.StoreId;
        _activeListingId = listing.Id;
        return IdeaInboxResult.Success(ToSummary(snapshot, listingId), BuildState(snapshot, listing.StoreId));
    }

    private static Dictionary<string, string> BuildIdeaMetadata(IdeaInboxContext context)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
        ListingMetadataCodec.SetOptional(metadata, ListingMetadataCodec.IdeaAudienceKey, context.Audience);
        ListingMetadataCodec.SetOptional(metadata, ListingMetadataCodec.IdeaPhraseFragmentsKey, context.PhraseFragments);
        ListingMetadataCodec.SetOptional(metadata, ListingMetadataCodec.IdeaVisualDirectionKey, context.VisualDirection);
        return metadata;
    }

    private static bool IsIdeaStageRejected(Listing listing)
    {
        var metadata = ListingMetadataCodec.ParseMetadata(listing.MetadataJson);
        return metadata.TryGetValue(ListingMetadataCodec.IdeaRejectedKey, out var value) && value == "true";
    }

    private static IdeaInboxSummary ToSummary(WorkspaceSnapshot snapshot, Guid listingId)
    {
        var listing = snapshot.Listings.Single(candidate => candidate.Id == listingId);
        var metadata = ListingMetadataCodec.ParseMetadata(listing.MetadataJson);
        metadata.TryGetValue(ListingMetadataCodec.IdeaAudienceKey, out var audience);
        metadata.TryGetValue(ListingMetadataCodec.IdeaPhraseFragmentsKey, out var phraseFragments);
        metadata.TryGetValue(ListingMetadataCodec.IdeaVisualDirectionKey, out var visualDirection);
        metadata.TryGetValue(ListingMetadataCodec.IdeaRejectedReasonKey, out var rejectedReason);
        var niche = ListingHierarchy.GetEffectiveNiche(snapshot, listing);
        var groupPath = listing.GroupId is Guid groupId
            ? GroupHierarchy.GetAncestors(snapshot, snapshot.Groups.Single(group => group.Id == groupId))
                .Select(group => group.Name)
                .Append(snapshot.Groups.Single(group => group.Id == groupId).Name)
            : [];
        var displayPath = string.Join(" / ", new[] { niche.Name }.Concat(groupPath).Append(listing.Name));
        return new IdeaInboxSummary(
            listing.Id,
            listing.StoreId,
            listing.Name,
            audience,
            phraseFragments,
            visualDirection,
            IsIdeaStageRejected(listing),
            rejectedReason,
            displayPath,
            listing.CreatedAt,
            listing.UpdatedAt);
    }

    private IdeaInboxState BuildState(WorkspaceSnapshot snapshot, Guid? storeId)
    {
        if (storeId is Guid requestedStoreId)
        {
            _activeStoreId = snapshot.Stores.Any(store => store.Id == requestedStoreId && !store.IsArchived && StoreBelongsToActiveWorkspace(store))
                ? requestedStoreId
                : null;
        }
        else if (_activeStoreId is Guid activeStoreId &&
                 !snapshot.Stores.Any(store => store.Id == activeStoreId && !store.IsArchived && StoreBelongsToActiveWorkspace(store)))
        {
            _activeStoreId = null;
        }

        if (_activeListingId is Guid activeListingId &&
            snapshot.Listings.SingleOrDefault(candidate => candidate.Id == activeListingId) is not { } activeListing ||
            _activeListingId is Guid selectedId && snapshot.Listings.SingleOrDefault(candidate => candidate.Id == selectedId) is { } selected &&
            (_activeStoreId != selected.StoreId || !ListingHierarchy.IsEffectivelyActive(snapshot, selected)))
        {
            _activeListingId = null;
        }

        var ideas = _activeStoreId is Guid id
            ? snapshot.Listings
                .Where(listing => listing.StoreId == id && listing.Stage == WorkflowStage.Idea && ListingHierarchy.IsEffectivelyActive(snapshot, listing) && !IsIdeaStageRejected(listing))
                .OrderBy(listing => listing.Name, StringComparer.OrdinalIgnoreCase)
                .Select(listing => ToSummary(snapshot, listing.Id))
                .ToArray()
            : [];
        var activeIdea = _activeListingId is Guid listingId ? ideas.SingleOrDefault(idea => idea.Id == listingId) : null;
        return new IdeaInboxState(
            _activeStoreId,
            _activeListingId,
            activeIdea,
            ideas,
            _activeStoreId is not null && ideas.Length == 0);
    }

    private static IdeaInboxState BuildStateFromListingState(ListingManagementState state) =>
        new(state.ActiveStoreId, state.ActiveListingId, null, [], false);

    private bool StoreBelongsToActiveWorkspace(Store store) => _activeWorkspaceId is null || store.WorkspaceId == _activeWorkspaceId;
}
