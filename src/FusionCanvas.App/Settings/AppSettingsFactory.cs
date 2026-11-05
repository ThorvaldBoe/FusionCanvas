using FusionCanvas.Application.Settings;
using FusionCanvas.Integration.Settings;
using FusionCanvas.App.Storage;

namespace FusionCanvas.App.Settings;

public static class AppSettingsFactory
{
    public const string SettingsPathEnvironmentVariable = "FUSIONCANVAS_SETTINGS_PATH";

    public static IApplicationSettingsStore CreateStore()
        => new JsonApplicationSettingsStore(DefaultSettingsPath());

    public static SettingsViewModel LoadInitialState(CancellationToken cancellationToken = default)
    {
        var store = CreateStore();
        var load = StartupTaskRunner.Run(
            token => store.LoadAsync(token),
            cancellationToken);
        var themeController = new AvaloniaApplicationThemeController();
        return new SettingsViewModel(store, themeController, load.Value, load.Warning);
    }

    public static string DefaultSettingsPath()
    {
        return AppDataPathResolver.ResolveFromEnvironment().Settings;
    }
}
