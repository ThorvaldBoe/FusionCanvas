using Avalonia.Platform.Storage;

namespace FusionCanvas.App.Assets;

public sealed class AvaloniaAssetFilePicker : IAssetFilePicker
{
    private static readonly IReadOnlyList<FilePickerFileType> CreativeAssetFilters =
    [
        new("Creative assets")
        {
            Patterns = ["*.afdesign", "*.psd", "*.ai", "*.eps", "*.png", "*.jpg", "*.jpeg", "*.webp", "*.tif", "*.tiff", "*.svg", "*.ttf", "*.otf", "*.brush"]
        },
        FilePickerFileTypes.All
    ];

    private static readonly IReadOnlyList<FilePickerFileType> RasterImageFilters =
    [
        new("Raster images")
        {
            Patterns = ["*.png", "*.jpg", "*.jpeg"]
        }
    ];

    private readonly IStorageProvider _storageProvider;

    public AvaloniaAssetFilePicker(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
    }

    public async Task<string?> PickImportFileAsync(CancellationToken cancellationToken = default)
    {
        if (!_storageProvider.CanOpen)
        {
            return null;
        }

        var files = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import asset",
            AllowMultiple = false,
            FileTypeFilter = CreativeAssetFilters
        });

        return files is { Count: > 0 } ? files[0].TryGetLocalPath() : null;
    }

    public async Task<IReadOnlyList<string>> PickImportFilesAsync(CancellationToken cancellationToken = default)
    {
        if (!_storageProvider.CanOpen)
        {
            return [];
        }

        var files = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Upload mockup source images",
            AllowMultiple = true,
            FileTypeFilter = RasterImageFilters
        });

        return files
            .Select(file => file.TryGetLocalPath())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path!)
            .ToArray();
    }
}
