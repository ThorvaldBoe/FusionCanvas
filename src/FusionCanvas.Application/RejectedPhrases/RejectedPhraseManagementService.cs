using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.RejectedPhrases;

public sealed class RejectedPhraseManagementService : IRejectedPhraseManagementService
{
    private readonly IWorkspaceRepository _repository;
    private readonly Func<Guid> _idGenerator;
    private readonly Func<DateTimeOffset> _clock;

    public RejectedPhraseManagementService(
        IWorkspaceRepository repository,
        Func<Guid>? idGenerator = null,
        Func<DateTimeOffset>? clock = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _idGenerator = idGenerator ?? Guid.NewGuid;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public async Task<RejectedPhraseManagementResult> InitializeAsync(
        RejectedPhraseScope scope,
        string? searchText = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var snapshot = await LoadSnapshotAsync(scope, searchText, cancellationToken).ConfigureAwait(false);
        return snapshot.Result ?? BuildSuccess(snapshot.Snapshot!, scope, searchText);
    }

    public async Task<RejectedPhraseManagementResult> LoadAsync(
        RejectedPhraseScope scope,
        string? searchText = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var snapshot = await LoadSnapshotAsync(scope, searchText, cancellationToken).ConfigureAwait(false);
        return snapshot.Result ?? BuildSuccess(snapshot.Snapshot!, scope, searchText);
    }

    public async Task<RejectedPhraseManagementResult> CreateAsync(
        RejectedPhraseCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var scope = request.Scope;
        var snapshot = await LoadSnapshotAsync(scope, request.SearchText, cancellationToken).ConfigureAwait(false);
        if (snapshot.Result is { } loadFailure)
        {
            return loadFailure;
        }

        var current = snapshot.Snapshot!;
        var scopeValidation = ValidateMutationScope(current, scope);
        if (scopeValidation is not null)
        {
            return Failure(current, scope, request.SearchText, scopeValidation);
        }

        var trimmedText = request.Text?.Trim() ?? string.Empty;
        if (trimmedText.Length == 0)
        {
            return Failure(current, scope, request.SearchText, "Rejected phrase text is required.");
        }

        var trimmedReason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();

        var draft = new IdeationRejection(
            _idGenerator(),
            scope.StoreId!.Value,
            scope.NicheId!.Value,
            scope.GroupId,
            trimmedText,
            trimmedReason,
            IdeationMode.Basic,
            _clock());

        if (HasWithinScopeDuplicate(current.IdeationRejections, draft))
        {
            return Failure(
                current,
                scope,
                request.SearchText,
                "A rejected phrase with the same text already exists in this scope.");
        }

        var updated = current with { IdeationRejections = [.. current.IdeationRejections, draft] };
        var saveFailure = await TrySaveAsync(current, updated, scope, request.SearchText, cancellationToken).ConfigureAwait(false);
        return saveFailure ?? BuildSuccess(updated, scope, request.SearchText, RejectedPhraseSummary.From(draft));
    }

    public async Task<RejectedPhraseManagementResult> UpdateAsync(
        RejectedPhraseUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var scope = request.Scope;
        var snapshot = await LoadSnapshotAsync(scope, request.SearchText, cancellationToken).ConfigureAwait(false);
        if (snapshot.Result is { } loadFailure)
        {
            return loadFailure;
        }

        var current = snapshot.Snapshot!;
        var existing = current.IdeationRejections.SingleOrDefault(candidate => candidate.Id == request.Id);
        if (existing is null)
        {
            return Failure(current, scope, request.SearchText, "The rejected phrase was not found.");
        }

        var trimmedText = request.Text?.Trim() ?? string.Empty;
        if (trimmedText.Length == 0)
        {
            return Failure(current, scope, request.SearchText, "Rejected phrase text is required.");
        }

        var trimmedReason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();

        var edited = existing with
        {
            Text = trimmedText,
            Reason = trimmedReason,
            UpdatedAt = _clock()
        };

        if (HasWithinScopeDuplicate(current.IdeationRejections, edited))
        {
            return Failure(
                current,
                scope,
                request.SearchText,
                "A rejected phrase with the same text already exists in this scope.");
        }

        var updated = current with
        {
            IdeationRejections = current.IdeationRejections
                .Select(candidate => candidate.Id == request.Id ? edited : candidate)
                .ToArray()
        };

        var saveFailure = await TrySaveAsync(current, updated, scope, request.SearchText, cancellationToken).ConfigureAwait(false);
        return saveFailure ?? BuildSuccess(updated, scope, request.SearchText, RejectedPhraseSummary.From(edited));
    }

    public async Task<RejectedPhraseManagementResult> DeleteAsync(
        Guid id,
        RejectedPhraseScope scope,
        string? searchText = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var snapshot = await LoadSnapshotAsync(scope, searchText, cancellationToken).ConfigureAwait(false);
        if (snapshot.Result is { } loadFailure)
        {
            return loadFailure;
        }

        var current = snapshot.Snapshot!;
        var existing = current.IdeationRejections.SingleOrDefault(candidate => candidate.Id == id);
        if (existing is null)
        {
            return Failure(current, scope, searchText, "The rejected phrase was not found.");
        }

        var updated = current with
        {
            IdeationRejections = current.IdeationRejections.Where(candidate => candidate.Id != id).ToArray()
        };

        var saveFailure = await TrySaveAsync(current, updated, scope, searchText, cancellationToken).ConfigureAwait(false);
        return saveFailure ?? BuildSuccess(updated, scope, searchText, RejectedPhraseSummary.From(existing));
    }

    private async Task<(WorkspaceSnapshot? Snapshot, RejectedPhraseManagementResult? Result)> LoadSnapshotAsync(
        RejectedPhraseScope scope,
        string? searchText,
        CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
            return (snapshot, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return (null, Failure(WorkspaceSnapshot.Empty, scope, searchText, $"Unable to load rejected phrases: {ex.Message}"));
        }
    }

    private async Task<RejectedPhraseManagementResult?> TrySaveAsync(
        WorkspaceSnapshot confirmed,
        WorkspaceSnapshot updated,
        RejectedPhraseScope scope,
        string? searchText,
        CancellationToken cancellationToken)
    {
        try
        {
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Failure(confirmed, scope, searchText, $"Unable to save the rejected phrase: {ex.Message}");
        }
    }

    private static string? ValidateMutationScope(WorkspaceSnapshot snapshot, RejectedPhraseScope scope)
    {
        if (!scope.IsSingleStoreNiche)
        {
            return "Select a single store and niche scope before creating a rejected phrase.";
        }

        var storeId = scope.StoreId!.Value;
        if (snapshot.Stores.All(store => store.Id != storeId))
        {
            return "The selected store is no longer available.";
        }

        var nicheId = scope.NicheId!.Value;
        if (snapshot.Niches.All(niche => niche.Id != nicheId))
        {
            return "The selected niche is no longer available.";
        }

        if (scope.GroupId is { } groupId && snapshot.Groups.All(group => group.Id != groupId))
        {
            return "The selected group is no longer available.";
        }

        return null;
    }

    private static bool HasWithinScopeDuplicate(IReadOnlyList<IdeationRejection> rejections, IdeationRejection candidate) =>
        rejections.Any(other => RejectionPhraseComparison.IsWithinScopeDuplicate(other, candidate));

    private static RejectedPhraseManagementResult BuildSuccess(
        WorkspaceSnapshot snapshot,
        RejectedPhraseScope scope,
        string? searchText,
        RejectedPhraseSummary? affected = null) =>
        RejectedPhraseManagementResult.Success(BuildState(snapshot, scope, searchText), affected);

    private static RejectedPhraseManagementResult Failure(
        WorkspaceSnapshot snapshot,
        RejectedPhraseScope scope,
        string? searchText,
        string error) =>
        RejectedPhraseManagementResult.Failure(error, BuildState(snapshot, scope, searchText));

    private static RejectedPhraseManagementState BuildState(
        WorkspaceSnapshot snapshot,
        RejectedPhraseScope scope,
        string? searchText)
    {
        var normalizedSearch = searchText?.Trim() ?? string.Empty;
        var inScope = snapshot.IdeationRejections.Where(rejection => IsWithinScope(rejection, scope));
        var all = Sort(inScope).Select(RejectedPhraseSummary.From).ToArray();
        var visible = normalizedSearch.Length == 0
            ? all
            : all.Where(summary =>
                    summary.Text.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                    (summary.Reason ?? string.Empty).Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                .ToArray();

        return new RejectedPhraseManagementState(all, visible, scope, normalizedSearch);
    }

    private static bool IsWithinScope(IdeationRejection rejection, RejectedPhraseScope scope)
    {
        if (scope.WholeWorkspace)
        {
            return true;
        }

        if (scope.StoreId is { } storeId && rejection.StoreId != storeId)
        {
            return false;
        }

        if (scope.NicheId is { } nicheId && rejection.NicheId != nicheId)
        {
            return false;
        }

        if (scope.GroupId is { } groupId && rejection.GroupId != groupId)
        {
            return false;
        }

        if (scope.GroupId is null && scope.NicheId is null && scope.StoreId is not null)
        {
            return true;
        }

        if (scope.GroupId is null && scope.NicheId is not null)
        {
            return true;
        }

        return true;
    }

    private static IEnumerable<IdeationRejection> Sort(IEnumerable<IdeationRejection> rejections) =>
        rejections
            .OrderBy(rejection => rejection.Text, StringComparer.OrdinalIgnoreCase)
            .ThenBy(rejection => rejection.Id);
}
