using FusionCanvas.Domain.Products;

namespace FusionCanvas.Application.Products;

public sealed record CreateVariantRequest
(
    Guid OfferingId,
    IReadOnlyList<VariantOptionDraft> Options);
