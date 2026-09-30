using Avalonia.Platform.Storage;

namespace FusionCanvas.App.Settings;

public sealed class AvaloniaTelemetryExportFilePicker(IStorageProvider storageProvider) : ITelemetryExportFilePicker
{
    private readonly IStorageProvider _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));

    public async Task<Stream?> OpenExportAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_storageProvider.CanSave)
        {
            return null;
        }

        var file = await _storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export telemetry",
            SuggestedFileName = "fusioncanvas-telemetry.json",
            DefaultExtension = "json",
            FileTypeChoices = [new FilePickerFileType("JSON") { Patterns = ["*.json"] }]
        });
        cancellationToken.ThrowIfCancellationRequested();

        return file is null ? null : await file.OpenWriteAsync();
    }
}
