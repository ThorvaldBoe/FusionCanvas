using FusionCanvas.Domain.Products;

namespace FusionCanvas.Application.Products;

public sealed record DesignAreaSummary
(
    Guid Id,
    Guid OfferingId,
    string Name,
    string Position,
    string DecorationMethod,
    int Width,
    int Height,
    IReadOnlyList<Guid> VariantIds,
    bool IsChoiceNetwork);
