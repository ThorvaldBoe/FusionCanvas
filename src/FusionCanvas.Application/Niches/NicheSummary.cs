namespace FusionCanvas.Application.Niches;

public sealed record NicheSummary(
    Guid Id,
    Guid StoreId,
    string Name,
    NicheContext Context,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
