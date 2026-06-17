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
    Other = 10
}

public enum WorkspaceEntityKind
{
    Store = 0,
    Niche = 1,
    Group = 2,
    Listing = 3
}

public sealed record ListingTag(Guid ListingId, Guid TagId);

public sealed record AssetLink(Guid AssetId, WorkspaceEntityKind EntityKind, Guid EntityId);
