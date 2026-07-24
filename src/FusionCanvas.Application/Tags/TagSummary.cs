namespace FusionCanvas.Application.Tags;

public sealed record TagSummary(
    Guid Id,
    Guid StoreId,
    string Name,
    string? Description,
    string? Color,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
