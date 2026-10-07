namespace FusionCanvas.Application.Listings;

public sealed record ListingArtworkProjection(
    Guid DesignAreaId,
    Guid AssetId,
    string AssetPath,
    ArtworkDimensions Artwork,
    ArtworkDimensions Target,
    ArtworkPlacement Placement)
{
    public string Position { get; init; } = string.Empty;
    public string DecorationMethod { get; init; } = string.Empty;
}
