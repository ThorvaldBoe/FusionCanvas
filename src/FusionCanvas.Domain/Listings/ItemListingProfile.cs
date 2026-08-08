namespace FusionCanvas.Domain.Listings;

public sealed record ItemListingProfile
{
    public ItemListingProfile(
        Guid itemId,
        ListingFulfillmentStrategy strategy = ListingFulfillmentStrategy.Manual,
        decimal? price = null,
        string? currency = null,
        ListingReadinessState readiness = ListingReadinessState.Draft,
        ListingPublicationState publication = ListingPublicationState.NotPublished,
        IReadOnlyList<Guid>? mediaAssetIds = null,
        IReadOnlyList<Guid>? variantIds = null,
        string? sharedMetadataJson = null,
        string? fieldOwnershipJson = null,
        DateTimeOffset? updatedAt = null)
    {
        if (itemId == Guid.Empty)
        {
            throw new ArgumentException("An item identifier is required.", nameof(itemId));
        }

        if (price is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
        }

        var normalizedCurrency = string.IsNullOrWhiteSpace(currency) ? null : currency.Trim().ToUpperInvariant();
        if (normalizedCurrency is not null && (normalizedCurrency.Length != 3 || normalizedCurrency.Any(character => character is < 'A' or > 'Z')))
        {
            throw new ArgumentException("Currency must be a three-letter ISO-style code.", nameof(currency));
        }

        if (publication == ListingPublicationState.Published && readiness != ListingReadinessState.Ready)
        {
            throw new ArgumentException("A published listing must be locally ready.", nameof(publication));
        }

        ItemId = itemId;
        Strategy = strategy;
        Price = price;
        Currency = normalizedCurrency;
        Readiness = readiness;
        Publication = publication;
        MediaAssetIds = NormalizeIds(mediaAssetIds);
        VariantIds = NormalizeIds(variantIds);
        SharedMetadataJson = NormalizeJson(sharedMetadataJson);
        FieldOwnershipJson = NormalizeJson(fieldOwnershipJson);
        UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow;
    }

    public Guid ItemId { get; init; }
    public ListingFulfillmentStrategy Strategy { get; init; }
    public decimal? Price { get; init; }
    public string? Currency { get; init; }
    public ListingReadinessState Readiness { get; init; }
    public ListingPublicationState Publication { get; init; }
    public IReadOnlyList<Guid> MediaAssetIds { get; init; }
    public IReadOnlyList<Guid> VariantIds { get; init; }
    public string SharedMetadataJson { get; init; }
    public string FieldOwnershipJson { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }

    private static IReadOnlyList<Guid> NormalizeIds(IReadOnlyList<Guid>? values) =>
        (values ?? []).Where(value => value != Guid.Empty).Distinct().ToArray();

    private static string NormalizeJson(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "{}" : value;
}
