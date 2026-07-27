namespace FusionCanvas.Application.Workspaces.Transfer;

public sealed record WorkspaceTransferSummary(
    IReadOnlyDictionary<string, int> EntityCounts,
    int WrittenFiles,
    int RestoredFiles,
    int SkippedExistingFiles,
    IReadOnlyList<string> MissingFiles,
    IReadOnlyList<string> SkippedUnsupportedFiles,
    int DroppedLinkCount,
    string OriginalWorkspaceName,
    string FinalWorkspaceName,
    IReadOnlyList<string> Warnings);
