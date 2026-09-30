using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using FusionCanvas.App.Navigation;
using FusionCanvas.App.Settings;
using FusionCanvas.App.Stores;
using FusionCanvas.App.Views;

namespace FusionCanvas.App;

public partial class App : Avalonia.Application
{
    private AppServices? _services;
    private bool _allowFinalClose;
    private Task? _shutdownTask;
    private CancellationTokenSource? _startupCancellation;

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
            _startupCancellation = new CancellationTokenSource();
            desktop.ShutdownRequested += OnShutdownRequested;
            desktop.MainWindow = splash;
            splash.Show();
            Dispatcher.UIThread.Post(() => InitializeMainWindow(desktop, splash));
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void InitializeMainWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var startupCancellation = _startupCancellation;
        try
        {
            _services = AppServicesFactory.Create(startupCancellation?.Token ?? default);
            var mainWindow = new MainWindow(_services, startupCancellation?.Token ?? default);
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
        catch (OperationCanceledException) when (startupCancellation?.IsCancellationRequested == true)
        {
            _services?.Dispose();
            _services = null;
            return;
        }
        finally
        {
            if (ReferenceEquals(_startupCancellation, startupCancellation))
            {
                _startupCancellation = null;
                desktop.ShutdownRequested -= OnShutdownRequested;
                startupCancellation?.Dispose();
            }
        }
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e) =>
        _startupCancellation?.Cancel();

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
        var workspaceTree = (window.DataContext as MainWindowViewModel)?.WorkspaceTree;
        _shutdownTask ??= CompleteWindowShutdownAsync(window, storeManagement, workspaceTree);
        try
        {
            await _shutdownTask;
        }
        catch (Exception exception)
        {
            Trace.TraceError("Application shutdown failed: {0}", exception);
        }
    }

    private async Task CompleteWindowShutdownAsync(
        Window window,
        StoreManagementViewModel? storeManagement,
        WorkspaceTreeViewModel? workspaceTree)
    {
        var storeManagementDrained = true;
        var workspaceTreeDrained = true;
        var shouldCloseWindow = true;
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

            if (workspaceTree is not null)
            {
                try
                {
                    await workspaceTree.DisposeAsync();
                }
                catch (Exception exception)
                {
                    Trace.TraceError("Workspace tree shutdown failed: {0}", exception);
                    try
                    {
                        await workspaceTree.WaitForCommandTasksAsync();
                    }
                    catch (Exception drainException)
                    {
                        workspaceTreeDrained = false;
                        Trace.TraceError("Workspace tree command drain failed: {0}", drainException);
                    }
                }

                workspaceTreeDrained &= workspaceTree.PendingCommandCount == 0;
                if (!workspaceTreeDrained)
                {
                    shouldCloseWindow = false;
                    Trace.TraceWarning("Keeping the window open because workspace-tree commands are still running.");
                }
            }

            if (_services is { } services)
            {
                if (storeManagementDrained && workspaceTreeDrained)
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
                        "Skipping service flush and disposal because a view model still has in-flight operations.");
                }
            }
        }
        finally
        {
            if (shouldCloseWindow)
            {
                _allowFinalClose = true;
                window.Close();
            }
            else
            {
                _shutdownTask = null;
            }
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
