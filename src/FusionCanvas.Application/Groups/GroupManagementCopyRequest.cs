namespace FusionCanvas.Application.Groups;

public sealed record GroupManagementCopyRequest(Guid GroupId, GroupParentReference Destination);
