using FusionCanvas.Application.Settings;
using FusionCanvas.Application.TermsConsent;

namespace FusionCanvas.App.TermsConsent;

internal static class TermsConsentStartupCoordinator
{
    public static async Task<ApplicationSettings?> ResolveAsync(
        ApplicationSettings loadedSettings,
        TermsConsentService consentService,
        string policyText,
        Func<TermsConsentViewModel, Task<ApplicationSettings?>> presentConsentAsync)
    {
        ArgumentNullException.ThrowIfNull(loadedSettings);
        ArgumentNullException.ThrowIfNull(consentService);
        ArgumentException.ThrowIfNullOrWhiteSpace(policyText);
        ArgumentNullException.ThrowIfNull(presentConsentAsync);

        if (!TermsConsentStartupGate.RequiresConsent(loadedSettings))
        {
            return loadedSettings;
        }

        var viewModel = new TermsConsentViewModel(
            loadedSettings,
            consentService,
            policyText);

        return await presentConsentAsync(viewModel).ConfigureAwait(true);
    }
}
