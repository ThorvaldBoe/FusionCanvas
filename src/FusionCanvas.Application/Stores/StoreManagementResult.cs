namespace FusionCanvas.Application.Stores;

public sealed record StoreManagementResult(
    bool Succeeded,
    string? Error,
    StoreSummary? Store,
    StoreManagementState State)
{
    public static StoreManagementResult Success(StoreSummary? store, StoreManagementState state) =>
        new(true, null, store, state);

    public static StoreManagementResult Failure(string error, StoreManagementState state) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Store operation failed." : error, null, state);
}
