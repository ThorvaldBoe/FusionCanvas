using FusionCanvas.Domain.Products;

namespace FusionCanvas.Application.Products;

public sealed record DeleteOfferingRequest
(Guid OfferingId, bool Confirm);
