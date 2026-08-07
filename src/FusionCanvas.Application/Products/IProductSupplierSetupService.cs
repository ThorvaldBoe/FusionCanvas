namespace FusionCanvas.Application.Products;

public interface IProductSupplierSetupService
{
    Task<ProductSupplierSetupState> LoadForStoreAsync(Guid storeId, CancellationToken cancellationToken = default);

    Task<ProductSupplierSetupResult> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);

    Task<ProductSupplierSetupResult> UpdateProductAsync(UpdateProductRequest request, CancellationToken cancellationToken = default);

    Task<ProductSupplierSetupResult> DeleteProductAsync(DeleteProductRequest request, CancellationToken cancellationToken = default);

    Task<ProductSupplierSetupResult> CreateOfferingAsync(CreateOfferingRequest request, CancellationToken cancellationToken = default);

    Task<ProductSupplierSetupResult> UpdateOfferingAsync(UpdateOfferingRequest request, CancellationToken cancellationToken = default);

    Task<ProductSupplierSetupResult> DeleteOfferingAsync(DeleteOfferingRequest request, CancellationToken cancellationToken = default);

    Task<ProductSupplierSetupResult> CreateVariantAsync(CreateVariantRequest request, CancellationToken cancellationToken = default);

    Task<ProductSupplierSetupResult> UpdateVariantAsync(UpdateVariantRequest request, CancellationToken cancellationToken = default);

    Task<ProductSupplierSetupResult> DeleteVariantAsync(DeleteVariantRequest request, CancellationToken cancellationToken = default);

    Task<ProductSupplierSetupResult> CreateDesignAreaAsync(CreateDesignAreaRequest request, CancellationToken cancellationToken = default);

    Task<ProductSupplierSetupResult> UpdateDesignAreaAsync(UpdateDesignAreaRequest request, CancellationToken cancellationToken = default);

    Task<ProductSupplierSetupResult> DeleteDesignAreaAsync(DeleteDesignAreaRequest request, CancellationToken cancellationToken = default);
}
