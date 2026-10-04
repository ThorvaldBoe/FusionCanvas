namespace FusionCanvas.Application.Mockups;

public interface IMockupOutputInvalidationService
{
    Task<MockupOutputInvalidationResult> InvalidateAsync(
        Guid itemId,
        IReadOnlySet<Guid>? sourceDesignAssetIds = null,
        bool invalidateAll = false,
        CancellationToken cancellationToken = default);
}
