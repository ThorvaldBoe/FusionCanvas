namespace FusionCanvas.Application.Listings;

public sealed record ListingVariantProjection(
    Guid SourceVariantId,
    string Name,
    string? Color,
    IReadOnlyList<KeyValuePair<string, string>> Options,
    decimal ProductionCost,
    ListingPrice Price)
{
    public int? ExternalVariantId { get; init; }
}
