using FusionCanvas.Application.Items;

namespace FusionCanvas.Application.Ideation;

public sealed record IdeationScope(
    Guid StoreId,
    Guid NicheId,
    Guid? GroupId,
    string DisplayPath,
    ItemTopicReference CreationTopic);
