using FusionCanvas.Domain.Products;

namespace FusionCanvas.Application.Products;

public sealed record DeleteVariantRequest
(Guid VariantId, bool Confirm);
