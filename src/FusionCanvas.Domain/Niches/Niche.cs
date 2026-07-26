using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Domain.Niches;

public sealed record Niche(
    Guid Id,
    Guid StoreId,
    string Name,
    string? Description,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string MetadataJson)
    : WorkspaceEntity(Id, Name, Description, IsArchived, CreatedAt, UpdatedAt, MetadataJson);
