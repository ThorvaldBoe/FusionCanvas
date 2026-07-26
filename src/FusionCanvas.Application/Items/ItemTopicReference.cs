using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Items;

public sealed record ItemTopicReference(WorkspaceEntityKind Kind, Guid Id)
{
    public WorkspaceEntityKind Kind { get; } = Kind is WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group
        ? Kind
        : throw new ArgumentException("A listing topic must be a niche or group.", nameof(Kind));

    public Guid Id { get; } = Id == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(Id))
        : Id;
}
