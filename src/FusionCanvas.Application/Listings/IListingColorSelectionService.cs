namespace FusionCanvas.Application.Listings;

public interface IListingColorSelectionService
{
    Task<ListingColorSelectionState> LoadAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task SetSelectedColorsAsync(Guid itemId, IReadOnlyCollection<Guid> colorOptionValueIds, CancellationToken cancellationToken = default);
    Task SetArtworkVariantAsync(Guid itemId, Guid colorOptionValueId, Guid artworkVariantId, CancellationToken cancellationToken = default);
    Task SetFavoriteAsync(Guid offeringId, Guid colorOptionValueId, bool isFavorite, CancellationToken cancellationToken = default);
    Task ResolveUnresolvedColorAsync(Guid itemId, int sortOrder, Guid colorOptionValueId, CancellationToken cancellationToken = default) =>
        Task.FromException(new InvalidOperationException("Legacy Listing Colors cannot be resolved by this service."));
    Task<Stream?> OpenArtworkPreviewAsync(Guid itemId, Guid colorOptionValueId, CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream?>(null);
}
