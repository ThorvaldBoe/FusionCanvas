using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Catalog;

public sealed record CreatePrintProviderRequest(Guid StoreId, string Name, string? ExternalProviderId = null);
