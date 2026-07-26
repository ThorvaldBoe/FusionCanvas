namespace FusionCanvas.Application.Workspaces;

public sealed record WorkspaceManagementResult(
    bool Succeeded,
    string? Error,
    WorkspaceSummary? Workspace,
    WorkspaceManagementState State)
{
    public static WorkspaceManagementResult Success(WorkspaceSummary? workspace, WorkspaceManagementState state) =>
        new(true, null, workspace, state);

    public static WorkspaceManagementResult Failure(string error, WorkspaceManagementState state) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Workspace operation failed." : error, null, state);
}
