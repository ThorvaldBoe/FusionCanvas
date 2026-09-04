using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.WorkspaceTree;

public sealed record WorkspaceTreeBatchResult(
    bool Succeeded,
    string? Error,
    IReadOnlyList<WorkspaceTreeBatchOutcome> Outcomes)
{
    public static WorkspaceTreeBatchResult Success(IReadOnlyList<WorkspaceTreeBatchOutcome> outcomes) =>
        new(true, null, outcomes);

    public static WorkspaceTreeBatchResult Failure(string error, IReadOnlyList<WorkspaceTreeBatchOutcome>? outcomes = null) =>
        new(false, error, outcomes ?? []);
}
