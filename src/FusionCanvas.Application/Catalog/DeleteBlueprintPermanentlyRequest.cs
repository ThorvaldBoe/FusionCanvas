namespace FusionCanvas.Application.Catalog;

public sealed record DeleteBlueprintPermanentlyRequest(Guid StoreId, Guid BlueprintId, bool Confirm);
