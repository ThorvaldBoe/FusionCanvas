namespace FusionCanvas.Application.Stores;

public sealed record StoreManagementCreateRequest(string Name, StoreContext? Context = null);
