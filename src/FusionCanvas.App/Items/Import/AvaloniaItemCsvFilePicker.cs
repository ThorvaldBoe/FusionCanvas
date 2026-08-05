using Avalonia.Platform.Storage;

namespace FusionCanvas.App.Items.Import;

public sealed class AvaloniaItemCsvFilePicker(IStorageProvider storageProvider)
    : IItemCsvFilePicker
{
    private static readonly IReadOnlyList<FilePickerFileType> CsvFilters =
    [
        new("CSV files") { Patterns = ["*.csv"] }
    ];

    private readonly IStorageProvider _storageProvider =
        storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));

    public async Task<Stream?> OpenImportAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_storageProvider.CanOpen)
        {
            return null;
        }

        var files = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import items",
            AllowMultiple = false,
            FileTypeFilter = CsvFilters
        });
        cancellationToken.ThrowIfCancellationRequested();

        return files is { Count: > 0 }
            ? await files[0].OpenReadAsync()
            : null;
    }

    public async Task<Stream?> OpenExportAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_storageProvider.CanSave)
        {
            return null;
        }

        var file = await _storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export sample",
            SuggestedFileName = "items-sample.csv",
            DefaultExtension = "csv",
            FileTypeChoices = CsvFilters
        });
        cancellationToken.ThrowIfCancellationRequested();

        return file is null ? null : await file.OpenWriteAsync();
    }
}