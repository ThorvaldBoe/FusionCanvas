using System.Text.Json;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Prompts;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;

namespace FusionCanvas.Application.Workspace;

public sealed record NicheContext(
    string? Description = null,
    string? Audience = null,
    string? HumorStyle = null,
    string? VisualStyleGuidance = null,
    string? Constraints = null,
    string? Risks = null,
    string? ResearchNotes = null,
    string? Notes = null);

public sealed record NicheManagementCreateRequest(Guid StoreId, string Name, NicheContext? Context = null);

public sealed record NicheManagementUpdateRequest(Guid NicheId, string Name, NicheContext? Context = null);

public sealed record NicheManagementDeleteRequest(Guid NicheId, bool ConfirmPermanentDeletion);

public sealed record NicheSummary(
    Guid Id,
    Guid StoreId,
    string Name,
    NicheContext Context,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record NicheManagementState(
    Guid? ActiveStoreId,
    IReadOnlyList<NicheSummary> ActiveNiches,
    IReadOnlyList<NicheSummary> ArchivedNiches,
    Guid? ActiveNicheId,
    NicheSummary? ActiveNiche,
    bool NeedsFirstNiche);

public sealed record NicheManagementResult(
    bool Succeeded,
    string? Error,
    NicheSummary? Niche,
    NicheManagementState State)
{
    public static NicheManagementResult Success(NicheSummary? niche, NicheManagementState state) =>
        new(true, null, niche, state);

    public static NicheManagementResult Failure(string error, NicheManagementState state) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Niche operation failed." : error, null, state);
}

public interface INicheManagementService
{
    Guid? ActiveWorkspaceId { get; }

    Guid? ActiveStoreId { get; }

    Guid? ActiveNicheId { get; }

    void SetActiveWorkspace(Guid? workspaceId);

    Task<NicheManagementState> LoadAsync(Guid? storeId, CancellationToken cancellationToken = default);

    Task<NicheManagementResult> CreateNicheAsync(NicheManagementCreateRequest request, CancellationToken cancellationToken = default);

    Task<NicheManagementResult> UpdateNicheAsync(NicheManagementUpdateRequest request, CancellationToken cancellationToken = default);

    Task<NicheManagementResult> ArchiveNicheAsync(Guid nicheId, CancellationToken cancellationToken = default);

    Task<NicheManagementResult> RestoreNicheAsync(Guid nicheId, CancellationToken cancellationToken = default);

    Task<NicheManagementResult> DeleteNicheAsync(NicheManagementDeleteRequest request, CancellationToken cancellationToken = default);

    Task<NicheManagementResult> SelectNicheAsync(Guid nicheId, CancellationToken cancellationToken = default);
}

public sealed class NicheManagementService : INicheManagementService
{
    private const string AudienceKey = "audience";
    private const string HumorStyleKey = "humorStyle";
    private const string VisualStyleGuidanceKey = "visualStyleGuidance";
    private const string ConstraintsKey = "constraints";
    private const string RisksKey = "risks";
    private const string ResearchNotesKey = "researchNotes";
    private const string NotesKey = "notes";

    private readonly IWorkspaceRepository _repository;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;
    private Guid? _activeWorkspaceId;
    private Guid? _activeStoreId;
    private Guid? _activeNicheId;

    public NicheManagementService(
        IWorkspaceRepository repository,
        Func<DateTimeOffset>? clock = null,
        Func<Guid>? newId = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newId = newId ?? Guid.NewGuid;
    }

    public Guid? ActiveWorkspaceId => _activeWorkspaceId;

    public Guid? ActiveStoreId => _activeStoreId;

    public Guid? ActiveNicheId => _activeNicheId;

    public void SetActiveWorkspace(Guid? workspaceId)
    {
        if (_activeWorkspaceId != workspaceId)
        {
            _activeStoreId = null;
            _activeNicheId = null;
        }

        _activeWorkspaceId = workspaceId;
    }

    public async Task<NicheManagementState> LoadAsync(Guid? storeId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return BuildState(snapshot, storeId);
    }

    public async Task<NicheManagementResult> CreateNicheAsync(
        NicheManagementCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var storeValidation = ValidateActiveStore(snapshot, request.StoreId);
        if (storeValidation is not null)
        {
            return NicheManagementResult.Failure(storeValidation, BuildState(snapshot, request.StoreId));
        }

        var normalizedName = NormalizeName(request.Name);
        var validation = ValidateName(normalizedName, snapshot, request.StoreId, existingNicheId: null);
        if (validation is not null)
        {
            return NicheManagementResult.Failure(validation, BuildState(snapshot, request.StoreId));
        }

        var now = _clock();
        var context = request.Context ?? new NicheContext();
        var niche = new Niche(_newId(), request.StoreId, normalizedName, NormalizeOptional(context.Description), false, now, now, ToMetadataJson(context));
        var updated = snapshot with { Niches = [.. snapshot.Niches, niche] };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        _activeStoreId = request.StoreId;
        _activeNicheId = niche.Id;
        return NicheManagementResult.Success(ToSummary(niche), BuildState(updated, request.StoreId));
    }

    public async Task<NicheManagementResult> UpdateNicheAsync(
        NicheManagementUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Niches.SingleOrDefault(niche => niche.Id == request.NicheId);
        if (existing is null)
        {
            return NicheManagementResult.Failure("Niche was not found.", BuildState(snapshot, _activeStoreId));
        }

        var normalizedName = NormalizeName(request.Name);
        var validation = ValidateName(normalizedName, snapshot, existing.StoreId, existing.Id);
        if (validation is not null)
        {
            return NicheManagementResult.Failure(validation, BuildState(snapshot, existing.StoreId));
        }

        var context = request.Context ?? ToContext(existing);
        var updatedNiche = existing with
        {
            Name = normalizedName,
            Description = NormalizeOptional(context.Description),
            UpdatedAt = _clock(),
            MetadataJson = ToMetadataJson(context, existing.MetadataJson)
        };

        var updated = snapshot with
        {
            Niches = snapshot.Niches.Select(niche => niche.Id == updatedNiche.Id ? updatedNiche : niche).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        _activeStoreId = updatedNiche.StoreId;
        return NicheManagementResult.Success(ToSummary(updatedNiche), BuildState(updated, updatedNiche.StoreId));
    }

    public async Task<NicheManagementResult> ArchiveNicheAsync(Guid nicheId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Niches.SingleOrDefault(niche => niche.Id == nicheId);
        if (existing is null)
        {
            return NicheManagementResult.Failure("Niche was not found.", BuildState(snapshot, _activeStoreId));
        }

        var archived = existing with { IsArchived = true, UpdatedAt = _clock() };
        var updated = snapshot with
        {
            Niches = snapshot.Niches.Select(niche => niche.Id == archived.Id ? archived : niche).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        if (_activeNicheId == nicheId)
        {
            _activeNicheId = null;
        }

        _activeStoreId = archived.StoreId;
        return NicheManagementResult.Success(ToSummary(archived), BuildState(updated, archived.StoreId));
    }

    public async Task<NicheManagementResult> RestoreNicheAsync(Guid nicheId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Niches.SingleOrDefault(niche => niche.Id == nicheId);
        if (existing is null)
        {
            return NicheManagementResult.Failure("Niche was not found.", BuildState(snapshot, _activeStoreId));
        }

        var duplicate = snapshot.Niches.Any(niche =>
            niche.Id != existing.Id &&
            niche.StoreId == existing.StoreId &&
            !niche.IsArchived &&
            string.Equals(niche.Name, existing.Name, StringComparison.OrdinalIgnoreCase));
        if (duplicate)
        {
            return NicheManagementResult.Failure("An active niche already uses this name in this store.", BuildState(snapshot, existing.StoreId));
        }

        var restored = existing with { IsArchived = false, UpdatedAt = _clock() };
        var updated = snapshot with
        {
            Niches = snapshot.Niches.Select(niche => niche.Id == restored.Id ? restored : niche).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        _activeStoreId = restored.StoreId;
        return NicheManagementResult.Success(ToSummary(restored), BuildState(updated, restored.StoreId));
    }

    public async Task<NicheManagementResult> DeleteNicheAsync(
        NicheManagementDeleteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Niches.SingleOrDefault(niche => niche.Id == request.NicheId);
        if (existing is null)
        {
            return NicheManagementResult.Failure("Niche was not found.", BuildState(snapshot, _activeStoreId));
        }

        if (!request.ConfirmPermanentDeletion)
        {
            return NicheManagementResult.Failure("Permanent deletion requires confirmation.", BuildState(snapshot, existing.StoreId));
        }

        if (HasConnectedData(snapshot, existing.Id))
        {
            return NicheManagementResult.Failure("Niche has connected data. Empty the niche or archive it instead.", BuildState(snapshot, existing.StoreId));
        }

        var updated = snapshot with
        {
            Niches = snapshot.Niches.Where(niche => niche.Id != existing.Id).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        if (_activeNicheId == existing.Id)
        {
            _activeNicheId = null;
        }

        _activeStoreId = existing.StoreId;
        return NicheManagementResult.Success(ToSummary(existing), BuildState(updated, existing.StoreId));
    }

    public async Task<NicheManagementResult> SelectNicheAsync(Guid nicheId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Niches.SingleOrDefault(niche => niche.Id == nicheId);
        if (existing is null)
        {
            return NicheManagementResult.Failure("Niche was not found.", BuildState(snapshot, _activeStoreId));
        }

        if (existing.IsArchived)
        {
            return NicheManagementResult.Failure("Archived niches must be restored before they can become active.", BuildState(snapshot, existing.StoreId));
        }

        var storeValidation = ValidateActiveStore(snapshot, existing.StoreId);
        if (storeValidation is not null)
        {
            return NicheManagementResult.Failure(storeValidation, BuildState(snapshot, existing.StoreId));
        }

        _activeStoreId = existing.StoreId;
        _activeNicheId = existing.Id;
        return NicheManagementResult.Success(ToSummary(existing), BuildState(snapshot, existing.StoreId));
    }

    private NicheManagementState BuildState(WorkspaceSnapshot snapshot, Guid? storeId)
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

        if (_activeNicheId is Guid activeNicheId &&
            snapshot.Niches.SingleOrDefault(niche => niche.Id == activeNicheId) is not { IsArchived: false } activeNiche ||
            _activeNicheId is Guid activeId && _activeStoreId is Guid store && snapshot.Niches.SingleOrDefault(niche => niche.Id == activeId)?.StoreId != store)
        {
            _activeNicheId = null;
        }

        var activeNiches = _activeStoreId is Guid id
            ? snapshot.Niches
                .Where(niche => niche.StoreId == id && !niche.IsArchived)
                .OrderBy(niche => niche.Name, StringComparer.OrdinalIgnoreCase)
                .Select(ToSummary)
                .ToArray()
            : [];
        var archivedNiches = _activeStoreId is Guid archiveStoreId
            ? snapshot.Niches
                .Where(niche => niche.StoreId == archiveStoreId && niche.IsArchived)
                .OrderBy(niche => niche.Name, StringComparer.OrdinalIgnoreCase)
                .Select(ToSummary)
                .ToArray()
            : [];
        var active = _activeNicheId is Guid nicheId
            ? activeNiches.SingleOrDefault(niche => niche.Id == nicheId)
            : null;

        return new NicheManagementState(_activeStoreId, activeNiches, archivedNiches, _activeNicheId, active, _activeStoreId is not null && activeNiches.Count() == 0);
    }

    private string? ValidateActiveStore(WorkspaceSnapshot snapshot, Guid storeId)
    {
        var store = snapshot.Stores.SingleOrDefault(candidate => candidate.Id == storeId);
        if (store is null)
        {
            return "Active store is required before creating niches.";
        }

        if (store.IsArchived)
        {
            return "Archived stores must be restored before niches can be managed.";
        }

        return StoreBelongsToActiveWorkspace(store) ? null : "Store does not belong to the active workspace.";
    }

    private bool StoreBelongsToActiveWorkspace(Store store) =>
        _activeWorkspaceId is null || store.WorkspaceId == _activeWorkspaceId;

    private static string? ValidateName(string name, WorkspaceSnapshot snapshot, Guid storeId, Guid? existingNicheId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Niche name is required.";
        }

        var duplicate = snapshot.Niches.Any(niche =>
            niche.Id != existingNicheId &&
            niche.StoreId == storeId &&
            !niche.IsArchived &&
            string.Equals(niche.Name, name, StringComparison.OrdinalIgnoreCase));

        return duplicate ? "An active niche already uses this name in this store." : null;
    }

    private static bool HasConnectedData(WorkspaceSnapshot snapshot, Guid nicheId)
    {
        var itemIds = snapshot.Items.Where(listing => listing.NicheId == nicheId).Select(listing => listing.Id).ToHashSet();
        var assetIds = snapshot.AssetLinks
            .Where(link => IsNicheScopedAssetTarget(snapshot, link, nicheId))
            .Select(link => link.AssetId)
            .ToHashSet();

        return snapshot.Groups.Any(group => group.NicheId == nicheId)
            || snapshot.Items.Any(listing => listing.NicheId == nicheId)
            || snapshot.Prompts.Any(prompt => prompt.ItemId is Guid itemId && itemIds.Contains(itemId))
            || snapshot.AssetLinks.Any(link => IsNicheScopedAssetTarget(snapshot, link, nicheId))
            || snapshot.Assets.Any(asset => assetIds.Contains(asset.Id))
            || snapshot.ItemTags.Any(link => itemIds.Contains(link.ItemId));
    }

    private static bool IsNicheScopedAssetTarget(WorkspaceSnapshot snapshot, AssetLink link, Guid nicheId) =>
        link.EntityKind switch
        {
            WorkspaceEntityKind.Niche => link.EntityId == nicheId,
            WorkspaceEntityKind.Group => snapshot.Groups.Any(group => group.Id == link.EntityId && group.NicheId == nicheId),
            WorkspaceEntityKind.Item => snapshot.Items.Any(listing => listing.Id == link.EntityId && listing.NicheId == nicheId),
            WorkspaceEntityKind.Prompt => snapshot.Prompts.Any(prompt => prompt.Id == link.EntityId && prompt.ItemId is Guid itemId && snapshot.Items.Any(listing => listing.Id == itemId && listing.NicheId == nicheId)),
            _ => false
        };

    private static NicheSummary ToSummary(Niche niche) =>
        new(niche.Id, niche.StoreId, niche.Name, ToContext(niche), niche.IsArchived, niche.CreatedAt, niche.UpdatedAt);

    private static NicheContext ToContext(Niche niche)
    {
        var metadata = ParseMetadata(niche.MetadataJson);
        return new NicheContext(
            niche.Description,
            metadata.GetValueOrDefault(AudienceKey),
            metadata.GetValueOrDefault(HumorStyleKey),
            metadata.GetValueOrDefault(VisualStyleGuidanceKey),
            metadata.GetValueOrDefault(ConstraintsKey),
            metadata.GetValueOrDefault(RisksKey),
            metadata.GetValueOrDefault(ResearchNotesKey),
            metadata.GetValueOrDefault(NotesKey));
    }

    private static string ToMetadataJson(NicheContext context, string existingMetadataJson = "{}")
    {
        var metadata = ParseMetadata(existingMetadataJson);
        SetOptional(metadata, AudienceKey, context.Audience);
        SetOptional(metadata, HumorStyleKey, context.HumorStyle);
        SetOptional(metadata, VisualStyleGuidanceKey, context.VisualStyleGuidance);
        SetOptional(metadata, ConstraintsKey, context.Constraints);
        SetOptional(metadata, RisksKey, context.Risks);
        SetOptional(metadata, ResearchNotesKey, context.ResearchNotes);
        SetOptional(metadata, NotesKey, context.Notes);

        return metadata.Count == 0 ? "{}" : JsonSerializer.Serialize(metadata);
    }

    private static Dictionary<string, string> ParseMetadata(string metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson) || metadataJson.Trim() == "{}")
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        using var document = JsonDocument.Parse(metadataJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        return document.RootElement
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.ToString(), StringComparer.Ordinal);
    }

    private static void SetOptional(Dictionary<string, string> metadata, string key, string? value)
    {
        var normalized = NormalizeOptional(value);
        if (normalized is null)
        {
            metadata.Remove(key);
            return;
        }

        metadata[key] = normalized;
    }

    private static string NormalizeName(string name) => name.Trim();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
