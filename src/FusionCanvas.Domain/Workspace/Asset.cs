namespace FusionCanvas.Domain.Workspace;

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
