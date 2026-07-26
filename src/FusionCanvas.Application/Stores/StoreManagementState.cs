namespace FusionCanvas.Application.Stores;

public sealed record StoreManagementState(
    Guid? ActiveWorkspaceId,
    IReadOnlyList<StoreSummary> ActiveStores,
    IReadOnlyList<StoreSummary> ArchivedStores,
    Guid? ActiveStoreId,
    StoreSummary? ActiveStore,
    bool NeedsFirstStore);
