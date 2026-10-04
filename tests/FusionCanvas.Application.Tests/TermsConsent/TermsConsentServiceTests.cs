using FusionCanvas.Application.Settings;
using FusionCanvas.Application.TermsConsent;

namespace FusionCanvas.Application.Tests.TermsConsent;

public sealed class TermsConsentServiceTests
{
    [Fact]
    public async Task AcceptAsync_ValidatesAllAcknowledgementsBeforeSaving()
    {
        var store = new RecordingStore();
        var service = new TermsConsentService(store);

        var result = await service.AcceptAsync(
            ApplicationSettings.Default,
            fusionCanvasTerms: true,
            printifyTerms: true,
            shopifyTerms: true,
            intellectualPropertyResponsibility: false,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.Saved);
        Assert.Null(result.Settings);
        Assert.Null(store.LastSaved);
        Assert.Contains("every acknowledgement", result.Warning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AcceptAsync_SavesCurrentVersionedRecord()
    {
        var store = new RecordingStore();
        var service = new TermsConsentService(store);

        var result = await service.AcceptAsync(
            ApplicationSettings.Default,
            fusionCanvasTerms: true,
            printifyTerms: true,
            shopifyTerms: true,
            intellectualPropertyResponsibility: true,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Saved);
        Assert.NotNull(result.Settings);
        Assert.Same(result.Settings, store.LastSaved);
        Assert.True(TermsConsentPolicy.IsCurrent(result.Settings!.TermsConsent));
    }

    private sealed class RecordingStore : IApplicationSettingsStore
    {
        public ApplicationSettings? LastSaved { get; private set; }

        public Task<ApplicationSettingsLoadResult> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationSettingsLoadResult.Success(ApplicationSettings.Default));

        public Task<ApplicationSettingsSaveResult> SaveAsync(
            ApplicationSettings settings,
            CancellationToken cancellationToken = default)
        {
            LastSaved = settings;
            return Task.FromResult(ApplicationSettingsSaveResult.Success);
        }
    }
}
