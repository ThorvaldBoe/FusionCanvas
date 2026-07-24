using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Domain.Assets;

public sealed record AssetLink(Guid AssetId, WorkspaceEntityKind EntityKind, Guid EntityId)
{
    public Guid AssetId { get; } = AssetId == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(AssetId))
        : AssetId;

    public Guid EntityId { get; } = EntityId == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(EntityId))
        : EntityId;
}
