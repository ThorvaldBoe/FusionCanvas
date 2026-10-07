namespace FusionCanvas.Application.Listings;

public sealed record ListingSnapshot(IReadOnlyDictionary<string, string?> Fields)
{
    public static ListingSnapshot Empty { get; } = new(new Dictionary<string, string?>(StringComparer.Ordinal));
}
