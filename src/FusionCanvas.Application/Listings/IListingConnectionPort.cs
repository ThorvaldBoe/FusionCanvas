namespace FusionCanvas.Application.Listings;

public interface IListingConnectionPort
{
    Task<ListingReadiness> CheckAsync(ListingConnectionRequest request, CancellationToken cancellationToken = default);
}
