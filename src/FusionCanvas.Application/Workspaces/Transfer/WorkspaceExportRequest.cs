namespace FusionCanvas.Application.Workspaces.Transfer;

public sealed record WorkspaceExportRequest(Guid WorkspaceId, string DestinationPath);
