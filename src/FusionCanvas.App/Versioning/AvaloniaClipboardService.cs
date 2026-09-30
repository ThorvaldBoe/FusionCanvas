using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;

namespace FusionCanvas.App.Versioning;

public sealed class AvaloniaClipboardService : IClipboardService
{
    public static AvaloniaClipboardService Instance { get; } = new();

    public async Task SetTextAsync(string text)
    {
        var clipboard = (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
            ?.MainWindow?.Clipboard;
        if (clipboard is null)
        {
            throw new InvalidOperationException("The application clipboard is unavailable.");
        }

        await clipboard.SetTextAsync(text).ConfigureAwait(false);
    }
}
