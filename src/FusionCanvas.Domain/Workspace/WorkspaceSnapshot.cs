namespace FusionCanvas.Domain.Workspace;

public sealed record WorkspaceSnapshot(
    IReadOnlyList<Store> Stores,
    IReadOnlyList<Niche> Niches,
    IReadOnlyList<TopicGroup> Groups,
    IReadOnlyList<Listing> Listings,
    IReadOnlyList<Asset> Assets,
    IReadOnlyList<Prompt> Prompts,
    IReadOnlyList<Tag> Tags,
    IReadOnlyList<ListingTag> ListingTags,
    IReadOnlyList<AssetLink> AssetLinks)
{
    public static WorkspaceSnapshot Empty { get; } = new(
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        []);
}
