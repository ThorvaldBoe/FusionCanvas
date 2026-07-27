using FusionCanvas.App.Settings;
using FusionCanvas.Application.AI;
using FusionCanvas.Integration.AI;

namespace FusionCanvas.App;

public static class AppServicesFactory
{
    public static AppServices Create()
    {
        var settingsStore = AppSettingsFactory.CreateStore();
        var load = settingsStore.LoadAsync().GetAwaiter().GetResult();
        var settingsDirectory = Path.GetDirectoryName(
            ((FusionCanvas.Integration.Settings.JsonApplicationSettingsStore)settingsStore).SettingsPath)
            ?? AppContext.BaseDirectory;

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
            aiSettings);
        var textService = new AiTextGenerationService(aiSettings, credentials, catalogCache, openRouter);
        return new AppServices(httpClient, settingsStore, settings, textService);
    }
}
