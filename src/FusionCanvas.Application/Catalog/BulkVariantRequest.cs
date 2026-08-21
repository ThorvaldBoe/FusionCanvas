namespace FusionCanvas.Application.Catalog;

public sealed record BulkVariantRequest(OfferingContext Context, Guid ColorOptionValueId, IReadOnlyList<Guid> EnabledSizeOptionValueIds);
