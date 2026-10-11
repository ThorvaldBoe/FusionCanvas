using FusionCanvas.Application.Stores.Printify;

namespace FusionCanvas.App.Workspace;

internal sealed class UiTestPrintifyCredentialStore : IStorePrintifyCredentialStore
{
    public Task<PrintifyCredentialReadResult> ReadAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PrintifyCredentialReadResult(new(PrintifyConfigurationKind.Available, "UI test fixture"), "synthetic-ui-test-key"));

    public Task<PrintifyConfigurationResult> SaveAsync(StoreCredentialScope scope, string key, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
