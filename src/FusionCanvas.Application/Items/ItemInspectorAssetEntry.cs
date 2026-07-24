using FusionCanvas.Domain.Assets;

namespace FusionCanvas.Application.Items;

public sealed record ItemInspectorAssetEntry(Guid Id, string Name, AssetKind Kind, bool IsMissing);
