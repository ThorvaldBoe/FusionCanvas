namespace FusionCanvas.Application.Updates;

public sealed record VerifiedUpdatePackage(UpdateManifest Manifest, string InstallerPath);
