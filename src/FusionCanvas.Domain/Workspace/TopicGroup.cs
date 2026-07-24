namespace FusionCanvas.Domain.Workspace;

public sealed record TopicGroup(
    Guid Id,
    Guid StoreId,
    Guid? NicheId,
    Guid? ParentGroupId,
    string Name,
    string? Description,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string MetadataJson,
    int SortOrder = 0)
    : WorkspaceEntity(Id, Name, Description, IsArchived, CreatedAt, UpdatedAt, MetadataJson);
