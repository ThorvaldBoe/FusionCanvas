using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Catalog;

public sealed record UpdateCatalogRecordRequest(Guid StoreId, CatalogRecordKind Kind, Guid RecordId, string? Name = null, string? Description = null, string? Position = null, string? DecorationMethod = null, int? Width = null, int? Height = null, string? ProviderNetworkCode = null, Guid? DefaultPlaceholderId = null, string? ExternalOfferingId = null, Guid? PrintProviderId = null);
