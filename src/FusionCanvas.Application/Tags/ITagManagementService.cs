namespace FusionCanvas.Application.Tags;

public interface ITagManagementService
{
    Guid? ActiveStoreId { get; }

    void SetActiveStore(Guid? storeId);

    Task<TagManagementState> LoadAsync(Guid? storeId, CancellationToken cancellationToken = default);

    Task<TagManagementResult> CreateTagAsync(TagManagementCreateRequest request, CancellationToken cancellationToken = default);

    Task<TagManagementResult> UpdateTagAsync(TagManagementUpdateRequest request, CancellationToken cancellationToken = default);

    Task<TagManagementResult> ArchiveTagAsync(Guid tagId, CancellationToken cancellationToken = default);

    Task<TagManagementResult> RestoreTagAsync(Guid tagId, CancellationToken cancellationToken = default);

    Task<TagManagementResult> DeleteTagAsync(TagManagementDeleteRequest request, CancellationToken cancellationToken = default);

    Task<TagApplicationResult> ApplyTagAsync(ApplyTagRequest request, CancellationToken cancellationToken = default);

    Task<TagManagementResult> RemoveTagAsync(RemoveTagRequest request, CancellationToken cancellationToken = default);

    Task<TagApplicationResult> ApplyOrCreateTagAsync(ApplyOrCreateTagRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TagSummary>> GetActiveTagVocabularyAsync(Guid storeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetItemTagsAsync(Guid itemId, CancellationToken cancellationToken = default);

    Task<int> GetTagApplicationCountAsync(Guid tagId, CancellationToken cancellationToken = default);
}
