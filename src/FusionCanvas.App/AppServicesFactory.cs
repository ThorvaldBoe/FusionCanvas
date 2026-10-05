using FusionCanvas.App.Settings;
using FusionCanvas.App.Versioning;
using FusionCanvas.Application.AI;
using FusionCanvas.Integration.AI;
using FusionCanvas.Application.Telemetry;
using FusionCanvas.App.Workspace;
using FusionCanvas.Integration.Persistence;
using FusionCanvas.Application.Settings;
using FusionCanvas.Application.TermsConsent;
using FusionCanvas.Application.Updates;
using FusionCanvas.Integration.Updates;
using FusionCanvas.App.Updates;
using System.Runtime.InteropServices;

namespace FusionCanvas.App;

public static class AppServicesFactory
{
    public static AppServices Create()
        => Create(AppSettingsFactory.CreateStore());

    public static AppServices Create(CancellationToken cancellationToken, Action? requestShutdown = null) =>
        Create(AppSettingsFactory.CreateStore(), cancellationToken, requestShutdown: requestShutdown);

    public static AppServices Create(
        FusionCanvas.Application.Settings.IApplicationSettingsStore settingsStore,
        CancellationToken cancellationToken = default,
        ApplicationSettings? initialSettings = null,
        string? loadWarning = null,
        Action? requestShutdown = null)
    {
        ArgumentNullException.ThrowIfNull(settingsStore);
        var load = initialSettings is null
            ? StartupTaskRunner.Run(token => settingsStore.LoadAsync(token), cancellationToken)
            : new ApplicationSettingsLoadResult(initialSettings, UsedDefault: false, loadWarning);
        var settingsPath =
            (settingsStore as FusionCanvas.Integration.Settings.JsonApplicationSettingsStore)?.SettingsPath;
        var settingsDirectory = settingsPath is null
            ? AppContext.BaseDirectory
            : Path.GetDirectoryName(settingsPath) ?? AppContext.BaseDirectory;

        var credentials = new NativeAiCredentialStore();
        var telemetryContext = new TelemetryWorkspaceContext();
        var telemetry = new WorkspaceTelemetryService(
            new SqliteTelemetryStore(AppWorkspaceFactory.ResolveDefaultDatabasePath()),
            telemetryContext);
        var catalogCache = new JsonAiModelCatalogCache(Path.Combine(settingsDirectory, "ai-cache"));
        var httpClient = new HttpClient
        {
            BaseAddress = OpenRouterClient.DefaultBaseAddress,
            Timeout = Timeout.InfiniteTimeSpan
        };
        var openRouter = new OpenRouterClient(httpClient, telemetry);
        var aiSettings = new AiSettingsViewModel(
            load.Value.Ai,
            credentials,
            openRouter,
            openRouter,
            catalogCache);
        var settings = new SettingsViewModel(
            settingsStore,
            new AvaloniaApplicationThemeController(),
            load.Value,
            load.Warning,
            aiSettings,
            new AssemblyApplicationVersionProvider(),
            AvaloniaClipboardService.Instance,
            telemetry,
            telemetryContext,
            new TermsConsentService(settingsStore));
        var updateHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        updateHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("FusionCanvas-UpdateClient/1.0");
        settings.ReplaceUpdateService(CreateUpdateService(updateHttpClient, settings, requestShutdown));
        var textService = new AiTextGenerationService(aiSettings, credentials, catalogCache, openRouter);
        var services = new AppServices(
            httpClient,
            settingsStore,
            settings,
            textService,
            openRouter,
            new FusionCanvas.Integration.Items.ItemCsvCodec(),
            new FusionCanvas.Integration.Items.Import.ItemCsvCodec(),
            telemetry,
            updateHttpClient);
        var printifyClient = FusionCanvas.Integration.Stores.Printify.PrintifyCredentialVerifier.CreateHttpClient();
        var printifyCatalogClient = FusionCanvas.Integration.Stores.Printify.PrintifyCatalogClient.CreateHttpClient();
        services.ConfigurePrintify(printifyClient,
            new FusionCanvas.Integration.Stores.Printify.NativeStorePrintifyCredentialStore(),
            new FusionCanvas.Integration.Stores.Printify.PrintifyCredentialVerifier(printifyClient),
            new FusionCanvas.Integration.Stores.Printify.PrintifyCatalogClient(printifyCatalogClient, telemetry),
            printifyCatalogClient);
        return services;
    }

    private static IUpdateService CreateUpdateService(
        HttpClient updateHttpClient,
        SettingsViewModel settings,
        Action? requestShutdown)
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
        {
            return new DisabledUpdateService();
        }

        return new UpdateService(
            new AssemblyApplicationVersionProvider(),
            new GitHubUpdateSource(updateHttpClient),
            new UpdatePackageDownloader(updateHttpClient),
            new WindowsInstallerLauncher(),
            new AppUpdateApplicationLifecycle(
                settings.FlushAsync,
                requestShutdown ?? (() => { })),
            UpdatePlatform.WindowsX64);
    }
}
