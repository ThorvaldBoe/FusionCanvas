namespace FusionCanvas.Application.Catalog;

public sealed record DeleteOfferingPermanentlyRequest(Guid StoreId, Guid OfferingId, bool Confirm);
