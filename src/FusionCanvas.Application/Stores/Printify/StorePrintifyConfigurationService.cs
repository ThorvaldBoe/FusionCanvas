using FusionCanvas.Domain.Stores;

namespace FusionCanvas.Application.Stores.Printify;

public sealed class StorePrintifyConfigurationService(
    IStoreManagementService stores,
    IStorePrintifyCredentialStore credentials,
    IPrintifyCredentialVerifier verifier) : IStorePrintifyConfigurationService
{
    private static readonly PrintifyConfigurationResult InvalidContext =
        new(PrintifyConfigurationKind.InvalidContext, "Save and select an active Store in the current workspace first.");

    public async Task<PrintifyConfigurationResult> ReadStatusAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default)
    {
        if (await ResolveAsync(scope, cancellationToken).ConfigureAwait(false) is null) return InvalidContext;
        return (await credentials.ReadAsync(scope, cancellationToken).ConfigureAwait(false)).Status;
    }

    public async Task<PrintifyConfigurationResult> SaveAsync(StoreCredentialScope scope, string key, CancellationToken cancellationToken = default)
    {
        if (await ResolveAsync(scope, cancellationToken).ConfigureAwait(false) is null) return InvalidContext;
        if (!PrintifyToken.IsValid(key))
            return new(PrintifyConfigurationKind.InvalidKey, "Enter a non-empty Printify key without control characters.");
        var existing = await credentials.ReadAsync(scope, cancellationToken).ConfigureAwait(false);
        if (existing.Status.Kind is not (PrintifyConfigurationKind.Missing or PrintifyConfigurationKind.Available))
            return existing.Status;
        return await credentials.SaveAsync(scope, key.Trim(), cancellationToken).ConfigureAwait(false);
    }

    public async Task<PrintifyConfigurationResult> VerifyAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default)
    {
        var store = await ResolveAsync(scope, cancellationToken).ConfigureAwait(false);
        if (store is null) return InvalidContext;
        if (!FulfillmentStrategyPolicy.RequiresPrintifyKey(store.FulfillmentStrategy))
            return new(PrintifyConfigurationKind.InvalidContext, "Save Shopify + Printify as the Store strategy before verifying.");
        var read = await credentials.ReadAsync(scope, cancellationToken).ConfigureAwait(false);
        if (read.Status.Kind != PrintifyConfigurationKind.Available) return read.Status;
        if (!PrintifyToken.IsValid(read.Secret)) return PrintifyConfigurationResult.Unavailable;
        return await verifier.VerifyAsync(read.Secret!, cancellationToken).ConfigureAwait(false);
    }

    private async Task<StoreSummary?> ResolveAsync(StoreCredentialScope scope, CancellationToken cancellationToken)
    {
        var state = await stores.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (scope.WorkspaceId == Guid.Empty || scope.StoreId == Guid.Empty ||
            state.ActiveWorkspaceId != scope.WorkspaceId || stores.ActiveWorkspaceId != scope.WorkspaceId)
            return null;
        return state.ActiveStores.FirstOrDefault(store =>
            store.Id == scope.StoreId && store.WorkspaceId == scope.WorkspaceId && !store.IsArchived);
    }
}
