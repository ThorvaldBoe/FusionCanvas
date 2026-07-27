using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Ideation;

public sealed class IdeationService : IIdeationService
{
    public const int MinimumCount = 1;
    public const int MaximumCount = 20;
    public const int MaximumConcurrency = 4;

    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);
    private readonly IWorkspaceRepository _repository;
    private readonly IItemManagementService _itemManagement;
    private readonly IIdeaGenerator _generator;
    private readonly ISnowcloneCatalog _snowclones;
    private readonly IIdeationAccessStatus _access;
    private readonly IdeationScopeResolver _scopeResolver;
    private readonly Func<Guid> _idGenerator;
    private readonly Func<DateTimeOffset> _clock;

    public IdeationService(
        IWorkspaceRepository repository,
        IItemManagementService itemManagement,
        IIdeaGenerator generator,
        ISnowcloneCatalog snowclones,
        IIdeationAccessStatus access,
        IdeationScopeResolver? scopeResolver = null,
        Func<Guid>? idGenerator = null,
        Func<DateTimeOffset>? clock = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _itemManagement = itemManagement ?? throw new ArgumentNullException(nameof(itemManagement));
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _snowclones = snowclones ?? throw new ArgumentNullException(nameof(snowclones));
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _scopeResolver = scopeResolver ?? new IdeationScopeResolver();
        _idGenerator = idGenerator ?? Guid.NewGuid;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public IdeationScopeResult ResolveScope(WorkspaceSnapshot snapshot, WorkspaceEntityKind entityKind, Guid entityId) =>
        _scopeResolver.Resolve(snapshot, entityKind, entityId);

    public async Task<IdeationGenerationResult> GenerateAsync(
        IdeationGenerationRequest request,
        IProgress<IdeationGenerationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!_access.GetAvailability().IsAvailable)
        {
            return IdeationGenerationResult.Failure("Placeholder AI access is not configured.", request.Count);
        }

        if (!Enum.IsDefined(request.Mode))
        {
            return IdeationGenerationResult.Failure("The selected Ideation mode is not supported.", request.Count);
        }

        if (request.Count is < MinimumCount or > MaximumCount)
        {
            return IdeationGenerationResult.Failure($"Idea count must be between {MinimumCount} and {MaximumCount}.", request.Count);
        }

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var scopeResult = RevalidateScope(snapshot, request.Scope);
        if (!scopeResult.IsAvailable)
        {
            return IdeationGenerationResult.Failure(scopeResult.Error!, request.Count);
        }

        var context = AssembleContext(snapshot, scopeResult.Scope!, request.Guidance, request.Mode);
        var templates = request.Mode == IdeationMode.Snowclones ? _snowclones.GetTemplates(request.Count) : [];
        var outputs = new ConcurrentDictionary<int, string>();
        var failed = 0;
        var completed = 0;

        try
        {
            await Parallel.ForEachAsync(
                Enumerable.Range(0, request.Count),
                new ParallelOptions { MaxDegreeOfParallelism = MaximumConcurrency, CancellationToken = cancellationToken },
                async (index, token) =>
                {
                    try
                    {
                        var operationContext = context with
                        {
                            SnowcloneTemplate = request.Mode == IdeationMode.Snowclones ? templates[index] : null
                        };
                        var text = await _generator.GenerateAsync(operationContext, index, token).ConfigureAwait(false);
                        outputs[index] = text;
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch
                    {
                        Interlocked.Increment(ref failed);
                    }
                    finally
                    {
                        var current = Interlocked.Increment(ref completed);
                        progress?.Report(new IdeationGenerationProgress(current, request.Count));
                    }
                }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new IdeationGenerationResult(false, true, [], request.Count, completed, failed, null);
        }

        var seen = new HashSet<string>(
            (request.ExistingCandidates ?? []).Select(ComparisonKey).Where(key => key.Length > 0),
            StringComparer.OrdinalIgnoreCase);
        var candidates = new List<IdeationCandidate>();
        foreach (var output in outputs.OrderBy(pair => pair.Key))
        {
            var displayText = output.Value.Trim();
            var key = ComparisonKey(displayText);
            if (key.Length > 0 && seen.Add(key))
            {
                candidates.Add(new IdeationCandidate(output.Key, displayText));
            }
        }

        var error = failed switch
        {
            0 => null,
            var count when count == request.Count => "No ideas could be generated. Try again.",
            _ => $"{failed} of {request.Count} ideas could not be generated."
        };
        return new IdeationGenerationResult(
            failed < request.Count,
            false,
            candidates,
            request.Count,
            completed,
            failed,
            error);
    }

    public async Task<IdeationDecisionResult> CreateAsync(
        IdeationScope scope,
        string candidateText,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var normalized = NormalizeCandidate(candidateText);
        var before = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var scopeResult = RevalidateScope(before, scope);
        if (!scopeResult.IsAvailable)
        {
            return new IdeationDecisionResult(false, scopeResult.Error, before);
        }

        if (normalized is null)
        {
            return new IdeationDecisionResult(false, "Candidate idea text is required.", before);
        }

        var title = FirstSentence(normalized);
        var result = await _itemManagement.CreateItemAsync(
            new ItemManagementCreateRequest(
                scopeResult.Scope!.CreationTopic,
                title,
                new ItemContext(Metadata: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [ItemMetadataCodec.IdeaKey] = normalized
                })),
            cancellationToken).ConfigureAwait(false);
        var state = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var created = result.Item is null ? null : state.Items.SingleOrDefault(item => item.Id == result.Item.Id);
        return result.Succeeded && created is not null
            ? new IdeationDecisionResult(true, null, state, CreatedItem: created)
            : new IdeationDecisionResult(false, result.Error ?? "The idea could not be created.", state);
    }

    public async Task<IdeationDecisionResult> RejectAsync(
        IdeationScope scope,
        string candidateText,
        string? reason,
        IdeationMode mode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var scopeResult = RevalidateScope(snapshot, scope);
        if (!scopeResult.IsAvailable)
        {
            return new IdeationDecisionResult(false, scopeResult.Error, snapshot);
        }

        IdeationRejection rejection;
        try
        {
            rejection = new IdeationRejection(
                _idGenerator(),
                scope.StoreId,
                scope.NicheId,
                scope.GroupId,
                candidateText,
                reason,
                mode,
                _clock());
        }
        catch (ArgumentException exception)
        {
            return new IdeationDecisionResult(false, exception.Message, snapshot);
        }

        var updated = snapshot with { IdeationRejections = [.. snapshot.IdeationRejections, rejection] };
        try
        {
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
            return new IdeationDecisionResult(true, null, updated, Rejection: rejection);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new IdeationDecisionResult(false, $"The rejection could not be saved. {exception.Message}", snapshot);
        }
    }

    internal IdeationGenerationContext AssembleContext(
        WorkspaceSnapshot snapshot,
        IdeationScope scope,
        string? guidance,
        IdeationMode mode)
    {
        var store = snapshot.Stores.Single(store => store.Id == scope.StoreId);
        var niche = snapshot.Niches.Single(niche => niche.Id == scope.NicheId);
        var group = scope.GroupId is Guid groupId
            ? snapshot.Groups.Single(candidate => candidate.Id == groupId)
            : null;

        bool InScope(Item item) =>
            item.NicheId == scope.NicheId &&
            (scope.GroupId is null || item.GroupId == scope.GroupId);
        var activeIdeas = snapshot.Items
            .Where(InScope)
            .Where(item => !item.IsArchived && item.Status != ItemStatus.Rejected)
            .Select(TryGetIdea)
            .Where(text => text is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var rejectedItems = snapshot.Items
            .Where(InScope)
            .Where(item => item.Status == ItemStatus.Rejected)
            .Select(TryGetIdea)
            .Where(text => text is not null)
            .Cast<string>()
            .Select(text => new IdeationRejectedContext(text, null));
        var recorded = snapshot.IdeationRejections
            .Where(rejection => rejection.NicheId == scope.NicheId)
            .Where(rejection => scope.GroupId is null || rejection.GroupId == scope.GroupId)
            .Select(rejection => new IdeationRejectedContext(rejection.Text, rejection.Reason));

        return new IdeationGenerationContext(
            CreativeContext(store.Name, store.Description, store.MetadataJson),
            CreativeContext(niche.Name, niche.Description, niche.MetadataJson),
            group is null ? null : CreativeContext(group.Name, group.Description, group.MetadataJson),
            string.IsNullOrWhiteSpace(guidance) ? null : guidance.Trim(),
            mode,
            null,
            activeIdeas,
            rejectedItems.Concat(recorded).ToArray());
    }

    private IdeationScopeResult RevalidateScope(WorkspaceSnapshot snapshot, IdeationScope scope)
    {
        var kind = scope.GroupId is null ? WorkspaceEntityKind.Niche : WorkspaceEntityKind.Group;
        var id = scope.GroupId ?? scope.NicheId;
        var result = _scopeResolver.Resolve(snapshot, kind, id);
        return result.IsAvailable &&
               result.Scope!.StoreId == scope.StoreId &&
               result.Scope.NicheId == scope.NicheId &&
               result.Scope.GroupId == scope.GroupId
            ? result
            : IdeationScopeResult.Unavailable("The Ideation scope is no longer available.");
    }

    private static IdeationCreativeContext CreativeContext(string name, string? description, string metadataJson) =>
        new(name, string.IsNullOrWhiteSpace(description) ? null : description.Trim(), SanitizeMetadata(metadataJson));

    private static IReadOnlyDictionary<string, string> SanitizeMetadata(string metadataJson)
    {
        Dictionary<string, string> parsed;
        try
        {
            parsed = ItemMetadataCodec.ParseMetadata(metadataJson);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }

        return parsed
            .Where(pair => !IsOperationalKey(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private static bool IsOperationalKey(string key)
    {
        var normalized = key.Trim().ToLowerInvariant();
        var compact = new string(normalized.Where(char.IsLetterOrDigit).ToArray());
        return normalized.StartsWith(ItemMetadataCodec.InheritedFromPrefix.ToLowerInvariant(), StringComparison.Ordinal) ||
               compact is "id" or "createdat" or "updatedat" or "isarchived" or "status" ||
               compact.Contains("path", StringComparison.Ordinal) ||
               compact.Contains("apikey", StringComparison.Ordinal) ||
               compact.Contains("credential", StringComparison.Ordinal) ||
               compact.Contains("secret", StringComparison.Ordinal) ||
               compact.Contains("token", StringComparison.Ordinal);
    }

    private static string? TryGetIdea(Item item)
    {
        try
        {
            var metadata = ItemMetadataCodec.ParseMetadata(item.MetadataJson);
            return metadata.TryGetValue(ItemMetadataCodec.IdeaKey, out var idea) && !string.IsNullOrWhiteSpace(idea)
                ? idea.Trim()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? NormalizeCandidate(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ComparisonKey(string value) => Whitespace.Replace(value.Trim(), " ");

    private static string FirstSentence(string text)
    {
        var line = Whitespace.Replace(text, " ");
        var end = line.IndexOfAny(['.', '!', '?']);
        return end < 0 ? line : line[..(end + 1)].Trim();
    }
}
