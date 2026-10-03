namespace FusionCanvas.App.Assets;

public sealed class NullAssetFilePicker : IAssetFilePicker
{
    public Task<string?> PickImportFileAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);

    public Task<IReadOnlyList<string>> PickImportFilesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>([]);
}
