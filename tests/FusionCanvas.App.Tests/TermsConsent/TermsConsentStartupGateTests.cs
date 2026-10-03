using FusionCanvas.App.TermsConsent;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.Settings;
using FusionCanvas.Application.TermsConsent;

namespace FusionCanvas.App.Tests.TermsConsent;

public sealed class TermsConsentStartupGateTests
{
    [Fact]
    public void RequiresConsent_WhenRecordIsMissingOrStale()
    {
        Assert.True(TermsConsentStartupGate.RequiresConsent(ApplicationSettings.Default));

        var stale = new ApplicationSettings(
            false,
            AiConfigurationSettings.Default,
            TermsConsent: new TermsConsentRecord("draft-old", TermsConsentPolicy.AcknowledgementPolicyVersion, DateTimeOffset.UtcNow));

        Assert.True(TermsConsentStartupGate.RequiresConsent(stale));
    }

    [Fact]
    public void DoesNotRequireConsent_WhenBothVersionsAreCurrent()
    {
        var current = new ApplicationSettings(
            false,
            AiConfigurationSettings.Default,
            TermsConsent: TermsConsentPolicy.CreateRecord(DateTimeOffset.UtcNow));

        Assert.False(TermsConsentStartupGate.RequiresConsent(current));
    }
}
