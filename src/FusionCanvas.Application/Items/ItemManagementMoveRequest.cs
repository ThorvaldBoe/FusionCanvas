namespace FusionCanvas.Application.Items;

public sealed record ItemManagementMoveRequest(Guid ItemId, ItemTopicReference Destination);
