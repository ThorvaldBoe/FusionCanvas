namespace FusionCanvas.App.Workspace;

public interface IWorkspacePackagePicker
{
    Task<string?> PickExportDestinationAsync(
        string suggestedFileName,
        CancellationToken cancellationToken = default);

    Task<string?> PickImportPackageAsync(CancellationToken cancellationToken = default);
}

public sealed class NullWorkspacePackagePicker : IWorkspacePackagePicker
{
    public Task<string?> PickExportDestinationAsync(string suggestedFileName, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);

    public Task<string?> PickImportPackageAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}
