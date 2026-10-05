namespace FusionCanvas.App.StageTools;

public interface IMockupFilePicker
{
    Task<string?> PickSaveFileAsync(string suggestedFileName, CancellationToken cancellationToken = default);
}
