namespace FusionCanvas.Application.Catalog;

public sealed record RestoreBlueprintCascadeRequest(Guid StoreId, Guid BlueprintId, bool Confirm);
