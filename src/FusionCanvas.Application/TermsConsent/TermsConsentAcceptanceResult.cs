using FusionCanvas.Application.Settings;

namespace FusionCanvas.Application.TermsConsent;

public sealed record TermsConsentAcceptanceResult(
    bool Saved,
    ApplicationSettings? Settings,
    string? Warning)
{
    public static TermsConsentAcceptanceResult Success(ApplicationSettings settings) =>
        new(true, settings, null);

    public static TermsConsentAcceptanceResult Failed(string warning) =>
        new(false, null, warning);
}
