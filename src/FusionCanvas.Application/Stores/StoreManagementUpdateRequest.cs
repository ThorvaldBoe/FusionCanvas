using FusionCanvas.Domain.Stores;

namespace FusionCanvas.Application.Stores;

public sealed record StoreManagementUpdateRequest(Guid StoreId, string Name, StoreContext? Context = null, FulfillmentStrategy? FulfillmentStrategy = null);
