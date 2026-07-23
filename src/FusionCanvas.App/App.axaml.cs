using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FusionCanvas.App.Settings;
using FusionCanvas.App.Views;

namespace FusionCanvas.App;

public partial class App : Avalonia.Application
{
    private SettingsViewModel? _settings;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _settings = AppSettingsFactory.LoadInitialState();
            var mainWindow = new MainWindow(_settings);
            mainWindow.Closing += (_, _) => _settings?.FlushAsync().GetAwaiter().GetResult();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
