namespace FusionCanvas.Application.Settings;

public sealed record ApplicationSettings(bool DarkMode)
{
    public static ApplicationSettings Default { get; } = new(DarkMode: false);
}
