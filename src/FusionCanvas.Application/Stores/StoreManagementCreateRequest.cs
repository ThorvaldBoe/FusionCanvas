namespace FusionCanvas.Application.Stores;

using FusionCanvas.Domain.Stores;

public sealed record StoreManagementCreateRequest(string Name, StoreContext? Context = null, FulfillmentStrategy FulfillmentStrategy = FulfillmentStrategy.Manual);
