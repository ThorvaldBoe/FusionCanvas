namespace FusionCanvas.Application.Catalog;

public sealed record RestoreOfferingCascadeRequest(Guid StoreId, Guid OfferingId, bool Confirm);
