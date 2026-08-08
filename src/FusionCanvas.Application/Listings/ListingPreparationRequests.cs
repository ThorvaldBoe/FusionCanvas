using FusionCanvas.Domain.Listings;

namespace FusionCanvas.Application.Listings;

public sealed record UpdateListingPreparationRequest(
    Guid ItemId,
    ListingFulfillmentStrategy Strategy,
    decimal? Price,
    string? Currency,
    ListingReadinessState Readiness,
    ListingPublicationState Publication,
    IReadOnlyList<Guid>? MediaAssetIds = null,
    IReadOnlyList<Guid>? VariantIds = null,
    string? SharedMetadataJson = null,
    string? FieldOwnershipJson = null,
    ListingProviderState? ProviderState = null);

public sealed record BindShopifyListingRequest(
    Guid ItemId,
    string ExternalId,
    string Channel,
    bool FromPrintifyPublication = false);
