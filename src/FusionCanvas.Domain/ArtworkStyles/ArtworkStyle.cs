namespace FusionCanvas.Domain.ArtworkStyles;

public sealed record ArtworkStyle(
    Guid Id,
    Guid WorkspaceId,
    string Title,
    string Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
