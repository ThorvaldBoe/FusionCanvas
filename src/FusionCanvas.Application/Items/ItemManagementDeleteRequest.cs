namespace FusionCanvas.Application.Items;

public sealed record ItemManagementDeleteRequest(Guid ItemId, bool ConfirmPermanentDeletion);
