namespace FusionCanvas.Application.Stores;

public sealed record StoreManagementDeleteRequest(Guid StoreId, bool ConfirmPermanentDeletion);
