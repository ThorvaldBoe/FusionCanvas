namespace FusionCanvas.Application.Workspaces.Transfer;

public sealed record WorkspaceTransferProgress(string Phase, long Completed, long Total);
