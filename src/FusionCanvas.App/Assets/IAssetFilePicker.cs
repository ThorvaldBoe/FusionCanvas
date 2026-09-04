namespace FusionCanvas.App.Assets;

public interface IAssetFilePicker
{
    Task<string?> PickImportFileAsync(CancellationToken cancellationToken = default);
}
