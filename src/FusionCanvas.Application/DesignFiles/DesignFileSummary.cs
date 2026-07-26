namespace FusionCanvas.Application.DesignFiles;

public sealed record DesignFileSummary(
    Guid AssetId,
    string Name,
    string WorkspaceRelativePath,
    bool IsMissing,
    bool CanPreview,
    bool CanExport);
