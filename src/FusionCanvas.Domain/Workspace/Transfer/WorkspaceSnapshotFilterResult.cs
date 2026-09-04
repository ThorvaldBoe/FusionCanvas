using FusionCanvas.Domain.Assets;

namespace FusionCanvas.Domain.Workspace.Transfer;

public sealed record WorkspaceSnapshotFilterResult(
    WorkspaceSnapshot Snapshot,
    IReadOnlyList<AssetLink> DroppedAssetLinks);
