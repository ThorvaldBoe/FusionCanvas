using FusionCanvas.Domain.Products;

namespace FusionCanvas.Application.Products;

public sealed record UpdateVariantRequest
(
    Guid VariantId,
    IReadOnlyList<VariantOptionDraft> Options);
