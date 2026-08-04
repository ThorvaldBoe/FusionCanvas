namespace FusionCanvas.Application.Products;

public sealed record ProductSupplierSetupState(
    Guid? StoreId,
    bool IsReadOnly,
    bool NeedsFirstProduct,
    IReadOnlyList<StoreProductSummary> Products);
