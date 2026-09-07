namespace FusionCanvas.Application.Stores.Printify;

public interface IStorePrintifyConfigurationService
{
    Task<PrintifyConfigurationResult> ReadStatusAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default);
    Task<PrintifyConfigurationResult> SaveAsync(StoreCredentialScope scope, string key, CancellationToken cancellationToken = default);
    Task<PrintifyConfigurationResult> VerifyAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default);
}
