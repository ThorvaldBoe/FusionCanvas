namespace FusionCanvas.Application.DesignFiles;

public interface IGlobalColorRemovalService
{
    Task<GlobalColorRemovalAvailabilityResult> CheckAvailabilityAsync(
        Guid itemId,
        Guid assetId,
        CancellationToken cancellationToken = default);

    Task<GlobalColorRemovalSourceResult> OpenSourcePreviewAsync(
        Guid itemId,
        Guid assetId,
        CancellationToken cancellationToken = default);

    Task<GlobalColorRemovalSampleResult> SampleColorAsync(
        Guid itemId,
        Guid assetId,
        int x,
        int y,
        CancellationToken cancellationToken = default);

    Task<GlobalColorRemovalPreviewResult> PreviewAsync(
        Guid itemId,
        Guid assetId,
        GlobalColorRemovalParameters parameters,
        CancellationToken cancellationToken = default);

    Task<GlobalColorRemovalApplyResult> ApplyAsync(
        Guid itemId,
        Guid assetId,
        GlobalColorRemovalParameters parameters,
        CancellationToken cancellationToken = default);
}
