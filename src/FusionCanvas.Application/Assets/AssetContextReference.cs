using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Assets;

public sealed record AssetContextReference(WorkspaceEntityKind Kind, Guid Id)
{
    public WorkspaceEntityKind Kind { get; } = Kind is
        WorkspaceEntityKind.Item or
        WorkspaceEntityKind.Niche or
        WorkspaceEntityKind.Group or
        WorkspaceEntityKind.Store
        ? Kind
        : throw new ArgumentException("An asset context must be a listing, niche, group, or store.", nameof(Kind));

    public Guid Id { get; } = Id == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(Id))
        : Id;
}
