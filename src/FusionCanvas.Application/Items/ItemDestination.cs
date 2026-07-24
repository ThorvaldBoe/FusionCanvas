namespace FusionCanvas.Application.Items;

public sealed record ItemDestination(ItemTopicReference Topic, Guid StoreId, Guid NicheId, string DisplayPath);
