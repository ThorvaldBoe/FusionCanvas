namespace FusionCanvas.Application.Workspaces;

public sealed record WorkspaceManagementDeleteRequest(
    Guid WorkspaceId,
    bool ConfirmPermanentDeletion,
    string? ConfirmationName = null);
