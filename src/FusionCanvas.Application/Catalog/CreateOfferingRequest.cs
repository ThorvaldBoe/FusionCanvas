using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Catalog;

public sealed record CreateOfferingRequest(Guid StoreId, Guid BlueprintId, string Name, BlueprintOfferingKind Kind, Guid? PrintProviderId = null, string? ProviderNetworkCode = null, string? Description = null, string? ExternalOfferingId = null);
