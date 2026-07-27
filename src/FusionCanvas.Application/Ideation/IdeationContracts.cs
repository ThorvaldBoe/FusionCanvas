using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Ideation;

public sealed record IdeationAccessAvailability(bool IsAvailable, string? UnavailableReason)
{
    public static IdeationAccessAvailability Available { get; } = new(true, null);

    public static IdeationAccessAvailability Unavailable(string reason) => new(false, reason);
}

public interface IIdeationAccessStatus
{
    IdeationAccessAvailability GetAvailability();
}

public interface ISnowcloneCatalog
{
    IReadOnlyList<string> GetTemplates(int count);
}

public interface IIdeaGenerator
{
    Task<string> GenerateAsync(IdeationGenerationContext context, int requestIndex, CancellationToken cancellationToken = default);
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
