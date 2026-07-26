namespace FusionCanvas.Application.Groups;

public sealed record GroupManagementDeleteRequest(Guid GroupId, bool ConfirmPermanentDeletion);
