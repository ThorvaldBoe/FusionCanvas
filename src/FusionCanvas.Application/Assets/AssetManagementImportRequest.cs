using FusionCanvas.Domain.Assets;

namespace FusionCanvas.Application.Assets;

public sealed record AssetManagementImportRequest(AssetContextReference Context, string SourcePath, AssetKind Kind);
