namespace FusionCanvas.Application.Stores.Printify;

public interface IStorePrintifyCredentialStore
{
    Task<PrintifyCredentialReadResult> ReadAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default);
    Task<PrintifyConfigurationResult> SaveAsync(StoreCredentialScope scope, string key, CancellationToken cancellationToken = default);
}
