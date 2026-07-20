using System.Globalization;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspace;

public sealed record ListingMarketplaceMetadata(
    string? Price, string? Currency, string? MarketplaceNotes,
    string? ProductType, string? ProviderProductRef,
    string? ShippingNotes, string? DraftKeywords);

public sealed record ListingMetadataEditorResult(
    bool Succeeded, string? Error, ListingMarketplaceMetadata? Metadata)
{
    public static ListingMetadataEditorResult Success(ListingMarketplaceMetadata metadata) => new(true, null, metadata);
    public static ListingMetadataEditorResult Failure(string error) => new(false, error, null);
}

public sealed record ListingMetadataEditRequest(
    Guid ListingId, ListingMarketplaceMetadata Metadata);

public interface IListingMetadataEditorService
{
    Task<ListingMarketplaceMetadata> LoadAsync(Guid listingId, CancellationToken cancellationToken = default);
    Task<ListingMetadataEditorResult> SaveAsync(ListingMetadataEditRequest request, CancellationToken cancellationToken = default);
}

public sealed class ListingMetadataEditorService : IListingMetadataEditorService
{
    private readonly IWorkspaceRepository _repository;
    private readonly Func<DateTimeOffset> _clock;

    public ListingMetadataEditorService(IWorkspaceRepository repository, Func<DateTimeOffset>? clock = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public async Task<ListingMarketplaceMetadata> LoadAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var listing = snapshot.Listings.SingleOrDefault(l => l.Id == listingId);
        return listing is null ? new ListingMarketplaceMetadata(null, null, null, null, null, null, null) : ReadMetadata(listing);
    }

    public async Task<ListingMetadataEditorResult> SaveAsync(ListingMetadataEditRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var listing = snapshot.Listings.SingleOrDefault(l => l.Id == request.ListingId);
        if (listing is null)
        {
            return ListingMetadataEditorResult.Failure("Listing was not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.Metadata.Price))
        {
            if (!decimal.TryParse(request.Metadata.Price, NumberStyles.Any, CultureInfo.InvariantCulture, out var priceValue) || priceValue < 0)
            {
                return ListingMetadataEditorResult.Failure("Price must be a non-negative decimal.");
            }
        }

        var metadata = ListingMetadataCodec.ParseMetadata(listing.MetadataJson);
        ListingMetadataCodec.SetOptional(metadata, ListingMetadataCodec.ListingPriceKey, request.Metadata.Price);
        ListingMetadataCodec.SetOptional(metadata, ListingMetadataCodec.ListingCurrencyKey, request.Metadata.Currency);
        ListingMetadataCodec.SetOptional(metadata, ListingMetadataCodec.ListingMarketplaceNotesKey, request.Metadata.MarketplaceNotes);
        ListingMetadataCodec.SetOptional(metadata, ListingMetadataCodec.ListingProductTypeKey, request.Metadata.ProductType);
        ListingMetadataCodec.SetOptional(metadata, ListingMetadataCodec.ListingProviderProductRefKey, request.Metadata.ProviderProductRef);
        ListingMetadataCodec.SetOptional(metadata, ListingMetadataCodec.ListingShippingNotesKey, request.Metadata.ShippingNotes);
        ListingMetadataCodec.SetOptional(metadata, ListingMetadataCodec.ListingDraftKeywordsKey, request.Metadata.DraftKeywords);

        var changed = listing with
        {
            MetadataJson = ListingMetadataCodec.SerializeMetadata(metadata),
            UpdatedAt = _clock()
        };
        var updated = snapshot with { Listings = snapshot.Listings.Select(l => l.Id == listing.Id ? changed : l).ToArray() };

        try
        {
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return ListingMetadataEditorResult.Failure($"The metadata could not be saved. {exception.Message}");
        }

        return ListingMetadataEditorResult.Success(ReadMetadata(changed));
    }

    private static ListingMarketplaceMetadata ReadMetadata(Listing listing)
    {
        var metadata = ListingMetadataCodec.ParseMetadata(listing.MetadataJson);
        metadata.TryGetValue(ListingMetadataCodec.ListingPriceKey, out var price);
        metadata.TryGetValue(ListingMetadataCodec.ListingCurrencyKey, out var currency);
        metadata.TryGetValue(ListingMetadataCodec.ListingMarketplaceNotesKey, out var notes);
        metadata.TryGetValue(ListingMetadataCodec.ListingProductTypeKey, out var productType);
        metadata.TryGetValue(ListingMetadataCodec.ListingProviderProductRefKey, out var providerRef);
        metadata.TryGetValue(ListingMetadataCodec.ListingShippingNotesKey, out var shipping);
        metadata.TryGetValue(ListingMetadataCodec.ListingDraftKeywordsKey, out var keywords);
        return new ListingMarketplaceMetadata(price, currency, notes, productType, providerRef, shipping, keywords);
    }
}
