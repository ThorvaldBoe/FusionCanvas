using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using FusionCanvas.App.Navigation;
using FusionCanvas.App.Settings;
using FusionCanvas.App.Stores;
using FusionCanvas.App.TermsConsent;
using FusionCanvas.App.Versioning;
using FusionCanvas.App.Views;
using FusionCanvas.Application.Settings;
using FusionCanvas.Application.TermsConsent;

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

            var splash = new SplashWindow(new AssemblyApplicationVersionProvider());
            _startupCancellation = new CancellationTokenSource();
            desktop.ShutdownRequested += OnShutdownRequested;
            desktop.MainWindow = splash;
            splash.Show();
            Dispatcher.UIThread.Post(() => _ = InitializeStartupAsync(desktop, splash));
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

    private async Task InitializeStartupAsync(
        IClassicDesktopStyleApplicationLifetime desktop,
        SplashWindow splash)
    {
        var startupCancellation = _startupCancellation;
        try
        {
            var settingsStore = AppSettingsFactory.CreateStore();
            var termsConsentService = new TermsConsentService(settingsStore);
            var load = await settingsStore.LoadAsync(startupCancellation?.Token ?? default).ConfigureAwait(true);
            if (TermsConsentStartupGate.RequiresConsent(load.Value))
            {
                var consentViewModel = new TermsConsentViewModel(
                    load.Value,
                    termsConsentService,
                    TermsConsentPolicyDocument.Load(),
                    startupCancellationToken: startupCancellation?.Token ?? default);
                var decision = new TaskCompletionSource<ApplicationSettings?>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                using var cancellationRegistration = startupCancellation?.Token.Register(
                    () => decision.TrySetResult(null));
                consentViewModel.Accepted += settings => decision.TrySetResult(settings);
                consentViewModel.QuitRequested += () => decision.TrySetResult(null);
                var consentWindow = new TermsConsentWindow { DataContext = consentViewModel };
                consentWindow.Show(splash);

                var acceptedSettings = await decision.Task.ConfigureAwait(true);
                if (acceptedSettings is null)
                {
                    await consentViewModel.WaitForPendingSaveAsync().ConfigureAwait(true);
                    consentWindow.AllowClose();
                    if (consentWindow.IsVisible)
                    {
                        consentWindow.Close();
                    }

                    splash.Close();
                    desktop.Shutdown();
                    return;
                }

                InitializeMainWindow(desktop, settingsStore, acceptedSettings, load.Warning);
                splash.Close();
                return;
            }

            InitializeMainWindow(desktop, settingsStore, load.Value, load.Warning);
            splash.Close();
        }
        catch (OperationCanceledException) when (startupCancellation?.IsCancellationRequested == true)
        {
            splash.Close();
        }
        catch (Exception exception)
        {
            Trace.TraceError("Application startup failed: {0}", exception);
            splash.Close();
            desktop.Shutdown();
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

    private void InitializeMainWindow(
        IClassicDesktopStyleApplicationLifetime desktop,
        IApplicationSettingsStore settingsStore,
        ApplicationSettings settings,
        string? loadWarning)
    {
        var startupCancellation = _startupCancellation;
        try
        {
            _services = AppServicesFactory.Create(
                settingsStore,
                startupCancellation?.Token ?? default,
                settings,
                loadWarning);
            var mainWindow = new MainWindow(_services, startupCancellation?.Token ?? default);
            mainWindow.Closing += OnWindowClosing;
            desktop.MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (OperationCanceledException) when (startupCancellation?.IsCancellationRequested == true)
        {
            _services?.Dispose();
            _services = null;
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
        window.IsEnabled = false;
        var mainWindowViewModel = window.DataContext as MainWindowViewModel;
        var storeManagement = window.DataContext switch
        {
            StoreManagementViewModel storeViewModel => storeViewModel,
            MainWindowViewModel mainViewModel => mainViewModel.StoreManagement,
            _ => null
        };
        var workspaceTree = mainWindowViewModel?.WorkspaceTree;
        _shutdownTask ??= CompleteWindowShutdownAsync(window, storeManagement, workspaceTree, mainWindowViewModel);
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
        WorkspaceTreeViewModel? workspaceTree,
        MainWindowViewModel? mainWindowViewModel)
    {
        var storeManagementDrained = true;
        var workspaceTreeDrained = true;
        var mainWindowViewModelDrained = true;
        var shouldCloseWindow = true;
        var allOwnersDrained = false;
        try
        {
            if (mainWindowViewModel is not null)
            {
                try
                {
                    await mainWindowViewModel.DisposeAsync()
                        .AsTask()
                        .WaitAsync(TimeSpan.FromSeconds(15));
                }
                catch (Exception exception)
                {
                    mainWindowViewModelDrained = false;
                    Trace.TraceError("Main window shutdown failed: {0}", exception);
                }
            }

            if (!mainWindowViewModelDrained)
            {
                shouldCloseWindow = false;
                Trace.TraceWarning("Keeping the window open because main-window commands are still running.");
            }

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

            allOwnersDrained = AreShutdownOwnersDrained(
                storeManagementDrained,
                workspaceTreeDrained,
                mainWindowViewModelDrained);
            if (!allOwnersDrained)
            {
                shouldCloseWindow = false;
                Trace.TraceWarning("Keeping the window open because one or more view models still have in-flight operations.");
            }

            if (_services is { } services)
            {
                if (allOwnersDrained)
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
                window.IsEnabled = true;
                _shutdownTask = null;
            }
        }
    }

    internal static bool AreShutdownOwnersDrained(
        bool storeManagementDrained,
        bool workspaceTreeDrained,
        bool mainWindowViewModelDrained) =>
        storeManagementDrained && workspaceTreeDrained && mainWindowViewModelDrained;

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
