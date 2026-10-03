using FusionCanvas.Application.AI;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Application.Mockups;

public interface IMockupSourceMetadataAssistanceService
{
    Task<AiAvailabilityResult> GetAvailabilityAsync(CancellationToken cancellationToken = default);

    Task<MockupSourceMetadataAssistanceResult> AssistAsync(
        MockupSourceMetadataAssistanceRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record MockupSourceMetadataAssistanceRequest(
    IReadOnlyList<MockupSourceMetadataImage> Images,
    IReadOnlyList<MockupSourceMetadataValue> Values,
    IReadOnlyList<MockupSourceMetadataPlacementReference> PlacementReferences,
    string? DesignAreaName = null,
    int? DesignAreaWidth = null,
    int? DesignAreaHeight = null);

public sealed record MockupSourceMetadataImage(
    string Token,
    string FileName,
    string SourcePath,
    int ImageWidth,
    int ImageHeight,
    IReadOnlyList<Guid> OptionValueIds,
    MockupImageSpaceMapping? Mapping);

public sealed record MockupSourceMetadataValue(
    Guid Id,
    string Label,
    OptionKind Kind);

public sealed record MockupSourceMetadataPlacementReference(
    string Token,
    IReadOnlyList<Guid> OptionValueIds,
    MockupImageSpaceMapping? Mapping);

public sealed record MockupSourceMetadataAssistanceResult(
    bool Succeeded,
    string? Message,
    IReadOnlyList<MockupSourceMetadataAssistanceItem> Items);

public sealed record MockupSourceMetadataAssistanceItem(
    string Token,
    bool Applied,
    IReadOnlyList<Guid> OptionValueIds,
    MockupImageSpaceMapping? Mapping,
    decimal? Confidence,
    string Status,
    string? Message,
    bool ImageSent);
