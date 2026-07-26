namespace FusionCanvas.Application.Workspaces;

public sealed record WorkspaceManagementUpdateRequest(Guid WorkspaceId, string Name, WorkspaceContext? Context = null);
