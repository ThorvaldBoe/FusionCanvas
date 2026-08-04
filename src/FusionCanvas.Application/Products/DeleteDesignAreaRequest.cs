using FusionCanvas.Domain.Products;

namespace FusionCanvas.Application.Products;

public sealed record DeleteDesignAreaRequest
(Guid DesignAreaId, bool Confirm);
