namespace FusionCanvas.Application.Stores.Printify;

public interface IPrintifyCredentialVerifier
{
    Task<PrintifyConfigurationResult> VerifyAsync(string key, CancellationToken cancellationToken = default);
}
