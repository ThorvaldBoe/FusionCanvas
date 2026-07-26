namespace FusionCanvas.Application.Items;

public sealed record ItemManagementCreateRequest(ItemTopicReference Topic, string Name, ItemContext? Context = null);
