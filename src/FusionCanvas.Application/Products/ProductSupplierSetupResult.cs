namespace FusionCanvas.Application.Products;

public sealed record ProductSupplierSetupResult(
    bool Succeeded,
    string? Error,
    ProductSupplierSetupState State,
    StoreProductSummary? Product = null,
    FulfillmentOfferingSummary? Offering = null,
    ProductVariantSummary? Variant = null,
    DesignAreaSummary? DesignArea = null)
{
    public static ProductSupplierSetupResult Success(ProductSupplierSetupState state) =>
        new(true, null, state);

    public static ProductSupplierSetupResult Failure(string error, ProductSupplierSetupState state) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Product and fulfillment operation failed." : error, state);
}
