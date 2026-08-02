using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
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
            var splash = new SplashWindow();
            desktop.MainWindow = splash;
            splash.Show();
            Dispatcher.UIThread.Post(() => InitializeMainWindow(desktop, splash));
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void InitializeMainWindow(
        IClassicDesktopStyleApplicationLifetime desktop,
        SplashWindow splash)
    {
        RunWithSplashCleanup(splash, () =>
        {
            _services = AppServicesFactory.Create();
            var mainWindow = new MainWindow(_services);
            mainWindow.Closing += (_, _) =>
            {
                _services?.FlushAsync().GetAwaiter().GetResult();
                _services?.Dispose();
            };
            desktop.MainWindow = mainWindow;
            mainWindow.Show();
        });
    }

    internal static void RunWithSplashCleanup(SplashWindow splash, Action startup)
    {
        ArgumentNullException.ThrowIfNull(splash);
        ArgumentNullException.ThrowIfNull(startup);

        try
        {
            startup();
        }
        finally
        {
            splash.Close();
        }
    }
}
