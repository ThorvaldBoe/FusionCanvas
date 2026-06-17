namespace FusionCanvas.Domain.Workspace;

public abstract record WorkspaceEntity(
    Guid Id,
    string Name,
    string? Description,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string MetadataJson);
