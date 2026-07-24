namespace FusionCanvas.Application.Workspaces;

public sealed record WorkspaceSummary(
    Guid Id,
    string Name,
    WorkspaceContext Context,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
