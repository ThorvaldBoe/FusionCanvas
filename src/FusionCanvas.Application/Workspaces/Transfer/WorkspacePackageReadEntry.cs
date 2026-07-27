namespace FusionCanvas.Application.Workspaces.Transfer;

public sealed record WorkspacePackageReadEntry(
    string WorkspaceRelativePath,
    long Size,
    Func<CancellationToken, Task<Stream>> OpenReadAsync);
