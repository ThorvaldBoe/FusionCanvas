using Avalonia.Platform.Storage;

namespace FusionCanvas.App.StageTools;

public sealed class AvaloniaMockupFilePicker(IStorageProvider storageProvider) : IMockupFilePicker
{
    private static readonly IReadOnlyList<FilePickerFileType> ImageFilters =
    [
        new("PNG images") { Patterns = ["*.png"] },
        FilePickerFileTypes.All
    ];

    private readonly IStorageProvider _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));

    public async Task<string?> PickSaveFileAsync(string suggestedFileName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_storageProvider.CanSave)
        {
            return null;
        }

        var file = await _storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save mockup copy",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "png",
            FileTypeChoices = ImageFilters
        });
        cancellationToken.ThrowIfCancellationRequested();
        return file?.TryGetLocalPath();
    }
}
