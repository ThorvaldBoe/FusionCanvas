namespace FusionCanvas.Application.Stores;

public sealed record StoreSummary(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    StoreContext Context,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
