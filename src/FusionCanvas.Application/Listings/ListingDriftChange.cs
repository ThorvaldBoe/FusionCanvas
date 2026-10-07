namespace FusionCanvas.Application.Listings;

public sealed record ListingDriftChange(string Field, string? Last, string? Remote, string? Local, bool RemoteChanged, bool LocalChanged);
