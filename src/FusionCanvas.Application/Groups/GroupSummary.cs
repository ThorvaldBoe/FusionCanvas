namespace FusionCanvas.Application.Groups;

public sealed record GroupSummary(
    Guid Id,
    Guid StoreId,
    Guid NicheId,
    GroupParentReference Parent,
    string Name,
    GroupContext Context,
    bool IsArchived,
    bool IsEffectivelyActive,
    IReadOnlyList<Guid> Path,
    string DisplayPath,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int SortOrder);
