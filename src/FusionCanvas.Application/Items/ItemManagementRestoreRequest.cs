namespace FusionCanvas.Application.Items;

public sealed record ItemManagementRestoreRequest(Guid ItemId, ItemTopicReference? Destination = null);
