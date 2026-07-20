namespace FusionCanvas.Domain.Workspace;

public sealed record WorkspaceSnapshot(
    IReadOnlyList<Workspace> Workspaces,
    IReadOnlyList<Store> Stores,
    IReadOnlyList<Niche> Niches,
    IReadOnlyList<TopicGroup> Groups,
    IReadOnlyList<Listing> Listings,
    IReadOnlyList<Asset> Assets,
    IReadOnlyList<Prompt> Prompts,
    IReadOnlyList<Tag> Tags,
    IReadOnlyList<ListingTag> ListingTags,
    IReadOnlyList<AssetLink> AssetLinks,
    IReadOnlyList<Concept> Concepts,
    IReadOnlyList<Design> Designs,
    IReadOnlyList<Mockup> Mockups)
{
    public WorkspaceSnapshot(
        IReadOnlyList<Store> Stores,
        IReadOnlyList<Niche> Niches,
        IReadOnlyList<TopicGroup> Groups,
        IReadOnlyList<Listing> Listings,
        IReadOnlyList<Asset> Assets,
        IReadOnlyList<Prompt> Prompts,
        IReadOnlyList<Tag> Tags,
        IReadOnlyList<ListingTag> ListingTags,
        IReadOnlyList<AssetLink> AssetLinks,
        IReadOnlyList<Concept>? Concepts = null,
        IReadOnlyList<Design>? Designs = null,
        IReadOnlyList<Mockup>? Mockups = null)
        : this(DefaultWorkspacesFor(Stores), Stores, Niches, Groups, Listings, Assets, Prompts, Tags, ListingTags, AssetLinks, Concepts ?? [], Designs ?? [], Mockups ?? [])
    {
    }

    public static WorkspaceSnapshot Empty { get; } = new(
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        []);

    public static Workspace DefaultWorkspace(DateTimeOffset timestamp) =>
        new(
            WorkspaceDefaults.DefaultWorkspaceId,
            WorkspaceDefaults.DefaultWorkspaceName,
            null,
            false,
            timestamp,
            timestamp,
            "{}");

    private static IReadOnlyList<Workspace> DefaultWorkspacesFor(IReadOnlyList<Store> stores)
    {
        if (stores.Count == 0)
        {
            return [];
        }

        var now = stores.Min(store => store.CreatedAt);
        var workspaceIds = stores.Select(store => store.WorkspaceId).Distinct().ToArray();
        return workspaceIds
            .Select((id, index) => new Workspace(
                id,
                id == WorkspaceDefaults.DefaultWorkspaceId ? WorkspaceDefaults.DefaultWorkspaceName : $"Workspace {index + 1}",
                null,
                false,
                now,
                now,
                "{}"))
            .ToArray();
    }
}
