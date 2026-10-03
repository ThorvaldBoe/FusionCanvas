namespace FusionCanvas.App.Assets;

public interface IAssetFilePicker
{
    Task<string?> PickImportFileAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> PickImportFilesAsync(CancellationToken cancellationToken = default);
}
