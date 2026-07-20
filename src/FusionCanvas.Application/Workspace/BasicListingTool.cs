using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspace;

public sealed record MockupGenerationRequest(
    Guid ListingId, Guid DesignId,
    Guid MockupProductId, Guid MockupTemplateId,
    IReadOnlyList<Guid> ColorVariantIds);

public sealed record MockupGenerationResult(
    bool Succeeded, IReadOnlyList<Guid> CreatedMockupIds, string? Error);

public interface IBasicListingToolService
{
    Task<MockupGenerationResult> GenerateMockupsAsync(MockupGenerationRequest request, CancellationToken cancellationToken = default);
    Task<bool> CanGenerateMockupsAsync(Guid listingId, CancellationToken cancellationToken = default);
}

public sealed class BasicListingToolService : IBasicListingToolService
{
    private readonly IMockupRecordsService _mockupRecords;
    private readonly IDesignRecordsService _designRecords;
    private readonly Func<Guid> _newId;

    public BasicListingToolService(
        IMockupRecordsService mockupRecords,
        IDesignRecordsService designRecords,
        Func<Guid>? newId = null)
    {
        _mockupRecords = mockupRecords ?? throw new ArgumentNullException(nameof(mockupRecords));
        _designRecords = designRecords ?? throw new ArgumentNullException(nameof(designRecords));
        _newId = newId ?? Guid.NewGuid;
    }

    public async Task<MockupGenerationResult> GenerateMockupsAsync(MockupGenerationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ColorVariantIds.Count == 0)
        {
            return new MockupGenerationResult(false, [], "At least one color variant must be selected.");
        }

        var designState = await _designRecords.LoadAsync(request.ListingId, cancellationToken).ConfigureAwait(false);
        if (!designState.FinalSelectedIds.Contains(request.DesignId))
        {
            return new MockupGenerationResult(false, [], "The selected design must be a final-selected variant.");
        }

        var createdIds = new List<Guid>();
        foreach (var colorId in request.ColorVariantIds)
        {
            var mockupId = _newId();
            var regenerationMeta = $$"""{"designId":"{{request.DesignId}}","mockupProductId":"{{request.MockupProductId}}","mockupTemplateId":"{{request.MockupTemplateId}}","colorVariantId":"{{colorId}}","generatedAt":"{{DateTimeOffset.UtcNow:O}}"}""";
            var result = await _mockupRecords.CreateAsync(new MockupCreateRequest(
                request.ListingId, $"Generated Mockup {colorId}", request.DesignId,
                SourceMethod: "generated",
                RegenerationMetadataJson: regenerationMeta), cancellationToken).ConfigureAwait(false);

            if (result.Succeeded)
            {
                createdIds.Add(result.Mockup!.Id);
            }
        }

        return new MockupGenerationResult(true, createdIds, null);
    }

    public async Task<bool> CanGenerateMockupsAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        var designState = await _designRecords.LoadAsync(listingId, cancellationToken).ConfigureAwait(false);
        return designState.FinalSelectedIds.Count > 0;
    }
}
