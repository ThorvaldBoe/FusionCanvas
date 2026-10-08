using System.Text.Json;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.Items;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Listings;

public sealed record ManualListingDetailsState(
    Guid ItemId,
    string Title,
    string Description,
    string? CurrencyCode,
    Guid? OfferingId,
    string? OfferingName,
    IReadOnlyList<ManualListingVariant> Variants,
    IReadOnlyList<ItemVariantListingTerms> VariantTerms,
    ItemListingDetails? Details,
    IReadOnlyList<ItemListingSetupHistory> History,
    bool CanEdit,
    string? ReadOnlyReason);

public sealed record ManualListingVariant(Guid Id, string Name);

public sealed record OfferingMigrationPreview(Guid ItemId, Guid SourceOfferingId, Guid DestinationOfferingId,
    string SourceOfferingName, string DestinationOfferingName, int MatchedVariantCount,
    int UnmatchedDestinationVariantCount, int VariantCount, string ResetSummary);

public sealed class ManualListingDetailsService(IWorkspaceRepository repository, Func<DateTimeOffset>? clock = null, Func<Guid>? newId = null)
{
    private readonly Func<DateTimeOffset> _clock = clock ?? (() => DateTimeOffset.UtcNow);
    private readonly Func<Guid> _newId = newId ?? Guid.NewGuid;

    public async Task<ManualListingDetailsState?> LoadAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var snapshot = await repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var item = snapshot.Items.SingleOrDefault(value => value.Id == itemId);
        return item is null ? null : BuildState(snapshot, item);
    }

    public async Task<ManualListingDetailsState> SaveAsync(Guid itemId, string? title, string? description,
        string? currencyCode, string? shippingOptionName, decimal? customerShippingCharge,
        decimal? expectedSellerShippingCost, string? deliveryEstimate,
        IReadOnlyList<ItemVariantListingTerms> variantTerms, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(variantTerms);
        var snapshot = await repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var item = snapshot.Items.SingleOrDefault(value => value.Id == itemId) ?? throw new InvalidOperationException("Item was not found.");
        var decision = ItemWorkflowPolicy.CanPerformOperation(item, ItemOperationKind.StageContent);
        if (!decision.IsAllowed) throw new InvalidOperationException(decision.Reason);
        var selectedOffering = snapshot.ItemListingConfigurations.SingleOrDefault(value => value.ItemId == itemId)?.OfferingId;
        if (selectedOffering is null
            && (variantTerms.Count > 0 || !string.IsNullOrWhiteSpace(shippingOptionName)
                || customerShippingCharge is not null || expectedSellerShippingCost is not null
                || !string.IsNullOrWhiteSpace(deliveryEstimate)))
            throw new InvalidOperationException("Select an Offering in Design before recording Variant pricing or shipping terms.");
        var prior = snapshot.ItemListingDetails.SingleOrDefault(value => value.ItemId == itemId);
        var details = new ItemListingDetails(itemId, selectedOffering, title, description, currencyCode,
            shippingOptionName, customerShippingCharge, expectedSellerShippingCost, deliveryEstimate);
        var validVariants = VariantsFor(snapshot, selectedOffering).Select(value => value.Id).ToHashSet();
        if (variantTerms.Any(value => value.ItemId != itemId || !validVariants.Contains(value.VariantId)))
            throw new InvalidOperationException("Variant prices must belong to the Item's active Offering.");
        var updated = snapshot with
        {
            ItemListingDetails = [.. snapshot.ItemListingDetails.Where(value => value.ItemId != itemId), details],
            ItemVariantListingTerms = [.. snapshot.ItemVariantListingTerms.Where(value => value.ItemId != itemId), .. variantTerms]
        };
        await repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        return BuildState(updated, item) with { Details = details, VariantTerms = variantTerms.ToArray() };
    }

    public OfferingMigrationPreview PreviewMigration(WorkspaceSnapshot snapshot, Guid itemId, Guid destinationOfferingId)
    {
        var item = snapshot.Items.Single(value => value.Id == itemId);
        var details = snapshot.ItemListingDetails.Single(value => value.ItemId == itemId);
        var sourceId = snapshot.ItemListingConfigurations.Single(value => value.ItemId == itemId).OfferingId;
        if (details.OfferingId != sourceId) throw new InvalidOperationException("Listing Details do not match the Item's active fulfillment setup.");
        if (sourceId == destinationOfferingId) throw new InvalidOperationException("Choose a different Offering to migrate.");
        var source = snapshot.BlueprintOfferings.SingleOrDefault(value => value.Id == sourceId && value.StoreId == item.StoreId && !value.IsArchived
            && snapshot.Blueprints.Any(blueprint => blueprint.Id == value.BlueprintId && blueprint.StoreId == item.StoreId && !blueprint.IsArchived));
        var destination = snapshot.BlueprintOfferings.SingleOrDefault(value => value.Id == destinationOfferingId && value.StoreId == item.StoreId && !value.IsArchived
            && snapshot.Blueprints.Any(blueprint => blueprint.Id == value.BlueprintId && blueprint.StoreId == item.StoreId && !blueprint.IsArchived));
        var storeProductIds = snapshot.StoreProducts.Where(value => value.StoreId == item.StoreId).Select(value => value.Id).ToHashSet();
        var sourceLegacy = snapshot.FulfillmentOfferings.SingleOrDefault(value => value.Id == sourceId && storeProductIds.Contains(value.StoreProductId));
        var destinationLegacy = snapshot.FulfillmentOfferings.SingleOrDefault(value => value.Id == destinationOfferingId && storeProductIds.Contains(value.StoreProductId));
        if ((source is null && sourceLegacy is null) || (destination is null && destinationLegacy is null))
            throw new InvalidOperationException("Both Offerings must be active and belong to this Item's Store.");
        var sourceVariants = snapshot.OfferingVariants.Where(value => value.OfferingId == sourceId && !value.IsArchived).ToArray();
        var destinationVariants = snapshot.OfferingVariants.Where(value => value.OfferingId == destinationOfferingId && !value.IsArchived).ToArray();
        var match = source is not null && destination is not null
            ? VariantMigrationMatcher.MatchExactVariants(sourceVariants, destinationVariants, snapshot.OfferingOptions, snapshot.OfferingOptionValues)
            : new Dictionary<Guid, Guid>();
        var targets = destinationVariants.Length > 0
            ? destinationVariants.Length
            : snapshot.ProductVariants.Count(value => value.FulfillmentOfferingId == destinationOfferingId);
        return new OfferingMigrationPreview(itemId, sourceId, destinationOfferingId, source?.Name ?? sourceLegacy!.Name, destination?.Name ?? destinationLegacy!.Name,
            match.Count, targets - match.Count, targets,
            "Selected Colors, Design Variant rows and colors, artwork slot assignments, and Offering-specific artwork-target preferences will be cleared.");
    }

    public async Task<ManualListingDetailsState> ConfirmMigrationAsync(OfferingMigrationPreview preview, CancellationToken cancellationToken = default)
    {
        var snapshot = await repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var item = snapshot.Items.Single(value => value.Id == preview.ItemId);
        var details = snapshot.ItemListingDetails.Single(value => value.ItemId == item.Id);
        var sourceConfiguration = snapshot.ItemListingConfigurations.Single(value => value.ItemId == item.Id);
        if (sourceConfiguration.OfferingId != preview.SourceOfferingId)
            throw new InvalidOperationException("The Item's Offering changed while the migration review was open. Reload and review again.");
        var canonical = PreviewMigration(snapshot, item.Id, preview.DestinationOfferingId);
        var sourceTerms = snapshot.ItemVariantListingTerms.Where(value => value.ItemId == item.Id).ToArray();
        var history = new ItemListingSetupHistory(_newId(), item.Id, details.OfferingId, canonical.SourceOfferingName,
            details.Title, details.Description, details.CurrencyCode, details.ShippingOptionName,
            details.CustomerShippingCharge, details.ExpectedSellerShippingCost, details.DeliveryEstimate,
            JsonSerializer.Serialize(sourceTerms), _clock());
        var canMatch = snapshot.BlueprintOfferings.Any(value => value.Id == preview.SourceOfferingId && !value.IsArchived)
            && snapshot.BlueprintOfferings.Any(value => value.Id == preview.DestinationOfferingId && !value.IsArchived);
        var matches = canMatch
            ? VariantMigrationMatcher.MatchExactVariants(
                snapshot.OfferingVariants.Where(value => value.OfferingId == preview.SourceOfferingId).ToArray(),
                snapshot.OfferingVariants.Where(value => value.OfferingId == preview.DestinationOfferingId).ToArray(),
                snapshot.OfferingOptions, snapshot.OfferingOptionValues)
            : new Dictionary<Guid, Guid>();
        var termsBySource = sourceTerms.ToDictionary(value => value.VariantId);
        var destinationTerms = matches.Select(pair => new ItemVariantListingTerms(item.Id, pair.Key,
            termsBySource.GetValueOrDefault(pair.Value)?.SellingPrice,
            termsBySource.GetValueOrDefault(pair.Value)?.ExpectedFulfillmentCost)).ToArray();
        var migratedDetails = new ItemListingDetails(item.Id, preview.DestinationOfferingId, details.Title,
            details.Description, details.CurrencyCode);
        var updated = ReplaceDesignConfiguration(snapshot, item, preview.DestinationOfferingId) with
        {
            ItemListingDetails = [.. snapshot.ItemListingDetails.Where(value => value.ItemId != item.Id), migratedDetails],
            ItemVariantListingTerms = [.. snapshot.ItemVariantListingTerms.Where(value => value.ItemId != item.Id), .. destinationTerms],
            ItemListingSetupHistory = [.. snapshot.ItemListingSetupHistory, history]
        };
        await repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        return BuildState(updated, updated.Items.Single(value => value.Id == item.Id));
    }

    private ManualListingDetailsState BuildState(WorkspaceSnapshot snapshot, Item item)
    {
        var details = snapshot.ItemListingDetails.SingleOrDefault(value => value.ItemId == item.Id);
        var configuration = snapshot.ItemListingConfigurations.SingleOrDefault(value => value.ItemId == item.Id);
        var offeringName = configuration is null ? null
            : snapshot.BlueprintOfferings.SingleOrDefault(value => value.Id == configuration.OfferingId)?.Name
                ?? snapshot.FulfillmentOfferings.SingleOrDefault(value => value.Id == configuration.OfferingId)?.Name;
        var decision = ItemWorkflowPolicy.CanPerformOperation(item, ItemOperationKind.StageContent);
        return new ManualListingDetailsState(item.Id, details is null ? item.Name : details.Title ?? string.Empty,
            details is null ? item.Description ?? string.Empty : details.Description ?? string.Empty,
            details?.CurrencyCode, configuration?.OfferingId, offeringName, VariantsFor(snapshot, configuration?.OfferingId),
            snapshot.ItemVariantListingTerms.Where(value => value.ItemId == item.Id).ToArray(), details,
            snapshot.ItemListingSetupHistory.Where(value => value.ItemId == item.Id).OrderByDescending(value => value.ArchivedAt).ToArray(),
            decision.IsAllowed, decision.IsAllowed ? null : decision.Reason);
    }

    private static IReadOnlyList<ManualListingVariant> VariantsFor(WorkspaceSnapshot snapshot, Guid? offeringId)
    {
        if (offeringId is null) return [];
        var normalized = snapshot.OfferingVariants.Where(value => value.OfferingId == offeringId && !value.IsArchived)
            .OrderBy(value => value.Name).Select(value => new ManualListingVariant(value.Id, value.Name)).ToArray();
        if (normalized.Length > 0) return normalized;
        return snapshot.ProductVariants.Where(value => value.FulfillmentOfferingId == offeringId).OrderBy(value => value.Id)
            .Select(value => new ManualListingVariant(value.Id, string.Join(", ", value.Options.Select(option => $"{option.Name}: {option.Value}"))))
            .ToArray();
    }

    private WorkspaceSnapshot ReplaceDesignConfiguration(WorkspaceSnapshot snapshot, Item item, Guid offeringId)
    {
        var oldRows = snapshot.DesignVariantRows.Where(row => row.ItemId == item.Id).Select(row => row.Id).ToHashSet();
        var metadata = ItemMetadataCodec.ParseMetadata(item.MetadataJson);
        metadata.Remove(ItemMetadataCodec.ArtworkTargetIdKey);
        metadata.Remove(ItemMetadataCodec.ArtworkTargetPreferenceKey);
        metadata.Remove(ItemMetadataCodec.ArtworkTransparentBackgroundKey);
        var changedItem = item with { MetadataJson = ItemMetadataCodec.SerializeMetadata(metadata), UpdatedAt = _clock() };
        return snapshot with
        {
            Items = [.. snapshot.Items.Where(value => value.Id != item.Id), changedItem],
            ItemListingConfigurations = [.. snapshot.ItemListingConfigurations.Where(value => value.ItemId != item.Id), new ItemListingConfiguration(item.Id, offeringId)],
            DesignSelectedColors = [.. snapshot.DesignSelectedColors.Where(value => value.ItemId != item.Id)],
            DesignVariantRows = [.. snapshot.DesignVariantRows.Where(value => value.ItemId != item.Id)],
            DesignVariantRowColors = [.. snapshot.DesignVariantRowColors.Where(value => !oldRows.Contains(value.RowId))],
            DesignSlotAssignments = [.. snapshot.DesignSlotAssignments.Where(value => !oldRows.Contains(value.RowId))]
        };
    }
}
