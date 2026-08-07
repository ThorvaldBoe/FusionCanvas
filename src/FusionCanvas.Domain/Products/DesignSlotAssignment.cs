namespace FusionCanvas.Domain.Products;

/// <summary>
/// Binds a managed asset (final image) to one cell of the slot grid.
/// PK is (rowId, designAreaId) — one image per row × area cell.
/// AssetId is null when the slot is empty.
/// The design area must belong to the item's configuration's offering.
/// </summary>
public sealed record DesignSlotAssignment
{
    public DesignSlotAssignment(Guid rowId, Guid designAreaId, Guid? assetId)
    {
        RowId = ProductRecordValidation.RequireId(rowId, nameof(rowId));
        DesignAreaId = ProductRecordValidation.RequireId(designAreaId, nameof(designAreaId));
        AssetId = assetId;
    }

    public Guid RowId { get; init; }

    public Guid DesignAreaId { get; init; }

    public Guid? AssetId { get; init; }
}