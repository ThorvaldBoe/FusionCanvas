using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Navigation;

public sealed record NavigationTarget(NavigationTargetKind Kind, WorkspaceEntityKind EntityKind, Guid EntityId)
{
    public WorkspaceEntityKind EntityKind { get; } = ValidateEntityKind(Kind, EntityKind);

    public Guid EntityId { get; } = EntityId == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(EntityId))
        : EntityId;

    private static WorkspaceEntityKind ValidateEntityKind(NavigationTargetKind kind, WorkspaceEntityKind entityKind) =>
        kind switch
        {
            NavigationTargetKind.Store when entityKind == WorkspaceEntityKind.Store => entityKind,
            NavigationTargetKind.Topic when entityKind is WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group => entityKind,
            NavigationTargetKind.Item when entityKind == WorkspaceEntityKind.Item => entityKind,
            _ => throw new ArgumentException("Navigation target kind does not match the workspace entity kind.", nameof(entityKind))
        };
}
