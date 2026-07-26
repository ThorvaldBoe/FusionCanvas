namespace FusionCanvas.Application.Niches;

public interface INicheManagementService
{
    Guid? ActiveWorkspaceId { get; }

    Guid? ActiveStoreId { get; }

    Guid? ActiveNicheId { get; }

    void SetActiveWorkspace(Guid? workspaceId);

    Task<NicheManagementState> LoadAsync(Guid? storeId, CancellationToken cancellationToken = default);

    Task<NicheManagementResult> CreateNicheAsync(NicheManagementCreateRequest request, CancellationToken cancellationToken = default);

    Task<NicheManagementResult> UpdateNicheAsync(NicheManagementUpdateRequest request, CancellationToken cancellationToken = default);

    Task<NicheManagementResult> ArchiveNicheAsync(Guid nicheId, CancellationToken cancellationToken = default);

    Task<NicheManagementResult> RestoreNicheAsync(Guid nicheId, CancellationToken cancellationToken = default);

    Task<NicheManagementResult> DeleteNicheAsync(NicheManagementDeleteRequest request, CancellationToken cancellationToken = default);

    Task<NicheManagementResult> SelectNicheAsync(Guid nicheId, CancellationToken cancellationToken = default);
}
