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
            return;
        }

        try
        {
            await clipboard.SetTextAsync(text).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
        }
        catch (PlatformNotSupportedException)
        {
        }
    }
}
