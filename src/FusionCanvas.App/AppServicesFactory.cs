using FusionCanvas.App.Settings;
using FusionCanvas.App.Versioning;
using FusionCanvas.Application.AI;
using FusionCanvas.Integration.AI;

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
        var catalogCache = new JsonAiModelCatalogCache(Path.Combine(settingsDirectory, "ai-cache"));
        var httpClient = new HttpClient
        {
            BaseAddress = OpenRouterClient.DefaultBaseAddress,
            Timeout = Timeout.InfiniteTimeSpan
        };
        var openRouter = new OpenRouterClient(httpClient);
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
            AvaloniaClipboardService.Instance);
        var textService = new AiTextGenerationService(aiSettings, credentials, catalogCache, openRouter);
        return new AppServices(
            httpClient,
            settingsStore,
            settings,
            textService,
            new FusionCanvas.Integration.Items.ItemCsvCodec(),
            new FusionCanvas.Integration.Items.Import.ItemCsvCodec());
    }
}
