using System.Collections.Immutable;
using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.Settings;

public sealed record ApplicationSettings(
    bool DarkMode,
    AiConfigurationSettings Ai,
    WindowLayoutSettings? WindowLayout = null,
    Guid? ActiveWorkspaceId = null,
    ImmutableDictionary<string, WindowGeometrySettings>? WindowGeometry = null)
{
    public ApplicationSettings(bool DarkMode)
        : this(DarkMode, AiConfigurationSettings.Default, null, null, null)
    {
    }

    public static ApplicationSettings Default { get; } =
        new(DarkMode: false, AiConfigurationSettings.Default);
}

public sealed record WindowLayoutSettings(
    int PositionX,
    int PositionY,
    double Width,
    double Height,
    double NavigationWidth);
