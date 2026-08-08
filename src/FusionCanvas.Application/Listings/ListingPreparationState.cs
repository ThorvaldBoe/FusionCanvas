using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Listings;

namespace FusionCanvas.Application.Listings;

public sealed record ListingPreparationState(
    Item Item,
    ItemListingProfile Profile,
    IReadOnlyList<ListingProviderState> Providers,
    IReadOnlyList<Guid> TagIds,
    IReadOnlyList<Guid> MediaAssetIds,
    bool CanEdit,
    string ReadOnlyReason)
{
    public bool ShopifyActionsEnabled =>
        Profile.Strategy != ListingFulfillmentStrategy.Manual
        && Providers.Any(provider => string.Equals(provider.Provider, "Shopify", StringComparison.OrdinalIgnoreCase));

    public bool RequiresShopifyBinding =>
        Profile.Strategy == ListingFulfillmentStrategy.ShopifyManual && !ShopifyActionsEnabled;

    public bool PrintifyLocked =>
        Profile.Strategy == ListingFulfillmentStrategy.ShopifyPrintify
        && Providers.Any(provider => string.Equals(provider.Provider, "Shopify", StringComparison.OrdinalIgnoreCase) && provider.IsLocked);
}
