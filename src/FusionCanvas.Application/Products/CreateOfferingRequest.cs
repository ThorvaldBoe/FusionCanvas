using FusionCanvas.Domain.Products;

namespace FusionCanvas.Application.Products;

public sealed record CreateOfferingRequest
(
    Guid ProductId,
    string Name,
    FulfillmentKind Kind,
    string? ProviderName = null,
    string? Description = null,
    string? ExternalOfferingId = null);
