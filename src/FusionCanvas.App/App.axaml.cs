using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using FusionCanvas.App.Settings;
using FusionCanvas.App.Stores;
using FusionCanvas.App.Views;
using Avalonia.Controls;

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
            if (IsUiTestMode())
            {
                InitializeMainWindow(desktop);
                base.OnFrameworkInitializationCompleted();
                return;
            }

            var splash = new SplashWindow();
            desktop.MainWindow = splash;
            splash.Show();
            Dispatcher.UIThread.Post(() => InitializeMainWindow(desktop, splash));
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void InitializeMainWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        _services = AppServicesFactory.Create();
        var mainWindow = new MainWindow(_services);
        if (IsUiTestMode())
        {
            var storeEditor = new StoreEditorWindow
            {
                DataContext = ((MainWindowViewModel)mainWindow.DataContext!).StoreManagement
            };
            storeEditor.Closing += (_, _) => DisposeServices();
            desktop.MainWindow = storeEditor;
            storeEditor.Show();
            return;
        }

        mainWindow.Closing += (_, _) =>
            DisposeServices();
        desktop.MainWindow = mainWindow;
        mainWindow.Show();
    }

    private void DisposeServices()
    {
        _services?.FlushAsync().GetAwaiter().GetResult();
        _services?.Dispose();
    }

    private void InitializeMainWindow(
        IClassicDesktopStyleApplicationLifetime desktop,
        SplashWindow splash)
    {
        try
        {
            InitializeMainWindow(desktop);
        }
        catch (Exception exception)
        {
            splash.Content = new TextBlock
            {
                Text = $"FusionCanvas could not start.\n\n{exception.Message}",
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(24)
            };
            splash.Width = 520;
            splash.Height = 220;
            splash.Show();
            return;
        }

        splash.Close();
    }

    private static bool IsUiTestMode() =>
        string.Equals(
            Environment.GetEnvironmentVariable(Program.UiTestModeEnvironmentVariable),
            "1",
            StringComparison.Ordinal);

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
