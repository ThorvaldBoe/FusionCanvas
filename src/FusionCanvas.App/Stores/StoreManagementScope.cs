namespace FusionCanvas.App.Stores;

/// <summary>The immutable identity of the workspace/store currently being edited.</summary>
public sealed record StoreManagementScope(
    Guid? WorkspaceId,
    Guid? StoreId,
    bool IsStoreArchived = false,
    bool IsCreatingNewStore = false)
{
    public static StoreManagementScope Empty { get; } = new(null, null);
}
