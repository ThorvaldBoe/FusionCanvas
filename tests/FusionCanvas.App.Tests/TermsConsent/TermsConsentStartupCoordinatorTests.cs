using FusionCanvas.Application.Settings;
using FusionCanvas.Application.TermsConsent;
using FusionCanvas.App.TermsConsent;

namespace FusionCanvas.App.Tests.TermsConsent;

public sealed class TermsConsentStartupCoordinatorTests
{
    [Fact]
    public async Task MissingConsent_DeclinedDecisionPreventsWorkspaceComposition()
    {
        var presented = false;
        var result = await TermsConsentStartupCoordinator.ResolveAsync(
            ApplicationSettings.Default,
            new TermsConsentService(new RecordingStore()),
            "Offline policy",
            _ =>
            {
                presented = true;
                return Task.FromResult<ApplicationSettings?>(null);
            });

        Assert.True(presented);
        Assert.Null(result);
    }

    [Fact]
    public async Task MissingConsent_AcceptedDecisionAllowsWorkspaceComposition()
    {
        var accepted = new ApplicationSettings(
            false,
            FusionCanvas.Application.AI.AiConfigurationSettings.Default,
            TermsConsent: TermsConsentPolicy.CreateRecord(DateTimeOffset.UtcNow));

        var result = await TermsConsentStartupCoordinator.ResolveAsync(
            ApplicationSettings.Default,
            new TermsConsentService(new RecordingStore()),
            "Offline policy",
            _ => Task.FromResult<ApplicationSettings?>(accepted));

        Assert.Same(accepted, result);
    }

    [Fact]
    public async Task CurrentConsent_BypassesConsentSurfaceAndAllowsWorkspaceComposition()
    {
        var current = new ApplicationSettings(
            false,
            FusionCanvas.Application.AI.AiConfigurationSettings.Default,
            TermsConsent: TermsConsentPolicy.CreateRecord(DateTimeOffset.UtcNow));
        var presented = false;

        var result = await TermsConsentStartupCoordinator.ResolveAsync(
            current,
            new TermsConsentService(new RecordingStore()),
            "Offline policy",
            _ =>
            {
                presented = true;
                return Task.FromResult<ApplicationSettings?>(null);
            });

        Assert.Same(current, result);
        Assert.False(presented);
    }

    private sealed class RecordingStore : IApplicationSettingsStore
    {
        public Task<ApplicationSettingsLoadResult> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationSettingsLoadResult.Success(ApplicationSettings.Default));

        public Task<ApplicationSettingsSaveResult> SaveAsync(
            ApplicationSettings settings,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationSettingsSaveResult.Success);
    }
}
