namespace FusionCanvas.Application.Catalog;

public interface IOfferingManagementService
{
    Task<IReadOnlyList<BlueprintOfferingSetupSummary>> LoadForBlueprintAsync(Guid storeId, Guid blueprintId, CancellationToken cancellationToken = default);
    Task<OfferingManagementState> LoadOfferingAsync(OfferingContext context, CancellationToken cancellationToken = default);
    Task<BulkVariantPreview> PreviewBulkVariantsAsync(BulkVariantRequest request, CancellationToken cancellationToken = default);
    Task<BulkVariantResult> ConfirmBulkVariantsAsync(BulkVariantRequest request, CancellationToken cancellationToken = default);
    Task<FocusedCommandResult> CreateVariantAsync(CreateFocusedVariantRequest request, CancellationToken cancellationToken = default);
    Task<FocusedCommandResult> CreateDesignAreaAsync(CreateFocusedDesignAreaRequest request, CancellationToken cancellationToken = default);
    Task<FocusedCommandResult> UpdateDesignAreaAsync(UpdateFocusedDesignAreaRequest request, CancellationToken cancellationToken = default);
    Task<FocusedCommandResult> CreateMockupTemplateAsync(CreateFocusedMockupTemplateRequest request, CancellationToken cancellationToken = default);
}
