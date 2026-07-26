namespace FusionCanvas.Application.Assets;

public sealed record AssetManagementRemoveRequest(Guid AssetId, bool ConfirmPermanentRemoval);
