using FusionCanvas.App.Settings;
using FusionCanvas.App.Versioning;
using FusionCanvas.Application.AI;
using FusionCanvas.Integration.AI;
using FusionCanvas.Application.Telemetry;
using FusionCanvas.App.Workspace;
using FusionCanvas.Integration.Persistence;

namespace FusionCanvas.App;

public static class AppServicesFactory
{
    public static AppServices Create()
        => Create(AppSettingsFactory.CreateStore());

    public static AppServices Create(
        FusionCanvas.Application.Settings.IApplicationSettingsStore settingsStore)
    {
        ArgumentNullException.ThrowIfNull(settingsStore);
        var load = StartupTaskRunner.Run(() => settingsStore.LoadAsync());
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
            telemetryContext);
        var textService = new AiTextGenerationService(aiSettings, credentials, catalogCache, openRouter);
        var services = new AppServices(
            httpClient,
            settingsStore,
            settings,
            textService,
            openRouter,
            new FusionCanvas.Integration.Items.ItemCsvCodec(),
            new FusionCanvas.Integration.Items.Import.ItemCsvCodec(),
            telemetry);
        var printifyClient = FusionCanvas.Integration.Stores.Printify.PrintifyCredentialVerifier.CreateHttpClient();
        var printifyCatalogClient = FusionCanvas.Integration.Stores.Printify.PrintifyCatalogClient.CreateHttpClient();
        services.ConfigurePrintify(printifyClient,
            new FusionCanvas.Integration.Stores.Printify.NativeStorePrintifyCredentialStore(),
            new FusionCanvas.Integration.Stores.Printify.PrintifyCredentialVerifier(printifyClient),
            new FusionCanvas.Integration.Stores.Printify.PrintifyCatalogClient(printifyCatalogClient, telemetry),
            printifyCatalogClient);
        return services;
    }
}
