namespace FusionCanvas.Application.Updates;

public interface IInstallerAuthenticityVerifier
{
    Task VerifyAsync(
        string installerPath,
        string bundlePath,
        UpdateManifest manifest,
        CancellationToken cancellationToken = default);
}
