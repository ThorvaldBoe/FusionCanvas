namespace FusionCanvas.App.Workspace;

public interface IWorkspacePackagePicker
{
    Task<string?> PickExportDestinationAsync(
        string suggestedFileName,
        CancellationToken cancellationToken = default);

    Task<string?> PickImportPackageAsync(CancellationToken cancellationToken = default);
}
