namespace FusionCanvas.Domain.Workspace;

public sealed record Item(
    Guid Id,
    Guid StoreId,
    Guid? NicheId,
    Guid? GroupId,
    string Name,
    string? Description,
    ItemStatus Status,
    WorkflowStage Stage,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string MetadataJson)
    : WorkspaceEntity(Id, Name, Description, IsArchived, CreatedAt, UpdatedAt, MetadataJson);
