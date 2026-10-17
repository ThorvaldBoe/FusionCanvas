namespace FusionCanvas.Application.Updates;

public enum UpdateTrack
{
    Stable,
    Preview
}

public sealed record UpdateRelease(
    string TagName,
    string ProductVersion,
    UpdateTrack Track,
    Uri ReleaseUri,
    Uri ManifestUri);

public sealed record UpdateManifest(
    int SchemaVersion,
    string ProductVersion,
    string Platform,
    UpdateTrack Track,
    string TagName,
    string SourceCommit,
    Uri InstallerUri,
    string Sha256,
    Uri ReleaseUri,
    Uri BundleUri);
