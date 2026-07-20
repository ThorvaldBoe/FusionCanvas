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
        string metadataJson,
        Guid? defaultNicheId = null)
        : this(id, WorkspaceDefaults.DefaultWorkspaceId, name, description, isArchived, createdAt, updatedAt, metadataJson, defaultNicheId)
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
        string metadataJson,
        Guid? defaultNicheId = null)
        : base(id, name, description, isArchived, createdAt, updatedAt, metadataJson)
    {
        WorkspaceId = workspaceId == Guid.Empty
            ? throw new ArgumentException("Workspace identifier must not be empty.", nameof(workspaceId))
            : workspaceId;
        DefaultNicheId = defaultNicheId == Guid.Empty
            ? throw new ArgumentException("Default niche identifier must not be empty.", nameof(defaultNicheId))
            : defaultNicheId;
    }

    public Guid WorkspaceId { get; init; }

    public Guid? DefaultNicheId { get; init; }
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
    string MetadataJson,
    int SortOrder = 0)
    : WorkspaceEntity(Id, Name, Description, IsArchived, CreatedAt, UpdatedAt, MetadataJson);

public sealed record Listing(
    Guid Id,
    Guid StoreId,
    Guid? NicheId,
    Guid? GroupId,
    string Name,
    string? Description,
    ListingStatus Status,
    WorkflowStage Stage,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string MetadataJson)
    : WorkspaceEntity(Id, Name, Description, IsArchived, CreatedAt, UpdatedAt, MetadataJson);

public sealed record Concept(
    Guid Id,
    Guid StoreId,
    Guid ListingId,
    string Name,
    string? Description,
    string? Idea,
    string? Phrase,
    string? GraphicDirection,
    string? AudienceReaction,
    string? Risks,
    string? QualityNotes,
    string? ScoreJson,
    ConceptLifecycle Lifecycle,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string MetadataJson)
    : WorkspaceEntity(Id, Name, Description, IsArchived, CreatedAt, UpdatedAt, MetadataJson);

public sealed record Design(
    Guid Id,
    Guid StoreId,
    Guid ListingId,
    Guid? ImplementedConceptId,
    string Name,
    string? Description,
    string? SourceMethod,
    string? Notes,
    DesignApprovalState ApprovalState,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string MetadataJson)
    : WorkspaceEntity(Id, Name, Description, IsArchived, CreatedAt, UpdatedAt, MetadataJson);

public sealed record Mockup(
    Guid Id,
    Guid StoreId,
    Guid ListingId,
    Guid? DesignId,
    string Name,
    string? Description,
    string? SourceMethod,
    string? ProductType,
    string? VendorProduct,
    string? Template,
    string? ColorVariant,
    string? View,
    string? Notes,
    string? IntendedMarketplaceUse,
    string? RegenerationMetadataJson,
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

public sealed record Tag : WorkspaceEntity
{
    public Tag(
        Guid id,
        Guid storeId,
        string name,
        string? description,
        bool isArchived,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string metadataJson,
        string? color = null)
        : base(id, name, description, isArchived, createdAt, updatedAt, metadataJson)
    {
        StoreId = storeId;
        Color = color;
    }

    public Guid StoreId { get; init; }

    public string? Color
    {
        get => _color;
        init => _color = NormalizeColor(value);
    }

    private readonly string? _color;

    public static string? NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return null;
        }

        var trimmed = color.Trim();
        if (!trimmed.StartsWith('#'))
        {
            throw new ArgumentException("Color must be a hex color starting with '#'.", nameof(color));
        }

        var hex = trimmed[1..];
        if (hex.Length == 3 && IsHexHex(hex))
        {
            return $"#{new string(hex[0], 2)}{new string(hex[1], 2)}{new string(hex[2], 2)}".ToUpperInvariant();
        }

        if (hex.Length == 6 && IsHexHex(hex))
        {
            return $"#{hex.ToUpperInvariant()}";
        }

        throw new ArgumentException("Color must be a valid hex color (#RGB or #RRGGBB).", nameof(color));
    }

    private static bool IsHexHex(string value)
    {
        foreach (var c in value)
        {
            if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
            {
                return false;
            }
        }

        return true;
    }
}
