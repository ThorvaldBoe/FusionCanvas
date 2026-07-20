namespace FusionCanvas.Domain.Workspace;

public enum ListingStatus
{
    Draft = 0,
    Published = 1,
    Paused = 2,
    Rejected = 3
}

public static class ListingStatuses
{
    public static IReadOnlyList<ListingStatus> Ordered { get; } =
    [
        ListingStatus.Draft,
        ListingStatus.Published,
        ListingStatus.Paused,
        ListingStatus.Rejected
    ];

    public static string GetDisplayName(ListingStatus status) =>
        status switch
        {
            ListingStatus.Draft => "Draft",
            ListingStatus.Published => "Published",
            ListingStatus.Paused => "Paused",
            ListingStatus.Rejected => "Rejected",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported listing lifecycle status.")
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
    Listing = 3,
    Asset = 4,
    Prompt = 5,
    Design = 6,
    FutureRelatedRecord = 7,
    Concept = 8
}

public enum ConceptLifecycle
{
    Active = 0,
    Superseded = 1,
    Rejected = 2
}

public enum DesignApprovalState
{
    Draft = 0,
    NeedsRevision = 1,
    Approved = 2,
    Rejected = 3,
    Exported = 4,
    ReadyForExport = 5
}

public static class DesignApprovalStates
{
    public static IReadOnlyList<DesignApprovalState> Ordered { get; } =
    [
        DesignApprovalState.Draft,
        DesignApprovalState.NeedsRevision,
        DesignApprovalState.Approved,
        DesignApprovalState.Rejected,
        DesignApprovalState.Exported,
        DesignApprovalState.ReadyForExport
    ];

    public static string GetDisplayName(DesignApprovalState state) =>
        state switch
        {
            DesignApprovalState.Draft => "Draft",
            DesignApprovalState.NeedsRevision => "Needs Revision",
            DesignApprovalState.Approved => "Approved",
            DesignApprovalState.Rejected => "Rejected",
            DesignApprovalState.Exported => "Exported",
            DesignApprovalState.ReadyForExport => "Ready for Export",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported design approval state.")
        };
}

public static class ConceptLifecycles
{
    public static IReadOnlyList<ConceptLifecycle> Ordered { get; } =
    [
        ConceptLifecycle.Active,
        ConceptLifecycle.Superseded,
        ConceptLifecycle.Rejected
    ];

    public static string GetDisplayName(ConceptLifecycle lifecycle) =>
        lifecycle switch
        {
            ConceptLifecycle.Active => "Active",
            ConceptLifecycle.Superseded => "Superseded",
            ConceptLifecycle.Rejected => "Rejected",
            _ => throw new ArgumentOutOfRangeException(nameof(lifecycle), lifecycle, "Unsupported concept lifecycle.")
        };
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
