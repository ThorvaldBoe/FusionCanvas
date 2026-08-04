using FusionCanvas.Domain.Products;

namespace FusionCanvas.Application.Products;

public sealed record CreateDesignAreaRequest
(
    Guid OfferingId,
    string Name,
    string Position,
    string DecorationMethod,
    int Width,
    int Height,
    IReadOnlyList<Guid>? VariantIds = null);
