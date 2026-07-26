
namespace FusionCanvas.Application.Settings;

public sealed record ApplicationSettingsLoadResult(ApplicationSettings Value, bool UsedDefault, string? Warning)
{
    public static ApplicationSettingsLoadResult Success(ApplicationSettings value)
        => new(value, UsedDefault: false, Warning: null);

    public static ApplicationSettingsLoadResult Defaulted(string? warning = null)
        => new(ApplicationSettings.Default, UsedDefault: true, warning);
}
