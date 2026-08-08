using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Listings;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Listings;

public sealed class ListingPreparationService(IWorkspaceRepository repository) : IListingPreparationService
{
    public async Task<ListingPreparationState?> LoadAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var snapshot = await repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return BuildState(snapshot, itemId);
    }

    public async Task<ListingPreparationResult> UpdateAsync(UpdateListingPreparationRequest request, CancellationToken cancellationToken = default)
    {
        var snapshot = await repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var current = BuildState(snapshot, request.ItemId);
        if (current is null)
        {
            return ListingPreparationResult.Failure("The selected Item no longer exists.");
        }

        if (!current.CanEdit)
        {
            return ListingPreparationResult.Failure(current.ReadOnlyReason, current);
        }

        if (request.Strategy == ListingFulfillmentStrategy.ShopifyPrintify && request.Publication == ListingPublicationState.Published && request.ProviderState is null)
        {
            return ListingPreparationResult.Failure("A published Shopify plus Printify listing requires a confirmed provider identity.", current);
        }

        ItemListingProfile profile;
        try
        {
            profile = new ItemListingProfile(
                request.ItemId,
                request.Strategy,
                request.Price,
                request.Currency,
                request.Readiness,
                request.Publication,
                request.MediaAssetIds,
                request.VariantIds,
                request.SharedMetadataJson,
                request.FieldOwnershipJson,
                DateTimeOffset.UtcNow);
            ValidateReferences(snapshot, current.Item, profile);
        }
        catch (ArgumentException exception)
        {
            return ListingPreparationResult.Failure(exception.Message, current);
        }
        catch (InvalidOperationException exception)
        {
            return ListingPreparationResult.Failure(exception.Message, current);
        }

        var profiles = snapshot.ItemListingProfiles.Where(candidate => candidate.ItemId != request.ItemId).Append(profile).ToArray();
        var providers = snapshot.ListingProviderStates
            .Where(candidate => candidate.ItemId != request.ItemId)
            .Concat(request.ProviderState is null
                ? snapshot.ListingProviderStates.Where(candidate => candidate.ItemId == request.ItemId)
                : [request.ProviderState])
            .ToArray();
        var updated = snapshot with { ItemListingProfiles = profiles, ListingProviderStates = providers };

        try
        {
            await repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return ListingPreparationResult.Failure($"Listing preparation could not be saved: {exception.Message}", current);
        }

        return ListingPreparationResult.Success(BuildState(updated, request.ItemId)!);
    }

    public async Task<ListingPreparationResult> BindShopifyAsync(BindShopifyListingRequest request, CancellationToken cancellationToken = default)
    {
        var current = await LoadAsync(request.ItemId, cancellationToken).ConfigureAwait(false);
        if (current is null)
        {
            return ListingPreparationResult.Failure("The selected Item no longer exists.");
        }

        if (!current.CanEdit)
        {
            return ListingPreparationResult.Failure(current.ReadOnlyReason, current);
        }

        if (string.IsNullOrWhiteSpace(request.ExternalId) || string.IsNullOrWhiteSpace(request.Channel))
        {
            return ListingPreparationResult.Failure("A Shopify item ID and publication channel are required.", current);
        }

        var provider = new ListingProviderState(
            request.ItemId,
            "Shopify",
            request.Channel,
            request.ExternalId,
            ListingSyncStatus.NotConnected,
            isLocked: request.FromPrintifyPublication);
        return await UpdateAsync(new(
            request.ItemId,
            request.FromPrintifyPublication ? ListingFulfillmentStrategy.ShopifyPrintify : ListingFulfillmentStrategy.ShopifyManual,
            current.Profile.Price,
            current.Profile.Currency,
            current.Profile.Readiness,
            request.FromPrintifyPublication ? ListingPublicationState.Published : current.Profile.Publication,
            current.Profile.MediaAssetIds,
            current.Profile.VariantIds,
            current.Profile.SharedMetadataJson,
            current.Profile.FieldOwnershipJson,
            provider), cancellationToken).ConfigureAwait(false);
    }

    private static ListingPreparationState? BuildState(WorkspaceSnapshot snapshot, Guid itemId)
    {
        var item = snapshot.Items.SingleOrDefault(candidate => candidate.Id == itemId);
        if (item is null)
        {
            return null;
        }

        var profile = snapshot.ItemListingProfiles.SingleOrDefault(candidate => candidate.ItemId == itemId)
            ?? new ItemListingProfile(itemId, updatedAt: item.UpdatedAt);
        var providers = snapshot.ListingProviderStates.Where(candidate => candidate.ItemId == itemId).ToArray();
        var inactive = !ItemHierarchy.IsEffectivelyActive(snapshot, item);
        var canEdit = !inactive && item.Status is not (ItemStatus.Published or ItemStatus.Rejected);
        var reason = inactive
            ? "Restore the Item and its parent path before editing listing preparation."
            : canEdit ? string.Empty : "Pause or reactivate the Item before editing listing preparation.";
        var tagIds = snapshot.ItemTags.Where(link => link.ItemId == itemId).Select(link => link.TagId).ToArray();
        var mediaIds = snapshot.AssetLinks
            .Where(link => link.EntityKind == WorkspaceEntityKind.Item && link.EntityId == itemId)
            .Select(link => link.AssetId)
            .ToArray();
        return new ListingPreparationState(item, profile, providers, tagIds, mediaIds, canEdit, reason);
    }

    private static void ValidateReferences(WorkspaceSnapshot snapshot, Item item, ItemListingProfile profile)
    {
        var assets = snapshot.Assets.Where(asset => profile.MediaAssetIds.Contains(asset.Id)).ToArray();
        if (assets.Any(asset => asset.StoreId != item.StoreId))
        {
            throw new InvalidOperationException("Listing media must belong to the Item's Store.");
        }

        var variants = snapshot.ProductVariants.Where(variant => profile.VariantIds.Contains(variant.Id)).ToArray();
        var offeringIds = variants.Select(variant => variant.FulfillmentOfferingId).ToHashSet();
        var productIds = snapshot.FulfillmentOfferings
            .Where(offering => offeringIds.Contains(offering.Id))
            .Join(snapshot.StoreProducts, offering => offering.StoreProductId, product => product.Id, (_, product) => product)
            .Select(product => product.StoreId)
            .ToArray();
        if (variants.Length != profile.VariantIds.Count || productIds.Any(storeId => storeId != item.StoreId))
        {
            throw new InvalidOperationException("Listing variants must belong to a product offering in the Item's Store.");
        }
    }
}
