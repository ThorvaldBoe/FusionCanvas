using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Listings;

public interface IListingProjectionSource
{
    Task<ListingProjectionResult> BuildAsync(
        WorkspaceSnapshot snapshot,
        Guid itemId,
        ListingPricingInput pricing,
        string? shippingProfile,
        string? outOfStockPolicy,
        CancellationToken cancellationToken = default);
}
