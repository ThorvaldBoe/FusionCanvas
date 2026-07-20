using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspace;

public interface IImageGenerationProvider
{
    Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest request, CancellationToken cancellationToken = default);
}

public sealed record ImageGenerationRequest(
    Guid ListingId, Guid? ConceptId,
    string? Idea, string? Phrase, string? GraphicDirection,
    string? VisualStyle, string? Composition, string? Constraints);

public sealed record ImageGenerationResult(bool Succeeded, IReadOnlyList<string> OutputAssetReferences, string? Error);

public interface IBasicDesignToolService
{
    Task<ImageGenerationResult> GenerateDesignAsync(Guid listingId, string? visualStyle, CancellationToken cancellationToken = default);
    Task<bool> CanAdvanceToListingAsync(Guid listingId, CancellationToken cancellationToken = default);
}

public sealed class BasicDesignToolService : IBasicDesignToolService
{
    private readonly IDesignRecordsService _designRecords;
    private readonly IImageGenerationProvider? _provider;

    public BasicDesignToolService(IDesignRecordsService designRecords, IImageGenerationProvider? provider = null)
    {
        _designRecords = designRecords ?? throw new ArgumentNullException(nameof(designRecords));
        _provider = provider;
    }

    public async Task<ImageGenerationResult> GenerateDesignAsync(Guid listingId, string? visualStyle, CancellationToken cancellationToken = default)
    {
        if (_provider is null)
        {
            return new ImageGenerationResult(false, [], "An image-generation provider must be configured.");
        }

        return await _provider.GenerateAsync(new ImageGenerationRequest(listingId, null, null, null, null, visualStyle, null, null), cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> CanAdvanceToListingAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        var state = await _designRecords.LoadAsync(listingId, cancellationToken).ConfigureAwait(false);
        return state.FinalSelectedIds.Count > 0;
    }
}
