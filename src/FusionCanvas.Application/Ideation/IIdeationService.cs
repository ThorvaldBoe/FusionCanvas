using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.Ideation;

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
