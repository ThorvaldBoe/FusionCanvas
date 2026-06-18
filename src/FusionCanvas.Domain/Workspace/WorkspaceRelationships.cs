namespace FusionCanvas.Domain.Workspace;

public enum ListingStatus
{
    Active = 0,
    Draft = 1,
    Ready = 2,
    Published = 3,
    Archived = 4
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
    Listing = 3,
    Asset = 4,
    Prompt = 5,
    Design = 6,
    FutureRelatedRecord = 7
}

public sealed record ListingTag(Guid ListingId, Guid TagId);

public sealed record AssetLink(Guid AssetId, WorkspaceEntityKind EntityKind, Guid EntityId)
{
    public Guid AssetId { get; } = AssetId == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(AssetId))
        : AssetId;

    public Guid EntityId { get; } = EntityId == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(EntityId))
        : EntityId;
}
