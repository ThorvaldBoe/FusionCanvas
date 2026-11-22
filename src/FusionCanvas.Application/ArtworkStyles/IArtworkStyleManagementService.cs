namespace FusionCanvas.Application.ArtworkStyles;

public interface IArtworkStyleManagementService
{
    Task<ArtworkStyleManagementState> LoadAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    Task<ArtworkStyleOperationResult> SaveAsync(
        Guid workspaceId,
        Guid? styleId,
        string title,
        string description,
        IReadOnlyList<ArtworkStyleImageSelection> images,
        CancellationToken cancellationToken = default);

    Task<ArtworkStyleOperationResult> DeleteAsync(Guid workspaceId, Guid styleId, CancellationToken cancellationToken = default);
}
