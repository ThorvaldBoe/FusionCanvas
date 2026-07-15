namespace FusionCanvas.Domain.Workspace;

public static class WorkspaceDefaults
{
    public static Guid DefaultWorkspaceId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public const string DefaultWorkspaceName = "Personal";
}

public sealed record Workspace(
    Guid Id,
    string Name,
    string? Description,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string MetadataJson)
    : WorkspaceEntity(Id, Name, Description, IsArchived, CreatedAt, UpdatedAt, MetadataJson);

public sealed record Store : WorkspaceEntity
{
    public Store(
        Guid id,
        string name,
        string? description,
        bool isArchived,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string metadataJson)
        : this(id, WorkspaceDefaults.DefaultWorkspaceId, name, description, isArchived, createdAt, updatedAt, metadataJson)
    {
    }

    public Store(
        Guid id,
        Guid workspaceId,
        string name,
        string? description,
        bool isArchived,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string metadataJson)
        : base(id, name, description, isArchived, createdAt, updatedAt, metadataJson)
    {
        WorkspaceId = workspaceId == Guid.Empty
            ? throw new ArgumentException("Workspace identifier must not be empty.", nameof(workspaceId))
            : workspaceId;
    }

    public Guid WorkspaceId { get; init; }
}

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
    string MetadataJson)
    : WorkspaceEntity(Id, Name, Description, IsArchived, CreatedAt, UpdatedAt, MetadataJson);

public sealed record Listing(
    Guid Id,
    Guid StoreId,
    Guid? NicheId,
    Guid? GroupId,
    string Name,
    string? Description,
    ListingStatus Status,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string MetadataJson)
    : WorkspaceEntity(Id, Name, Description, IsArchived, CreatedAt, UpdatedAt, MetadataJson);

public sealed record Asset : WorkspaceEntity
{
    public Asset(
        Guid id,
        Guid storeId,
        string name,
        string? description,
        AssetKind kind,
        string workspaceRelativePath,
        string? originalSourcePath,
        bool isMissing,
        bool isArchived,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string metadataJson)
        : base(id, name, description, isArchived, createdAt, updatedAt, metadataJson)
    {
        StoreId = storeId;
        Kind = kind;
        WorkspaceRelativePath = WorkspaceFileReference.Normalize(workspaceRelativePath);
        OriginalSourcePath = string.IsNullOrWhiteSpace(originalSourcePath) ? null : originalSourcePath;
        IsMissing = isMissing;
    }

    public Guid StoreId { get; }

    public AssetKind Kind { get; }

    public string WorkspaceRelativePath { get; }

    public string? OriginalSourcePath { get; }

    public bool IsMissing { get; }
}

public sealed record Prompt(
    Guid Id,
    Guid StoreId,
    Guid? ListingId,
    string Name,
    string? Description,
    string Text,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string MetadataJson)
    : WorkspaceEntity(Id, Name, Description, IsArchived, CreatedAt, UpdatedAt, MetadataJson);

public sealed record Tag(
    Guid Id,
    Guid StoreId,
    string Name,
    string? Description,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string MetadataJson)
    : WorkspaceEntity(Id, Name, Description, IsArchived, CreatedAt, UpdatedAt, MetadataJson);
