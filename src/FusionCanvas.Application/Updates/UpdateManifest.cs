namespace FusionCanvas.Application.Updates;

public sealed record UpdateManifest(
    int SchemaVersion,
    string ProductVersion,
    string Platform,
    Uri InstallerUri,
    string Sha256,
    Uri ReleaseUri,
    string PublisherCertificateSha256);
