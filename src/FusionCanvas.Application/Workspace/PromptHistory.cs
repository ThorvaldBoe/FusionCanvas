using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspace;

public sealed record PromptSummary(
    Guid Id,
    Guid StoreId,
    Guid? ListingId,
    string Name,
    string Text,
    string? Output,
    string? PromptType,
    string? Provider,
    string? Model,
    ConceptLifecycle Lifecycle,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PromptHistoryState(
    Guid? ActiveStoreId,
    Guid? ActiveListingId,
    IReadOnlyList<PromptSummary> ActivePrompts,
    IReadOnlyList<PromptSummary> SupersededPrompts,
    IReadOnlyList<PromptSummary> RejectedPrompts)
{
    public static PromptHistoryState Empty { get; } = new(null, null, [], [], []);
}

public sealed record PromptHistoryResult(
    bool Succeeded,
    string? Error,
    PromptSummary? Prompt,
    PromptHistoryState State)
{
    public static PromptHistoryResult Success(PromptSummary? prompt, PromptHistoryState state) =>
        new(true, null, prompt, state);

    public static PromptHistoryResult Failure(string error, PromptHistoryState state) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Prompt operation failed." : error, null, state);
}

public sealed record PromptSaveRequest(
    Guid StoreId,
    Guid? ListingId,
    string Name,
    string Text,
    string? Output = null,
    string? PromptType = null,
    string? Provider = null,
    string? Model = null);

public sealed record PromptAssociationRequest(Guid PromptId, WorkspaceEntityKind EntityKind, Guid EntityId);

public interface IPromptHistoryService
{
    Guid? ActiveStoreId { get; }
    Guid? ActiveListingId { get; }

    void SetActiveWorkspace(Guid? workspaceId);
    Task<PromptHistoryState> LoadAsync(Guid? storeId, Guid? listingId, CancellationToken cancellationToken = default);
    Task<PromptHistoryResult> SaveAsync(PromptSaveRequest request, CancellationToken cancellationToken = default);
    Task<PromptHistoryResult> RejectAsync(Guid promptId, CancellationToken cancellationToken = default);
    Task<PromptHistoryResult> SupersedeAsync(Guid promptId, CancellationToken cancellationToken = default);
    Task<PromptHistoryResult> CopyInputAsync(Guid promptId, CancellationToken cancellationToken = default);
}

public sealed class PromptHistoryService : IPromptHistoryService
{
    private readonly IWorkspaceRepository _repository;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;
    private Guid? _activeWorkspaceId;
    private Guid? _activeStoreId;
    private Guid? _activeListingId;

    public PromptHistoryService(
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

    public async Task<PromptHistoryState> LoadAsync(Guid? storeId, Guid? listingId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (storeId is Guid sid)
        {
            _activeStoreId = snapshot.Stores.Any(s => s.Id == sid && !s.IsArchived) ? sid : null;
        }
        _activeListingId = listingId;
        return BuildState(snapshot);
    }

    public async Task<PromptHistoryResult> SaveAsync(PromptSaveRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var store = snapshot.Stores.SingleOrDefault(s => s.Id == request.StoreId && !s.IsArchived);
        if (store is null)
        {
            return PromptHistoryResult.Failure("Store was not found or is archived.", BuildState(snapshot));
        }

        var now = _clock();
        var metadata = BuildPromptMetadata(request);
        var prompt = new Prompt(
            _newId(),
            store.Id,
            request.ListingId,
            request.Name.Trim(),
            null,
            request.Text,
            false,
            now,
            now,
            metadata);

        var updated = snapshot with { Prompts = [.. snapshot.Prompts, prompt] };

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return PromptHistoryResult.Failure(saveError, BuildState(snapshot));
        }

        _activeStoreId = store.Id;
        _activeListingId = request.ListingId;
        return PromptHistoryResult.Success(ToSummary(prompt), BuildState(updated));
    }

    public async Task<PromptHistoryResult> RejectAsync(Guid promptId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Prompts.SingleOrDefault(p => p.Id == promptId);
        if (existing is null)
        {
            return PromptHistoryResult.Failure("Prompt was not found.", BuildState(snapshot));
        }

        var metadata = ListingMetadataCodec.ParseMetadata(existing.MetadataJson);
        metadata["prompt.lifecycle"] = "rejected";
        var changed = existing with
        {
            MetadataJson = ListingMetadataCodec.SerializeMetadata(metadata),
            IsArchived = true,
            UpdatedAt = _clock()
        };
        var updated = snapshot with { Prompts = snapshot.Prompts.Select(p => p.Id == existing.Id ? changed : p).ToArray() };

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return PromptHistoryResult.Failure(saveError, BuildState(snapshot));
        }

        return PromptHistoryResult.Success(ToSummary(changed), BuildState(updated));
    }

    public async Task<PromptHistoryResult> SupersedeAsync(Guid promptId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Prompts.SingleOrDefault(p => p.Id == promptId);
        if (existing is null)
        {
            return PromptHistoryResult.Failure("Prompt was not found.", BuildState(snapshot));
        }

        var metadata = ListingMetadataCodec.ParseMetadata(existing.MetadataJson);
        metadata["prompt.lifecycle"] = "superseded";
        var changed = existing with
        {
            MetadataJson = ListingMetadataCodec.SerializeMetadata(metadata),
            UpdatedAt = _clock()
        };
        var updated = snapshot with { Prompts = snapshot.Prompts.Select(p => p.Id == existing.Id ? changed : p).ToArray() };

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return PromptHistoryResult.Failure(saveError, BuildState(snapshot));
        }

        return PromptHistoryResult.Success(ToSummary(changed), BuildState(updated));
    }

    public Task<PromptHistoryResult> CopyInputAsync(Guid promptId, CancellationToken cancellationToken = default)
    {
        var snapshot = _repository.LoadAsync(cancellationToken).GetAwaiter().GetResult();
        var existing = snapshot.Prompts.SingleOrDefault(p => p.Id == promptId);
        if (existing is null)
        {
            return Task.FromResult(PromptHistoryResult.Failure("Prompt was not found.", BuildState(snapshot)));
        }

        return Task.FromResult(PromptHistoryResult.Success(ToSummary(existing), BuildState(snapshot)));
    }

    private static string BuildPromptMetadata(PromptSaveRequest request)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(request.Output))
        {
            metadata["prompt.output"] = request.Output!.Trim();
        }
        if (!string.IsNullOrWhiteSpace(request.PromptType))
        {
            metadata["prompt.type"] = request.PromptType!.Trim();
        }
        if (!string.IsNullOrWhiteSpace(request.Provider))
        {
            metadata["prompt.provider"] = request.Provider!.Trim();
        }
        if (!string.IsNullOrWhiteSpace(request.Model))
        {
            metadata["prompt.model"] = request.Model!.Trim();
        }
        return ListingMetadataCodec.SerializeMetadata(metadata);
    }

    private static PromptSummary ToSummary(Prompt prompt)
    {
        var metadata = ListingMetadataCodec.ParseMetadata(prompt.MetadataJson);
        metadata.TryGetValue("prompt.output", out var output);
        metadata.TryGetValue("prompt.type", out var type);
        metadata.TryGetValue("prompt.provider", out var provider);
        metadata.TryGetValue("prompt.model", out var model);
        metadata.TryGetValue("prompt.lifecycle", out var lifecycleStr);
        var lifecycle = lifecycleStr switch
        {
            "rejected" => ConceptLifecycle.Rejected,
            "superseded" => ConceptLifecycle.Superseded,
            _ => ConceptLifecycle.Active
        };
        return new PromptSummary(
            prompt.Id, prompt.StoreId, prompt.ListingId, prompt.Name, prompt.Text,
            output, type, provider, model, lifecycle, prompt.CreatedAt, prompt.UpdatedAt);
    }

    private PromptHistoryState BuildState(WorkspaceSnapshot snapshot)
    {
        var storePrompts = _activeStoreId is Guid sid
            ? snapshot.Prompts.Where(p => p.StoreId == sid).ToArray()
            : [];
        var listingPrompts = _activeListingId is Guid lid
            ? storePrompts.Where(p => p.ListingId == lid).ToArray()
            : storePrompts;

        var active = listingPrompts.Where(p => !p.IsArchived && GetLifecycle(p) == ConceptLifecycle.Active).OrderBy(p => p.CreatedAt).Select(ToSummary).ToArray();
        var superseded = listingPrompts.Where(p => GetLifecycle(p) == ConceptLifecycle.Superseded).OrderByDescending(p => p.UpdatedAt).Select(ToSummary).ToArray();
        var rejected = listingPrompts.Where(p => GetLifecycle(p) == ConceptLifecycle.Rejected).OrderByDescending(p => p.UpdatedAt).Select(ToSummary).ToArray();

        return new PromptHistoryState(_activeStoreId, _activeListingId, active, superseded, rejected);
    }

    private static ConceptLifecycle GetLifecycle(Prompt prompt)
    {
        var metadata = ListingMetadataCodec.ParseMetadata(prompt.MetadataJson);
        return metadata.TryGetValue("prompt.lifecycle", out var value) ? value switch
        {
            "rejected" => ConceptLifecycle.Rejected,
            "superseded" => ConceptLifecycle.Superseded,
            _ => ConceptLifecycle.Active
        } : ConceptLifecycle.Active;
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
            return $"The prompt change could not be saved. {exception.Message}";
        }
    }
}
