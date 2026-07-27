using Avalonia.Platform.Storage;

namespace FusionCanvas.App.Workspace;

public sealed class AvaloniaWorkspacePackagePicker(IStorageProvider storageProvider) : IWorkspacePackagePicker
{
    private static readonly FilePickerFileType WorkspacePackageType = new("FusionCanvas workspace")
    {
        Patterns = ["*.fcworkspace"]
    };

    public async Task<string?> PickExportDestinationAsync(
        string suggestedFileName,
        CancellationToken cancellationToken = default)
    {
        if (!storageProvider.CanSave)
        {
            return null;
        }

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export workspace",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "fcworkspace",
            FileTypeChoices = [WorkspacePackageType]
        });
        return file?.TryGetLocalPath();
    }

    public async Task<string?> PickImportPackageAsync(CancellationToken cancellationToken = default)
    {
        if (!storageProvider.CanOpen)
        {
            return null;
        }

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import workspace",
            AllowMultiple = false,
            FileTypeFilter = [WorkspacePackageType]
        });
        return files is { Count: > 0 } ? files[0].TryGetLocalPath() : null;
    }
}
