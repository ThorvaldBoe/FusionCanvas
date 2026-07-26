using FusionCanvas.Domain.Assets;

namespace FusionCanvas.Application.Assets;

public sealed record AssetManagementRelabelRequest(Guid AssetId, AssetKind Kind);
