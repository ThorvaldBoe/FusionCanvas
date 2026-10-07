using FusionCanvas.Domain.Stores;

namespace FusionCanvas.Application.Listings;

public static class ListingCapabilityResolver
{
    public static ListingStrategyCapabilities Resolve(
        FulfillmentStrategy strategy,
        ListingReadiness readiness)
    {
        ArgumentNullException.ThrowIfNull(readiness);

        var printifyEnabled = strategy is FulfillmentStrategy.Printify or FulfillmentStrategy.ShopifyPrintify;
        var publicationEnabled = strategy == FulfillmentStrategy.ShopifyPrintify;
        return new(
            printifyEnabled && readiness.ProductOperationsAvailable,
            publicationEnabled && readiness.PublicationOperationsAvailable);
    }
}
