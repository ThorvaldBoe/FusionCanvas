using FusionCanvas.Domain.Assets;

namespace FusionCanvas.Domain.Workspace.Transfer;

public static class WorkspaceSnapshotFilter
{
    public static WorkspaceSnapshotFilterResult ForWorkspace(WorkspaceSnapshot source, Guid workspaceId)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("Workspace identifier must not be empty.", nameof(workspaceId));
        }

        var workspaces = source.Workspaces.Where(workspace => workspace.Id == workspaceId).ToArray();
        var stores = source.Stores.Where(store => store.WorkspaceId == workspaceId).ToArray();
        var storeIds = stores.Select(store => store.Id).ToHashSet();
        var niches = source.Niches.Where(niche => storeIds.Contains(niche.StoreId)).ToArray();
        var groups = source.Groups.Where(group => storeIds.Contains(group.StoreId)).ToArray();
        var nicheIds = niches.Select(niche => niche.Id).ToHashSet();
        var groupIds = groups.Select(group => group.Id).ToHashSet();
        var items = source.Items.Where(item => storeIds.Contains(item.StoreId)).ToArray();
        var assets = source.Assets.Where(asset => storeIds.Contains(asset.StoreId)).ToArray();
        var prompts = source.Prompts.Where(prompt => storeIds.Contains(prompt.StoreId)).ToArray();
        var tags = source.Tags.Where(tag => storeIds.Contains(tag.StoreId)).ToArray();

        var itemIds = items.Select(item => item.Id).ToHashSet();
        var assetIds = assets.Select(asset => asset.Id).ToHashSet();
        var tagIds = tags.Select(tag => tag.Id).ToHashSet();
        var itemTags = source.ItemTags
            .Where(itemTag => itemIds.Contains(itemTag.ItemId) && tagIds.Contains(itemTag.TagId))
            .ToArray();

        var includedTargetIds = BuildIncludedTargetIds(
            stores.Select(store => store.Id),
            niches.Select(niche => niche.Id),
            groups.Select(group => group.Id),
            items.Select(item => item.Id),
            assets.Select(asset => asset.Id),
            prompts.Select(prompt => prompt.Id));
        var includedLinks = source.AssetLinks
            .Where(link => assetIds.Contains(link.AssetId) && IsTargetIncluded(link, includedTargetIds))
            .ToArray();
        var droppedLinks = source.AssetLinks
            .Where(link => assetIds.Contains(link.AssetId) && !IsTargetIncluded(link, includedTargetIds))
            .ToArray();
        var ideationRejections = source.IdeationRejections
            .Where(rejection =>
                storeIds.Contains(rejection.StoreId) &&
                nicheIds.Contains(rejection.NicheId) &&
                (rejection.GroupId is null || groupIds.Contains(rejection.GroupId.Value)))
            .ToArray();

        var storeProducts = source.StoreProducts.Where(value => storeIds.Contains(value.StoreId)).ToArray();
        var storeProductIds = storeProducts.Select(value => value.Id).ToHashSet();
        var fulfillmentOfferings = source.FulfillmentOfferings.Where(value => storeProductIds.Contains(value.StoreProductId)).ToArray();
        var fulfillmentOfferingIds = fulfillmentOfferings.Select(value => value.Id).ToHashSet();
        var productVariants = source.ProductVariants.Where(value => fulfillmentOfferingIds.Contains(value.FulfillmentOfferingId)).ToArray();
        var designAreas = source.DesignAreas.Where(value => fulfillmentOfferingIds.Contains(value.FulfillmentOfferingId)).ToArray();
        var designAreaIds = designAreas.Select(value => value.Id).ToHashSet();
        var itemListingConfigurations = source.ItemListingConfigurations.Where(value => itemIds.Contains(value.ItemId) && fulfillmentOfferingIds.Contains(value.OfferingId)).ToArray();
        var designSelectedColors = source.DesignSelectedColors.Where(value => itemIds.Contains(value.ItemId)).ToArray();
        var designVariantRows = source.DesignVariantRows.Where(value => itemIds.Contains(value.ItemId)).ToArray();
        var designVariantRowIds = designVariantRows.Select(value => value.Id).ToHashSet();
        var designVariantRowColors = source.DesignVariantRowColors.Where(value => designVariantRowIds.Contains(value.RowId)).ToArray();
        var designSlotAssignments = source.DesignSlotAssignments.Where(value => designVariantRowIds.Contains(value.RowId) && designAreaIds.Contains(value.DesignAreaId)).ToArray();

        var blueprints = source.Blueprints.Where(value => storeIds.Contains(value.StoreId)).ToArray();
        var blueprintIds = blueprints.Select(value => value.Id).ToHashSet();
        var printProviders = source.PrintProviders.Where(value => storeIds.Contains(value.StoreId)).ToArray();
        var blueprintOfferings = source.BlueprintOfferings.Where(value => storeIds.Contains(value.StoreId) && blueprintIds.Contains(value.BlueprintId)).ToArray();
        var blueprintOfferingIds = blueprintOfferings.Select(value => value.Id).ToHashSet();
        var offeringOptions = source.OfferingOptions.Where(value => blueprintOfferingIds.Contains(value.OfferingId)).ToArray();
        var offeringOptionValues = source.OfferingOptionValues.Where(value => blueprintOfferingIds.Contains(value.OfferingId)).ToArray();
        var offeringVariants = source.OfferingVariants.Where(value => blueprintOfferingIds.Contains(value.OfferingId)).ToArray();
        var offeringPlaceholders = source.OfferingPlaceholders.Where(value => blueprintOfferingIds.Contains(value.OfferingId)).ToArray();
        var mockupTemplates = source.MockupTemplates.Where(value => blueprintOfferingIds.Contains(value.BlueprintOfferingId)).ToArray();
        var mockupTemplateIds = mockupTemplates.Select(value => value.Id).ToHashSet();
        var mockupTemplateColors = source.MockupTemplateColorVariants.Where(value => mockupTemplateIds.Contains(value.MockupTemplateId)).ToArray();
        var mockupTemplateRevisions = source.MockupTemplateRevisions.Where(value => mockupTemplateIds.Contains(value.MockupTemplateId)).ToArray();
        var mockupTemplateRevisionIds = mockupTemplateRevisions.Select(value => value.Id).ToHashSet();
        var mockupTemplateRevisionColors = source.MockupTemplateRevisionColors.Where(value => mockupTemplateRevisionIds.Contains(value.RevisionId)).ToArray();

        return new WorkspaceSnapshotFilterResult(
            new WorkspaceSnapshot(
                workspaces,
                stores,
                niches,
                groups,
                items,
                assets,
                prompts,
                tags,
                itemTags,
                includedLinks)
            {
                IdeationRejections = ideationRejections,
                StoreProducts = storeProducts,
                FulfillmentOfferings = fulfillmentOfferings,
                ProductVariants = productVariants,
                DesignAreas = designAreas,
                ItemListingConfigurations = itemListingConfigurations,
                DesignSelectedColors = designSelectedColors,
                DesignVariantRows = designVariantRows,
                DesignVariantRowColors = designVariantRowColors,
                DesignSlotAssignments = designSlotAssignments,
                Blueprints = blueprints,
                PrintProviders = printProviders,
                BlueprintOfferings = blueprintOfferings,
                OfferingOptions = offeringOptions,
                OfferingOptionValues = offeringOptionValues,
                OfferingVariants = offeringVariants,
                OfferingPlaceholders = offeringPlaceholders,
                MockupTemplates = mockupTemplates,
                MockupTemplateColorVariants = mockupTemplateColors,
                MockupTemplateRevisions = mockupTemplateRevisions,
                MockupTemplateRevisionColors = mockupTemplateRevisionColors
            },
            droppedLinks);
    }

    private static IReadOnlyDictionary<WorkspaceEntityKind, HashSet<Guid>> BuildIncludedTargetIds(
        IEnumerable<Guid> stores,
        IEnumerable<Guid> niches,
        IEnumerable<Guid> groups,
        IEnumerable<Guid> items,
        IEnumerable<Guid> assets,
        IEnumerable<Guid> prompts) =>
        new Dictionary<WorkspaceEntityKind, HashSet<Guid>>
        {
            [WorkspaceEntityKind.Store] = stores.ToHashSet(),
            [WorkspaceEntityKind.Niche] = niches.ToHashSet(),
            [WorkspaceEntityKind.Group] = groups.ToHashSet(),
            [WorkspaceEntityKind.Item] = items.ToHashSet(),
            [WorkspaceEntityKind.Asset] = assets.ToHashSet(),
            [WorkspaceEntityKind.Prompt] = prompts.ToHashSet()
        };

    private static bool IsTargetIncluded(
        AssetLink link,
        IReadOnlyDictionary<WorkspaceEntityKind, HashSet<Guid>> includedTargetIds) =>
        includedTargetIds.TryGetValue(link.EntityKind, out var ids) && ids.Contains(link.EntityId);
}
