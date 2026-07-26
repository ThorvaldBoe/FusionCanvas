using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Domain.Prompts;

public sealed record Prompt(
    Guid Id,
    Guid StoreId,
    Guid? ItemId,
    string Name,
    string? Description,
    string Text,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string MetadataJson)
    : WorkspaceEntity(Id, Name, Description, IsArchived, CreatedAt, UpdatedAt, MetadataJson);
