using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.WorkspaceTree;

public sealed record WorkspaceTreeBatchOutcome(
    WorkspaceTreeSelection Selection,
    bool Succeeded,
    bool Skipped,
    string? Message);
