namespace FusionCanvas.Domain.Stores;

public static class FulfillmentStrategyPolicy
{
    public static IReadOnlyList<FulfillmentStrategy> AvailableStrategies { get; } =
        [FulfillmentStrategy.Manual, FulfillmentStrategy.ShopifyManual, FulfillmentStrategy.ShopifyPrintify, FulfillmentStrategy.Printify];

    public static bool IsAvailable(FulfillmentStrategy strategy) =>
        strategy is FulfillmentStrategy.Manual or FulfillmentStrategy.ShopifyManual or FulfillmentStrategy.ShopifyPrintify or FulfillmentStrategy.Printify;

    public static bool RequiresPrintifyKey(FulfillmentStrategy strategy) =>
        strategy is FulfillmentStrategy.ShopifyPrintify or FulfillmentStrategy.Printify;

    public static bool AllowsExternalCommunication(FulfillmentStrategy strategy) =>
        strategy != FulfillmentStrategy.Manual;
}
