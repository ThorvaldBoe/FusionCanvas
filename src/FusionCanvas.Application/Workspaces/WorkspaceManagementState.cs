namespace FusionCanvas.Application.Workspaces;

public sealed record WorkspaceManagementState(
    IReadOnlyList<WorkspaceSummary> ActiveWorkspaces,
    IReadOnlyList<WorkspaceSummary> ArchivedWorkspaces,
    Guid? ActiveWorkspaceId,
    WorkspaceSummary? ActiveWorkspace,
    bool NeedsFirstWorkspace);
