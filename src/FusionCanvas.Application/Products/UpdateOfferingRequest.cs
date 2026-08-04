using FusionCanvas.Domain.Products;

namespace FusionCanvas.Application.Products;

public sealed record UpdateOfferingRequest
(
    Guid OfferingId,
    string Name,
    FulfillmentKind Kind,
    string? ProviderName = null,
    string? Description = null,
    string? ExternalOfferingId = null);
