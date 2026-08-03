namespace FusionCanvas.App.Versioning;

public sealed class NullClipboardService : IClipboardService
{
    public static NullClipboardService Instance { get; } = new();

    public Task SetTextAsync(string text) => Task.CompletedTask;
}
