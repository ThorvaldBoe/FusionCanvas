using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FusionCanvas.App.Settings;
using FusionCanvas.App.Views;

namespace FusionCanvas.App;

public partial class App : Avalonia.Application
{
    private AppServices? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _services = AppServicesFactory.Create();
            var mainWindow = new MainWindow(_services.Settings);
            mainWindow.Closing += (_, _) =>
            {
                _services?.FlushAsync().GetAwaiter().GetResult();
                _services?.Dispose();
            };
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
