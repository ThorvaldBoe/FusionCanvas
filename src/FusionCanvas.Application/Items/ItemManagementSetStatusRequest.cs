using FusionCanvas.Domain.Items;

namespace FusionCanvas.Application.Items;

public sealed record ItemManagementSetStatusRequest(Guid ItemId, ItemStatus Status, bool ConfirmProtectedTransition = false);
