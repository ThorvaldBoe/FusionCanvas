using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Tags;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Prompts;
using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;
 
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
    public IReadOnlyList<IdeationRejection> IdeationRejections { get; init; } = [];

    public IReadOnlyList<StoreProduct> StoreProducts { get; init; } = [];

    public IReadOnlyList<FulfillmentOffering> FulfillmentOfferings { get; init; } = [];

    public IReadOnlyList<ProductVariant> ProductVariants { get; init; } = [];

    public IReadOnlyList<DesignArea> DesignAreas { get; init; } = [];

    public IReadOnlyList<ItemListingConfiguration> ItemListingConfigurations { get; init; } = [];

    public IReadOnlyList<DesignSelectedColor> DesignSelectedColors { get; init; } = [];

    public IReadOnlyList<DesignVariantRow> DesignVariantRows { get; init; } = [];

    public IReadOnlyList<DesignVariantRowColor> DesignVariantRowColors { get; init; } = [];

    public IReadOnlyList<DesignSlotAssignment> DesignSlotAssignments { get; init; } = [];

    public IReadOnlyList<Blueprint> Blueprints { get; init; } = [];

    public IReadOnlyList<PrintProvider> PrintProviders { get; init; } = [];

    public IReadOnlyList<BlueprintOffering> BlueprintOfferings { get; init; } = [];

    public IReadOnlyList<OfferingOption> OfferingOptions { get; init; } = [];

    public IReadOnlyList<OfferingOptionValue> OfferingOptionValues { get; init; } = [];

    public IReadOnlyList<OfferingVariant> OfferingVariants { get; init; } = [];

    public IReadOnlyList<OfferingPlaceholder> OfferingPlaceholders { get; init; } = [];

    public IReadOnlyList<MockupTemplate> MockupTemplates { get; init; } = [];

    public IReadOnlyList<MockupTemplateColorVariant> MockupTemplateColorVariants { get; init; } = [];

    public IReadOnlyList<MockupTemplateRevision> MockupTemplateRevisions { get; init; } = [];

    public IReadOnlyList<MockupTemplateRevisionColor> MockupTemplateRevisionColors { get; init; } = [];

    public IReadOnlyList<MockupTemplateSourceImage> MockupTemplateSourceImages { get; init; } = [];

    public IReadOnlyList<MockupTemplateSourceImageOptionValue> MockupTemplateSourceImageOptionValues { get; init; } = [];

    public IReadOnlyList<MockupTemplateRevisionSourceImage> MockupTemplateRevisionSourceImages { get; init; } = [];

    public IReadOnlyList<MockupTemplateRevisionSourceImageOptionValue> MockupTemplateRevisionSourceImageOptionValues { get; init; } = [];

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
