namespace FusionCanvas.Application.Listings;

public interface IListingProductPort
{
    Task<ListingRemoteProduct?> GetAsync(string shopId, string productId, CancellationToken cancellationToken = default);
    Task<ListingMutationResult> CreateAsync(string shopId, ListingProductProjection projection, IReadOnlyDictionary<Guid, ListingImageReference> images, CancellationToken cancellationToken = default);
    Task<ListingMutationResult> UpdateAsync(string shopId, string productId, ListingProductProjection projection, IReadOnlyDictionary<Guid, ListingImageReference> images, CancellationToken cancellationToken = default);
    Task<ListingMutationResult> DeleteAsync(string shopId, string productId, CancellationToken cancellationToken = default);
}
