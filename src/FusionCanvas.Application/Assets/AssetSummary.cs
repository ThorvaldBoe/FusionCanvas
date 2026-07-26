using FusionCanvas.Domain.Assets;

namespace FusionCanvas.Application.Assets;

public sealed record AssetSummary(
    Guid Id,
    Guid StoreId,
    string Name,
    AssetKind Kind,
    string WorkspaceRelativePath,
    string? OriginalSourcePath,
    string ManagedFileName,
    bool IsMissing,
    string? ContextLabel,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
