using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using FusionCanvas.App.Settings;
using FusionCanvas.App.Stores;
using FusionCanvas.App.Views;

namespace FusionCanvas.App;

public partial class App : Avalonia.Application
{
    private AppServices? _services;
    private bool _allowFinalClose;
    private Task? _shutdownTask;

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
            storeEditor.Closing += OnWindowClosing;
            desktop.MainWindow = storeEditor;
            storeEditor.Show();
            return;
        }

        mainWindow.Closing += OnWindowClosing;
        desktop.MainWindow = mainWindow;
        mainWindow.Show();
    }

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowFinalClose || e.Cancel || sender is not Window window)
        {
            return;
        }

        e.Cancel = true;
        var storeManagement = window.DataContext switch
        {
            StoreManagementViewModel storeViewModel => storeViewModel,
            MainWindowViewModel mainViewModel => mainViewModel.StoreManagement,
            _ => null
        };
        _shutdownTask ??= CompleteWindowShutdownAsync(window, storeManagement);
        try
        {
            await _shutdownTask;
        }
        catch (Exception exception)
        {
            Trace.TraceError("Application shutdown failed: {0}", exception);
        }
    }

    private async Task CompleteWindowShutdownAsync(Window window, StoreManagementViewModel? storeManagement)
    {
        var storeManagementDrained = true;
        try
        {
            if (storeManagement is not null)
            {
                try
                {
                    await storeManagement.DisposeAsync();
                }
                catch (Exception exception)
                {
                    storeManagementDrained = false;
                    Trace.TraceError("Store management shutdown failed: {0}", exception);
                }

                storeManagementDrained &= !storeManagement.IsBusy;
            }

            if (_services is { } services)
            {
                if (storeManagementDrained)
                {
                    try
                    {
                        await services.FlushAsync();
                    }
                    catch (Exception exception)
                    {
                        Trace.TraceError("Application service flush failed: {0}", exception);
                    }
                    finally
                    {
                        try
                        {
                            services.Dispose();
                        }
                        catch (Exception exception)
                        {
                            Trace.TraceError("Application service disposal failed: {0}", exception);
                        }
                        finally
                        {
                            _services = null;
                        }
                    }
                }
                else
                {
                    Trace.TraceWarning(
                        "Skipping service flush and disposal because StoreManagementViewModel still has in-flight operations.");
                }
            }
        }
        finally
        {
            _allowFinalClose = true;
            window.Close();
        }
    }

    private void InitializeMainWindow(
        IClassicDesktopStyleApplicationLifetime desktop,
        SplashWindow splash)
    {
        RunWithSplashCleanup(splash, () => InitializeMainWindow(desktop));
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
