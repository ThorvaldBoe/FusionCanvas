namespace FusionCanvas.Domain.Stores;

public static class FulfillmentStrategyPolicy
{
    public static IReadOnlyList<FulfillmentStrategy> AvailableStrategies { get; } =
        [FulfillmentStrategy.Manual];

    public static bool IsAvailable(FulfillmentStrategy strategy) =>
        strategy == FulfillmentStrategy.Manual;

    public static bool AllowsExternalCommunication(FulfillmentStrategy strategy) =>
        strategy != FulfillmentStrategy.Manual;
}
