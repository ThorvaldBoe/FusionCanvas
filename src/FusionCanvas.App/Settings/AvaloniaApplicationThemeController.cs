using Avalonia.Styling;
using Avalonia.Threading;

namespace FusionCanvas.App.Settings;

public sealed class AvaloniaApplicationThemeController : IApplicationThemeController
{
    public void ApplyDarkMode(bool darkMode)
    {
        if (Avalonia.Application.Current is { } app && Dispatcher.UIThread.CheckAccess())
        {
            app.RequestedThemeVariant = darkMode ? ThemeVariant.Dark : ThemeVariant.Light;
        }
    }
}

