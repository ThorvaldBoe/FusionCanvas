using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Domain.Navigation;

public sealed record NavigationTopicReference(WorkspaceEntityKind EntityKind, Guid EntityId)
{
    public WorkspaceEntityKind EntityKind { get; } = EntityKind is WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group
        ? EntityKind
        : throw new ArgumentException("Navigation topic must reference a niche or group.", nameof(EntityKind));

    public Guid EntityId { get; } = EntityId == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(EntityId))
        : EntityId;
}
