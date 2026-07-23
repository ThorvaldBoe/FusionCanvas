namespace FusionCanvas.Application.Settings;

public interface IApplicationSettingsStore
{
    Task<ApplicationSettingsLoadResult> LoadAsync(CancellationToken cancellationToken = default);

    Task<ApplicationSettingsSaveResult> SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default);
}

public sealed record ApplicationSettingsLoadResult(ApplicationSettings Value, bool UsedDefault, string? Warning)
{
    public static ApplicationSettingsLoadResult Success(ApplicationSettings value)
        => new(value, UsedDefault: false, Warning: null);

    public static ApplicationSettingsLoadResult Defaulted(string? warning = null)
        => new(ApplicationSettings.Default, UsedDefault: true, warning);
}

public sealed record ApplicationSettingsSaveResult(bool Saved, string? Warning)
{
    public static ApplicationSettingsSaveResult Success { get; } = new(Saved: true, Warning: null);

    public static ApplicationSettingsSaveResult Failed(string warning)
        => new(Saved: false, warning);
}
