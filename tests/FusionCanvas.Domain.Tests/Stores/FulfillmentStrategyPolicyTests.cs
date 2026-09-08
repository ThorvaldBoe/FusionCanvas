using FusionCanvas.Domain.Stores;

namespace FusionCanvas.Domain.Tests.Stores;

public class FulfillmentStrategyPolicyTests
{
    [Theory]
    [InlineData(FulfillmentStrategy.Manual, false)]
    [InlineData(FulfillmentStrategy.ShopifyManual, false)]
    [InlineData(FulfillmentStrategy.ShopifyPrintify, true)]
    [InlineData(FulfillmentStrategy.Printify, true)]
    public void SupportedStrategies_PrintifyStrategiesRequireKey(FulfillmentStrategy strategy, bool requiresKey)
    {
        Assert.True(FulfillmentStrategyPolicy.IsAvailable(strategy));
        Assert.Equal(requiresKey, FulfillmentStrategyPolicy.RequiresPrintifyKey(strategy));
        Assert.Equal(4, FulfillmentStrategyPolicy.AvailableStrategies.Count);
    }

    [Fact]
    public void UndefinedValue_IsNotAvailable() => Assert.False(FulfillmentStrategyPolicy.IsAvailable((FulfillmentStrategy)4));
}
