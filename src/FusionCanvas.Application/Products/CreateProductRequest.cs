using FusionCanvas.Domain.Products;

namespace FusionCanvas.Application.Products;

public sealed record CreateProductRequest
(
    Guid StoreId,
    string Name,
    string? Description = null,
    string? ExternalProductId = null);
