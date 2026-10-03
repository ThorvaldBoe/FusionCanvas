namespace FusionCanvas.Application.WorkspaceTree;

public sealed record WorkspaceBatchRollbackResult(bool Succeeded, string? Error)
{
    public static WorkspaceBatchRollbackResult Success() => new(true, null);

    public static WorkspaceBatchRollbackResult Failure(string error) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Workspace rollback failed." : error);
}
