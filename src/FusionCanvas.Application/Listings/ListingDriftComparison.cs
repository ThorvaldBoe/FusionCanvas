namespace FusionCanvas.Application.Listings;

public sealed record ListingDriftComparison(IReadOnlyList<ListingDriftChange> Changes)
{
    public static ListingDriftComparison Empty { get; } = new([]);
    public bool IsClean => Changes.Count == 0;
    public bool HasRemoteOnlyChanges => Changes.Any(value => value.RemoteChanged && !value.LocalChanged);
    public bool HasConflict => Changes.Any(value => value.RemoteChanged && value.LocalChanged);
}
