namespace FusionCanvas.Application.Items;

public sealed record ItemManagementUpdateRequest(Guid ItemId, string Name, ItemContext? Context = null);
