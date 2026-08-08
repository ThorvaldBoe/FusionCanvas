using FusionCanvas.Domain.Listings;

namespace FusionCanvas.Application.Listings;

public interface IListingPreparationService
{
    Task<ListingPreparationState?> LoadAsync(Guid itemId, CancellationToken cancellationToken = default);

    Task<ListingPreparationResult> UpdateAsync(UpdateListingPreparationRequest request, CancellationToken cancellationToken = default);

    Task<ListingPreparationResult> BindShopifyAsync(BindShopifyListingRequest request, CancellationToken cancellationToken = default);
}
