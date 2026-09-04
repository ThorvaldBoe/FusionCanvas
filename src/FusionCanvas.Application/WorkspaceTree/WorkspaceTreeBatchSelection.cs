using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.WorkspaceTree;

public sealed record WorkspaceTreeBatchSelection(
    IReadOnlyList<WorkspaceTreeSelection> Sources,
    WorkspaceTreeSelection? Active,
    WorkspaceTreeSelection? Anchor)
{
    public IReadOnlyList<WorkspaceTreeSelection> EffectiveSources { get; init; } = Sources;
}
