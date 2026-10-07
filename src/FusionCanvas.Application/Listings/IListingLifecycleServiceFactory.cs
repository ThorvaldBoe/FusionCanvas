using FusionCanvas.Domain.Stores;

namespace FusionCanvas.Application.Listings;

public interface IListingLifecycleServiceFactory
{
    ListingLifecycleService Create(Store store);
}
