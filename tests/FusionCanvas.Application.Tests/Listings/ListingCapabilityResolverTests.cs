using FusionCanvas.Application.Listings;
using FusionCanvas.Domain.Stores;

namespace FusionCanvas.Application.Tests.Listings;

public sealed class ListingCapabilityResolverTests
{
    [Theory]
    [InlineData(FulfillmentStrategy.Manual, false, false)]
    [InlineData(FulfillmentStrategy.ShopifyManual, false, false)]
    [InlineData(FulfillmentStrategy.Printify, true, true)]
    [InlineData(FulfillmentStrategy.ShopifyPrintify, true, true)]
    public void Strategy_controls_product_and_publication_capabilities(
        FulfillmentStrategy strategy,
        bool products,
        bool publication)
    {
        var capabilities = ListingCapabilityResolver.Resolve(
            strategy,
            new ListingReadiness(ListingConnectionState.Ready, true, true, []));

        Assert.Equal(products, capabilities.ProductOperationsAvailable);
        Assert.Equal(publication, capabilities.PublicationOperationsAvailable);
    }

    [Fact]
    public void Unavailable_readiness_disables_all_remote_capabilities()
    {
        var capabilities = ListingCapabilityResolver.Resolve(
            FulfillmentStrategy.ShopifyPrintify,
            ListingReadiness.Unavailable("offline", "Printify is unavailable."));

        Assert.False(capabilities.ProductOperationsAvailable);
        Assert.False(capabilities.PublicationOperationsAvailable);
    }

    [Fact]
    public void Standalone_printify_can_publish_when_the_shop_has_a_connected_channel()
    {
        var capabilities = ListingCapabilityResolver.Resolve(
            FulfillmentStrategy.Printify,
            new ListingReadiness(ListingConnectionState.Ready, true, true, []));

        Assert.True(capabilities.ProductOperationsAvailable);
        Assert.True(capabilities.PublicationOperationsAvailable);
    }

    [Fact]
    public void Disconnected_printify_shop_disables_publication_but_keeps_product_operations()
    {
        var capabilities = ListingCapabilityResolver.Resolve(
            FulfillmentStrategy.Printify,
            new ListingReadiness(ListingConnectionState.Ready, true, false,
                [new ListingReadinessIssue("printify-sales-channel-unavailable", "Connect a channel.")]));

        Assert.True(capabilities.ProductOperationsAvailable);
        Assert.False(capabilities.PublicationOperationsAvailable);
    }
}
