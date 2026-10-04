namespace FusionCanvas.Application.Mockups;
public interface IMockupGenerationService
{
    Task<MockupGenerationState> LoadAsync(Guid itemId, bool isReadOnly, string readOnlyReason, CancellationToken cancellationToken = default);
    Task<MockupGenerationResult> ApplyAsync(MockupGenerationRequest request, CancellationToken cancellationToken = default);

    Task<Stream> OpenPreviewAsync(Guid itemId, Guid assetId, CancellationToken cancellationToken = default) =>
        Task.FromException<Stream>(new NotSupportedException("Mockup preview is unavailable in this runtime."));

    Task<MockupOutputOperationResult> ExportCopyAsync(Guid itemId, Guid assetId, string destinationPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(MockupOutputOperationResult.Failure("Mockup download is unavailable in this runtime."));

    Task<MockupOutputOperationResult> RemoveAsync(Guid itemId, Guid assetId, CancellationToken cancellationToken = default) =>
        Task.FromResult(MockupOutputOperationResult.Failure("Mockup removal is unavailable in this runtime."));
}
