using System.Collections.Immutable;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.TermsConsent;
using FusionCanvas.Application.Updates;

namespace FusionCanvas.Application.Settings;

public sealed record ApplicationSettings(
    bool DarkMode,
    AiConfigurationSettings Ai,
    WindowLayoutSettings? WindowLayout = null,
    Guid? ActiveWorkspaceId = null,
    ImmutableDictionary<string, WindowGeometrySettings>? WindowGeometry = null,
    Guid? ActiveStoreId = null,
    TermsConsentRecord? TermsConsent = null,
    UpdateTrack UpdateTrack = UpdateTrack.Stable)
{
    public ApplicationSettings(bool DarkMode)
        : this(DarkMode, AiConfigurationSettings.Default, null, null, null)
    {
    }

    public static ApplicationSettings Default { get; } =
        new(DarkMode: false, AiConfigurationSettings.Default);
}
