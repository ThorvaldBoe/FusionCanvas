namespace FusionCanvas.App.StageTools;

public sealed class NullMockupFilePicker : IMockupFilePicker
{
    public Task<string?> PickSaveFileAsync(string suggestedFileName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<string?>(null);
    }
}
