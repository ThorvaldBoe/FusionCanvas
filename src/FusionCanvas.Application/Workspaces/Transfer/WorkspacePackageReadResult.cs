namespace FusionCanvas.Application.Workspaces.Transfer;

public sealed record WorkspacePackageReadResult(
    bool Succeeded,
    IWorkspacePackageReadSession? Session,
    string? Error)
{
    public static WorkspacePackageReadResult Success(IWorkspacePackageReadSession session) =>
        new(true, session, null);

    public static WorkspacePackageReadResult Failure(string error) =>
        new(false, null, error);
}
