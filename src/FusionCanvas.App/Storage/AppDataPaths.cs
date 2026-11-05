namespace FusionCanvas.App.Storage;

internal sealed record AppDataPaths(
    string Root,
    string WorkspaceDatabase,
    string WorkspaceFiles,
    string Settings);
