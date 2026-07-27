namespace FusionCanvas.Application.Workspaces.Transfer;

public sealed record WorkspaceTransferResult(
    bool Succeeded,
    bool Cancelled,
    Guid? WorkspaceId,
    WorkspaceTransferSummary? Summary,
    string? Error)
{
    public static WorkspaceTransferResult Success(Guid workspaceId, WorkspaceTransferSummary summary) =>
        new(true, false, workspaceId, summary, null);

    public static WorkspaceTransferResult Failure(string error) =>
        new(false, false, null, null, error);

    public static WorkspaceTransferResult CancelledResult() =>
        new(false, true, null, null, "The workspace transfer was cancelled.");
}
