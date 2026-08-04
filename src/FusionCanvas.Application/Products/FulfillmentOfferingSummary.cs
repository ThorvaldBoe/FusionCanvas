using FusionCanvas.Domain.Products;

namespace FusionCanvas.Application.Products;

public sealed record FulfillmentOfferingSummary
(
    Guid Id,
    Guid ProductId,
    string Name,
    string? Description,
    FulfillmentKind Kind,
    string? ProviderName,
    string? ExternalOfferingId,
    IReadOnlyList<ProductVariantSummary> Variants,
    IReadOnlyList<DesignAreaSummary> DesignAreas);
