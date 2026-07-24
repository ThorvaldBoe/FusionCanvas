namespace FusionCanvas.Application.Items;

public sealed record ItemManagementDuplicateRequest(Guid ItemId, ItemTopicReference? Destination = null);
