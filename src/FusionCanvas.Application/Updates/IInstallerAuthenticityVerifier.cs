namespace FusionCanvas.Application.Updates;

public interface IInstallerAuthenticityVerifier
{
    Task VerifyAsync(
        string installerPath,
        UpdateManifest manifest,
        CancellationToken cancellationToken = default);
}
