using Avalonia.Headless.XUnit;
using FusionCanvas.App.Settings;
using FusionCanvas.App.TermsConsent;
using FusionCanvas.Application.Settings;
using FusionCanvas.Application.TermsConsent;
using Avalonia.VisualTree;

namespace FusionCanvas.App.Tests.TermsConsent;

public sealed class TermsConsentSettingsTests
{
    [Fact]
    public void SettingsSummary_ExplainsWhenAcknowledgementIsMissing()
    {
        var viewModel = CreateViewModel(ApplicationSettings.Default);

        Assert.False(viewModel.HasCurrentTermsConsent);
        Assert.Contains("required", viewModel.TermsConsentSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SettingsSummary_ShowsAcceptedVersionsAndUtcTime()
    {
        var acceptedAt = new DateTimeOffset(2026, 10, 4, 12, 30, 0, TimeSpan.Zero);
        var settings = new ApplicationSettings(
            false,
            FusionCanvas.Application.AI.AiConfigurationSettings.Default,
            TermsConsent: TermsConsentPolicy.CreateRecord(acceptedAt));
        var viewModel = CreateViewModel(settings);

        Assert.True(viewModel.HasCurrentTermsConsent);
        Assert.Contains(TermsConsentPolicy.FusionCanvasTermsVersion, viewModel.TermsConsentSummary);
        Assert.Contains(TermsConsentPolicy.AcknowledgementPolicyVersion, viewModel.TermsConsentSummary);
        Assert.Contains("2026-10-04 12:30:00Z", viewModel.TermsConsentSummary);
    }

    [AvaloniaFact]
    public void SettingsTermsSection_ProvidesSingleReviewRoute()
    {
        var viewModel = CreateViewModel(ApplicationSettings.Default);
        viewModel.SelectedSection = SettingsSection.Terms;
        var window = new SettingsWindow { DataContext = viewModel };

        try
        {
            window.Show();
            window.UpdateLayout();

            Assert.True(viewModel.IsTermsSection);
            Assert.Contains(window.GetVisualDescendants().OfType<Avalonia.Controls.Button>(), button =>
                button.Content as string == "Review terms and policies");
        }
        finally
        {
            window.Close();
        }
    }

    private static SettingsViewModel CreateViewModel(ApplicationSettings settings) =>
        new(new InMemoryApplicationSettingsStore(), new AvaloniaApplicationThemeController(), settings, null);
}
