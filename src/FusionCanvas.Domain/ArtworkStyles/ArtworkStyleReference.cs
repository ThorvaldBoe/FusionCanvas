namespace FusionCanvas.Domain.ArtworkStyles;

public sealed record ArtworkStyleReference(
    Guid Id,
    Guid StyleId,
    string WorkspaceRelativePath,
    string MediaType,
    int SortOrder,
    DateTimeOffset CreatedAt);
