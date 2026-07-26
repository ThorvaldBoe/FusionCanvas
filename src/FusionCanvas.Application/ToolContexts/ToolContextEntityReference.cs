using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.ToolContexts;

public sealed record ToolContextEntityReference(
    WorkspaceEntityKind EntityKind,
    Guid EntityId,
    string Name)
{
    public Guid EntityId { get; } = EntityId == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(EntityId))
        : EntityId;

    public string Name { get; } = string.IsNullOrWhiteSpace(Name)
        ? throw new ArgumentException("Name is required.", nameof(Name))
        : Name;
}
