using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.Settings;

public sealed record ApplicationSettings(bool DarkMode, AiConfigurationSettings Ai)
{
    public ApplicationSettings(bool DarkMode)
        : this(DarkMode, AiConfigurationSettings.Default)
    {
    }

    public static ApplicationSettings Default { get; } =
        new(DarkMode: false, AiConfigurationSettings.Default);
}
