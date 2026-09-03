using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.WorkspaceTree;

public sealed record WorkspaceTreeMoveValidation(
    bool IsValid,
    string? Error,
    IReadOnlyList<WorkspaceTreeSelection> EffectiveSources)
{
    public static WorkspaceTreeMoveValidation Valid(IReadOnlyList<WorkspaceTreeSelection> sources) =>
        new(true, null, sources);

    public static WorkspaceTreeMoveValidation Invalid(string error, IReadOnlyList<WorkspaceTreeSelection>? sources = null) =>
        new(false, error, sources ?? []);
}
