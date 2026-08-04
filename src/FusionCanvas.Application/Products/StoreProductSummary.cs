using FusionCanvas.Domain.Products;

namespace FusionCanvas.Application.Products;

public sealed record StoreProductSummary
(
    Guid Id,
    Guid StoreId,
    string Name,
    string? Description,
    string? ExternalProductId,
    IReadOnlyList<FulfillmentOfferingSummary> Offerings);
