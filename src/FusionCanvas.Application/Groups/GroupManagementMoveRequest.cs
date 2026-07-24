namespace FusionCanvas.Application.Groups;

public sealed record GroupManagementMoveRequest(
    Guid GroupId,
    GroupParentReference Destination,
    GroupPlacement? Placement = null);
