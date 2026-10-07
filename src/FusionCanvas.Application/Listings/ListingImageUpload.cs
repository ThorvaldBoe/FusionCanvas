namespace FusionCanvas.Application.Listings;

public sealed record ListingImageUpload(Guid AssetId, string WorkspaceRelativePath, string Fingerprint);
