using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Catalog;

public sealed record ArchiveCatalogRecordRequest(Guid StoreId, CatalogRecordKind Kind, Guid RecordId);
