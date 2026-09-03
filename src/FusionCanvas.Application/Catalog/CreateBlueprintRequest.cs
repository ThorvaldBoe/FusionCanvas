using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Catalog;

public sealed record CreateBlueprintRequest(Guid StoreId, string Name, string? Description = null);
