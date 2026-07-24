namespace FusionCanvas.Application.Workspaces;

public sealed record WorkspaceManagementCreateRequest(string Name, WorkspaceContext? Context = null);
