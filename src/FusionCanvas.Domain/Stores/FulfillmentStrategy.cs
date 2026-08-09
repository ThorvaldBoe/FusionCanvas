namespace FusionCanvas.Domain.Stores;

public enum FulfillmentStrategy
{
    Manual = 0,
    ShopifyManual = 1,
    ShopifyPrintify = 2
}

public static class FulfillmentStrategyPolicy
{
    public static IReadOnlyList<FulfillmentStrategy> AvailableStrategies { get; } =
        [FulfillmentStrategy.Manual];

    public static bool IsAvailable(FulfillmentStrategy strategy) =>
        strategy == FulfillmentStrategy.Manual;

    public static bool AllowsExternalCommunication(FulfillmentStrategy strategy) =>
        strategy != FulfillmentStrategy.Manual;
}
