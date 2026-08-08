using FusionCanvas.Domain.Listings;

namespace FusionCanvas.Domain.Tests.Listings;

public sealed class ListingModelTests
{
    [Fact]
    public void Profile_NormalizesCurrencyAndReferences()
    {
        var itemId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var profile = new ItemListingProfile(
            itemId,
            ListingFulfillmentStrategy.ShopifyManual,
            12.50m,
            " usd ",
            ListingReadinessState.Ready,
            mediaAssetIds: [mediaId, mediaId, Guid.Empty]);

        Assert.Equal("USD", profile.Currency);
        Assert.Equal([mediaId], profile.MediaAssetIds);
        Assert.Equal(ListingReadinessState.Ready, profile.Readiness);
    }

    [Fact]
    public void Profile_RejectsNegativePriceAndPublishedDraft()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ItemListingProfile(Guid.NewGuid(), price: -1));
        Assert.Throws<ArgumentException>(() => new ItemListingProfile(
            Guid.NewGuid(),
            readiness: ListingReadinessState.Draft,
            publication: ListingPublicationState.Published));
    }

    [Fact]
    public void ProviderState_RetainsDiagnosticsAndExternalIdentity()
    {
        var state = new ListingProviderState(
            Guid.NewGuid(),
            " Shopify ",
            "Online Store",
            "gid://shopify/Product/42",
            ListingSyncStatus.Conflict,
            lastResult: "partial",
            errorMessage: "Price differs",
            conflictDetails: "local override wins",
            isLocked: true);

        Assert.Equal("Shopify", state.Provider);
        Assert.Equal(ListingSyncStatus.Conflict, state.SyncStatus);
        Assert.Equal("gid://shopify/Product/42", state.ExternalId);
        Assert.True(state.IsLocked);
        Assert.Equal("Price differs", state.ErrorMessage);
    }
}
