namespace FusionCanvas.App.Assets;

public interface IAssetFilePicker
{
    Task<string?> PickImportFileAsync(CancellationToken cancellationToken = default);
}

public sealed class NullAssetFilePicker : IAssetFilePicker
{
    public Task<string?> PickImportFileAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}
