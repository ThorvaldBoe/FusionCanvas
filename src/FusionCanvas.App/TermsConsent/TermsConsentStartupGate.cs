using FusionCanvas.Application.Settings;
using FusionCanvas.Application.TermsConsent;

namespace FusionCanvas.App.TermsConsent;

internal static class TermsConsentStartupGate
{
    public static bool RequiresConsent(ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return !TermsConsentPolicy.IsCurrent(settings.TermsConsent);
    }
}
