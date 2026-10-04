using FusionCanvas.Application.Settings;

namespace FusionCanvas.Application.TermsConsent;

public sealed class TermsConsentService
{
    private readonly IApplicationSettingsStore _settingsStore;

    public TermsConsentService(IApplicationSettingsStore settingsStore)
    {
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
    }

    public async Task<TermsConsentAcceptanceResult> AcceptAsync(
        ApplicationSettings settings,
        bool fusionCanvasTerms,
        bool printifyTerms,
        bool shopifyTerms,
        bool intellectualPropertyResponsibility,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!TermsConsentPolicy.AreAllAcknowledgementsSelected(
                fusionCanvasTerms,
                printifyTerms,
                shopifyTerms,
                intellectualPropertyResponsibility))
        {
            return TermsConsentAcceptanceResult.Failed(
                "Select every acknowledgement before continuing.");
        }

        var updated = settings with
        {
            TermsConsent = TermsConsentPolicy.CreateRecord(DateTimeOffset.UtcNow)
        };
        var saveResult = await _settingsStore
            .SaveAsync(updated, cancellationToken)
            .ConfigureAwait(false);

        return saveResult.Saved
            ? TermsConsentAcceptanceResult.Success(updated)
            : TermsConsentAcceptanceResult.Failed(
                saveResult.Warning ?? "The acknowledgement could not be saved. Please try again.");
    }
}
