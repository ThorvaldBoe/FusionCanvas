using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Tags;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Prompts;

namespace FusionCanvas.Domain.Workspace;

public sealed record WorkspaceSnapshot(
    IReadOnlyList<Workspace> Workspaces,
    IReadOnlyList<Store> Stores,
    IReadOnlyList<Niche> Niches,
    IReadOnlyList<TopicGroup> Groups,
    IReadOnlyList<Item> Items,
    IReadOnlyList<Asset> Assets,
    IReadOnlyList<Prompt> Prompts,
    IReadOnlyList<Tag> Tags,
    IReadOnlyList<ItemTag> ItemTags,
    IReadOnlyList<AssetLink> AssetLinks)
{
    public WorkspaceSnapshot(
        IReadOnlyList<Store> Stores,
        IReadOnlyList<Niche> Niches,
        IReadOnlyList<TopicGroup> Groups,
        IReadOnlyList<Item> Items,
        IReadOnlyList<Asset> Assets,
        IReadOnlyList<Prompt> Prompts,
        IReadOnlyList<Tag> Tags,
        IReadOnlyList<ItemTag> ItemTags,
        IReadOnlyList<AssetLink> AssetLinks)
        : this(DefaultWorkspacesFor(Stores), Stores, Niches, Groups, Items, Assets, Prompts, Tags, ItemTags, AssetLinks)
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
