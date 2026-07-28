namespace FusionCanvas.Application.RejectedPhrases;

public interface IRejectedPhraseManagementService
{
    Task<RejectedPhraseManagementResult> InitializeAsync(
        RejectedPhraseScope scope,
        string? searchText = null,
        CancellationToken cancellationToken = default);

    Task<RejectedPhraseManagementResult> LoadAsync(
        RejectedPhraseScope scope,
        string? searchText = null,
        CancellationToken cancellationToken = default);

    Task<RejectedPhraseManagementResult> CreateAsync(
        RejectedPhraseCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<RejectedPhraseManagementResult> UpdateAsync(
        RejectedPhraseUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<RejectedPhraseManagementResult> DeleteAsync(
        Guid id,
        RejectedPhraseScope scope,
        string? searchText = null,
        CancellationToken cancellationToken = default);
}
