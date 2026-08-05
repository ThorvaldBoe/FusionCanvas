using Avalonia.Platform.Storage;

namespace FusionCanvas.App.Items;

public sealed class AvaloniaItemCsvFilePicker(IStorageProvider storageProvider)
    : IItemCsvFilePicker
{
    private static readonly IReadOnlyList<FilePickerFileType> CsvFilters =
    [
        new("CSV files") { Patterns = ["*.csv"] }
    ];

    private readonly IStorageProvider _storageProvider =
        storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));

    public async Task<Stream?> OpenExportAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_storageProvider.CanSave)
        {
            return null;
        }

        var file = await _storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export items to CSV",
            SuggestedFileName = "items.csv",
            DefaultExtension = "csv",
            FileTypeChoices = CsvFilters
        });
        cancellationToken.ThrowIfCancellationRequested();

        return file is null ? null : await file.OpenWriteAsync();
    }
}
