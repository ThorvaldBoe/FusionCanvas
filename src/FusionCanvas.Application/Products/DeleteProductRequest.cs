using FusionCanvas.Domain.Products;

namespace FusionCanvas.Application.Products;

public sealed record DeleteProductRequest
(Guid ProductId, bool Confirm);
