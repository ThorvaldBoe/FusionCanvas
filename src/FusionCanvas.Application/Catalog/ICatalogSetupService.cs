using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Catalog;

public interface ICatalogSetupService
{
    Task<CatalogSetupState> LoadForStoreAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> CreateBlueprintAsync(CreateBlueprintRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> CreatePrintProviderAsync(CreatePrintProviderRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> CreateOfferingAsync(CreateOfferingRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> CreateOptionAsync(CreateOfferingOptionRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> CreateOptionValueAsync(CreateOptionValueRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> ReorderOptionValuesAsync(ReorderOptionValuesRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> CreateVariantAsync(CreateOfferingVariantRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> CreatePlaceholderAsync(CreateOfferingPlaceholderRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> ArchiveAsync(ArchiveCatalogRecordRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> ArchiveBlueprintWithDependentsAsync(ArchiveBlueprintWithDependentsRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> DeleteBlueprintPermanentlyAsync(DeleteBlueprintPermanentlyRequest request, CancellationToken cancellationToken = default);
    Task<CatalogArchivePlan> PreviewArchiveOfferingAsync(ArchiveOfferingCascadeRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> ArchiveOfferingCascadeAsync(ArchiveOfferingCascadeRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> RestoreOfferingCascadeAsync(RestoreOfferingCascadeRequest request, CancellationToken cancellationToken = default);
    Task<CatalogOfferingDeletePlan> PreviewDeleteOfferingPermanentlyAsync(DeleteOfferingPermanentlyRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> DeleteOfferingPermanentlyAsync(DeleteOfferingPermanentlyRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> RestoreAsync(ArchiveCatalogRecordRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> UpdateAsync(UpdateCatalogRecordRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> DeleteAsync(ArchiveCatalogRecordRequest request, CancellationToken cancellationToken = default);
}
