namespace FusionCanvas.Application.Groups;

public sealed record GroupPlacement(GroupPlacementKind Kind = GroupPlacementKind.Append, Guid? RelativeGroupId = null);
