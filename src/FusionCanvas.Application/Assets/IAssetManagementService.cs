namespace FusionCanvas.Application.Assets;

public interface IAssetManagementService
{
    Guid? ActiveWorkspaceId { get; }

    void SetActiveWorkspace(Guid? workspaceId);

    Task<AssetManagementState> LoadAsync(AssetContextReference context, CancellationToken cancellationToken = default);

    Task<AssetManagementResult> ImportAssetAsync(AssetManagementImportRequest request, CancellationToken cancellationToken = default);

    Task<AssetManagementResult> RelabelAssetAsync(AssetManagementRelabelRequest request, CancellationToken cancellationToken = default);

    Task<AssetManagementResult> RemoveAssetAsync(AssetManagementRemoveRequest request, CancellationToken cancellationToken = default);
}
