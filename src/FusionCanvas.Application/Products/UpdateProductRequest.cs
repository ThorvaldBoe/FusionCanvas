using FusionCanvas.Domain.Products;

namespace FusionCanvas.Application.Products;

public sealed record UpdateProductRequest
(
    Guid ProductId,
    string Name,
    string? Description = null,
    string? ExternalProductId = null);
