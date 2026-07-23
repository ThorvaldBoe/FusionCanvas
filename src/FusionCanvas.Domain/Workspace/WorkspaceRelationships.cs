namespace FusionCanvas.Domain.Workspace;

public enum ItemStatus
{
    Draft = 0,
    Published = 1,
    Paused = 2,
    Rejected = 3
}

public static class ItemStatuses
{
    public static IReadOnlyList<ItemStatus> Ordered { get; } =
    [
        ItemStatus.Draft,
        ItemStatus.Published,
        ItemStatus.Paused,
        ItemStatus.Rejected
    ];

    public static string GetDisplayName(ItemStatus status) =>
        status switch
        {
            ItemStatus.Draft => "Draft",
            ItemStatus.Published => "Published",
            ItemStatus.Paused => "Paused",
            ItemStatus.Rejected => "Rejected",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported item lifecycle status.")
        };
}

public enum AssetKind
{
    SourceDesign = 0,
    ExportedImage = 1,
    Svg = 2,
    MockupImage = 3,
    ReferenceImage = 4,
    Texture = 5,
    Brush = 6,
    Font = 7,
    PromptOutput = 8,
    ExternalLink = 9,
    Unknown = 10,
    Other = 11
}

public enum WorkspaceEntityKind
{
    Store = 0,
    Niche = 1,
    Group = 2,
    Item = 3,
    Asset = 4,
    Prompt = 5,
    Design = 6,
    FutureRelatedRecord = 7
}

public sealed record ItemTag(Guid ItemId, Guid TagId);

public sealed record AssetLink(Guid AssetId, WorkspaceEntityKind EntityKind, Guid EntityId)
{
    public Guid AssetId { get; } = AssetId == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(AssetId))
        : AssetId;

    public Guid EntityId { get; } = EntityId == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(EntityId))
        : EntityId;
}
