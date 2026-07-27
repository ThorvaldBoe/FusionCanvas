namespace FusionCanvas.Application.Workspaces.Transfer;

public sealed record WorkspacePackageManifest(
    int FormatVersion,
    int SchemaVersion,
    string AppVersion,
    Guid WorkspaceId,
    string WorkspaceName,
    DateTimeOffset ExportedAtUtc,
    IReadOnlyDictionary<string, int> EntityCounts,
    IReadOnlyList<WorkspacePackageFile> Files,
    IReadOnlyList<string> MissingFiles,
    int DroppedLinkCount);
