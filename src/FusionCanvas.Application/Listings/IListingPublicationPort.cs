namespace FusionCanvas.Application.Listings;

public interface IListingPublicationPort
{
    Task<ListingMutationResult> PublishAsync(string shopId, string productId, CancellationToken cancellationToken = default);
    Task<ListingMutationResult> UnpublishAsync(string shopId, string productId, CancellationToken cancellationToken = default);
}
