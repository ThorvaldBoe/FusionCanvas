using FusionCanvas.Application.Listings;
using FusionCanvas.Application.Stores;
using FusionCanvas.Application.Stores.Printify;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Stores;

namespace FusionCanvas.Integration.Stores.Printify;

public sealed class PrintifyListingLifecycleServiceFactory(
    IWorkspaceRepository repository,
    IStorePrintifyCredentialStore credentials,
    IWorkspaceFileReader fileReader,
    HttpClient client) : IListingLifecycleServiceFactory
{
    public ListingLifecycleService Create(Store store)
    {
        ArgumentNullException.ThrowIfNull(store);
        var scopedStore = store;
        var listingClient = new PrintifyListingClient(
            scopedStore.Id,
            client,
            async cancellationToken =>
            {
                var read = await credentials.ReadAsync(
                    new StoreCredentialScope(scopedStore.WorkspaceId, scopedStore.Id),
                    cancellationToken).ConfigureAwait(false);
                return read.Secret;
            },
            fileReader);
        return new ListingLifecycleService(repository, listingClient, listingClient, listingClient, images: listingClient);
    }
}
