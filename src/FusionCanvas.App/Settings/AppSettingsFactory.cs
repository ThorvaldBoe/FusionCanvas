using FusionCanvas.Application.Settings;
using FusionCanvas.Integration.Settings;

namespace FusionCanvas.App.Settings;

public static class AppSettingsFactory
{
    public const string SettingsPathEnvironmentVariable = "FUSIONCANVAS_SETTINGS_PATH";

    public static IApplicationSettingsStore CreateStore()
        => new JsonApplicationSettingsStore(DefaultSettingsPath());

    public static SettingsViewModel LoadInitialState()
    {
        var store = CreateStore();
        var load = store.LoadAsync().GetAwaiter().GetResult();
        var themeController = new AvaloniaApplicationThemeController();
        return new SettingsViewModel(store, themeController, load.Value, load.Warning);
    }

    public static string DefaultSettingsPath()
    {
        var overridePath = Environment.GetEnvironmentVariable(SettingsPathEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return Path.GetFullPath(overridePath);
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "FusionCanvas", "settings.json");
    }
}
