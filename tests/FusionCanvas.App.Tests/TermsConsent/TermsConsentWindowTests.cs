using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FusionCanvas.App.TermsConsent;
using FusionCanvas.Application.Settings;

namespace FusionCanvas.App.Tests.TermsConsent;

public sealed class TermsConsentWindowTests
{
    [AvaloniaFact]
    public void Window_ShowsOfflinePolicyFourCheckboxesAndGatedActions()
    {
        var viewModel = new TermsConsentViewModel(
            ApplicationSettings.Default,
            new RecordingStore(),
            "Bundled offline policy");
        var window = new TermsConsentWindow { DataContext = viewModel };

        try
        {
            window.Show();
            window.UpdateLayout();

            var checkBoxes = window.GetVisualDescendants().OfType<CheckBox>().ToArray();
            Assert.Equal(4, checkBoxes.Length);
            Assert.Contains(window.GetVisualDescendants().OfType<TextBox>(), box => box.Text == "Bundled offline policy");
            var agree = window.GetVisualDescendants().OfType<Button>().Single(button =>
                AutomationProperties.GetName(button) == "Agree and continue");
            Assert.False(agree.IsEffectivelyEnabled);

            viewModel.FusionCanvasTermsSelected = true;
            viewModel.PrintifyTermsSelected = true;
            viewModel.ShopifyTermsSelected = true;
            viewModel.IntellectualPropertySelected = true;
            window.UpdateLayout();

            Assert.True(agree.IsEffectivelyEnabled);
            Assert.Contains(window.GetVisualDescendants().OfType<Button>(), button =>
                AutomationProperties.GetName(button) == "Quit FusionCanvas");
        }
        finally
        {
            window.Close();
        }
    }

    private sealed class RecordingStore : IApplicationSettingsStore
    {
        public Task<ApplicationSettingsLoadResult> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationSettingsLoadResult.Success(ApplicationSettings.Default));

        public Task<ApplicationSettingsSaveResult> SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationSettingsSaveResult.Success);
    }
}
