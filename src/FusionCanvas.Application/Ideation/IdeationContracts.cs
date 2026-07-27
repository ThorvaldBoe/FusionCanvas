using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.Ideation;

public sealed record IdeationAccessAvailability(bool IsAvailable, string? UnavailableReason)
{
    public static IdeationAccessAvailability Available { get; } = new(true, null);

    public static IdeationAccessAvailability Unavailable(string reason) => new(false, reason);
}

public interface IIdeationAccessStatus
{
    event EventHandler? AvailabilityChanged
    {
        add { }
        remove { }
    }

    IdeationAccessAvailability GetAvailability();

    Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public interface ISnowcloneCatalog
{
    Task<SnowcloneCatalogResult> GetSelectionsAsync(
        int count,
        CancellationToken cancellationToken = default);
}

public sealed record IdeationSnowcloneSelection(
    Guid Id,
    string Phrase,
    string Guidance,
    IReadOnlyList<string> PlaceholderTokens);

public sealed record SnowcloneCatalogResult(
    bool Succeeded,
    IReadOnlyList<IdeationSnowcloneSelection> Selections,
    string? Error)
{
    public static SnowcloneCatalogResult Success(IReadOnlyList<IdeationSnowcloneSelection> selections) =>
        new(true, selections, null);

    public static SnowcloneCatalogResult Failure(string error) =>
        new(false, [], error);
}

public interface IIdeaGenerator
{
    Task<IdeaGenerationResult> GenerateAsync(
        IdeationGenerationContext context,
        int requestIndex,
        CancellationToken cancellationToken = default);
}

public sealed record IdeaGenerationResult(
    bool Succeeded,
    string? Text,
    AiTextFailureKind? FailureKind,
    string? Error)
{
    public static IdeaGenerationResult Success(string text) => new(true, text, null, null);

    public static IdeaGenerationResult Failure(AiTextFailureKind kind, string error) =>
        new(false, null, kind, error);
}

public interface IIdeationService
{
    IdeationScopeResult ResolveScope(WorkspaceSnapshot snapshot, WorkspaceEntityKind entityKind, Guid entityId);

    Task<IdeationGenerationResult> GenerateAsync(
        IdeationGenerationRequest request,
        IProgress<IdeationGenerationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<IdeationDecisionResult> CreateAsync(
        IdeationScope scope,
        string candidateText,
        CancellationToken cancellationToken = default);

    Task<IdeationDecisionResult> RejectAsync(
        IdeationScope scope,
        string candidateText,
        string? reason,
        IdeationMode mode,
        CancellationToken cancellationToken = default);
}

public sealed record IdeationCreativeContext(
    string Name,
    string? Description,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record IdeationRejectedContext(string Text, string? Reason);

public sealed record IdeationGenerationContext(
    IdeationCreativeContext Store,
    IdeationCreativeContext Niche,
    IdeationCreativeContext? Group,
    string? Guidance,
    IdeationMode Mode,
    string? SnowcloneTemplate,
    string? SnowcloneGuidance,
    IReadOnlyList<string> SnowclonePlaceholderTokens,
    IReadOnlyList<string> ActiveIdeas,
    IReadOnlyList<IdeationRejectedContext> RejectedIdeas);

public sealed record IdeationGenerationRequest(
    IdeationScope Scope,
    IdeationMode Mode,
    string? Guidance,
    int Count,
    IReadOnlyCollection<string>? ExistingCandidates = null);

public sealed record IdeationCandidate(int RequestIndex, string Text);

public sealed record IdeationGenerationProgress(int Completed, int Requested);

public sealed record IdeationGenerationResult(
    bool Succeeded,
    bool Cancelled,
    IReadOnlyList<IdeationCandidate> Candidates,
    int Requested,
    int Completed,
    int Failed,
    string? Error)
{
    public static IdeationGenerationResult Failure(string error, int requested = 0) =>
        new(false, false, [], requested, 0, requested, error);
}

public sealed record IdeationDecisionResult(
    bool Succeeded,
    string? Error,
    WorkspaceSnapshot State,
    Item? CreatedItem = null,
    IdeationRejection? Rejection = null);
