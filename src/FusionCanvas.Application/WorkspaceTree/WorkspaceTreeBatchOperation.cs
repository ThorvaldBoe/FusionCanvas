using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.WorkspaceTree;

public enum WorkspaceTreeBatchAction
{
    OpenInNewTabs,
    Duplicate,
    Delete,
    Archive,
    Export,
    Group,
    Move
}

public sealed record WorkspaceTreeBatchSelection(
    IReadOnlyList<WorkspaceTreeSelection> Sources,
    WorkspaceTreeSelection? Active,
    WorkspaceTreeSelection? Anchor)
{
    public IReadOnlyList<WorkspaceTreeSelection> EffectiveSources { get; init; } = Sources;
}

public sealed record WorkspaceTreeBatchOutcome(
    WorkspaceTreeSelection Selection,
    bool Succeeded,
    bool Skipped,
    string? Message);

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
