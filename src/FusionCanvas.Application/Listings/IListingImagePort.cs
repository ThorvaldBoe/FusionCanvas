namespace FusionCanvas.Application.Listings;

public interface IListingImagePort
{
    Task<ListingImageReference> UploadAsync(ListingImageUpload request, CancellationToken cancellationToken = default);
}
