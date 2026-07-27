using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FusionCanvas.App.Settings;
using FusionCanvas.Application.Settings;

namespace FusionCanvas.App.Tests;

public class AiSettingsViewTests
{
    [AvaloniaFact]
    public void AiSection_ConstructsCompiledBindingsAndMasksCredentialDraft()
    {
        var settings = new SettingsViewModel(
            new InMemoryApplicationSettingsStore(),
            new FakeTheme(),
            ApplicationSettings.Default,
            null);
        settings.OpenCommand.Execute(null);
        var window = new SettingsWindow { DataContext = settings };
        try
        {
            window.Show();
            settings.SelectedSection = SettingsSection.AI;
            settings.Ai.AddOrReplaceCommand.Execute(null);

            var aiView = window.GetVisualDescendants().OfType<AiSettingsView>().Single();
            var draft = aiView.FindControl<TextBox>("ApiKeyDraft");

            Assert.True(settings.IsAiSection);
            Assert.NotNull(draft);
            Assert.NotEqual('\0', draft!.PasswordChar);
            Assert.True(window.MinWidth >= 720);
            Assert.True(window.MinHeight >= 520);
            Assert.NotNull(window.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault());
        }
        finally
        {
            settings.Ai.DiscardCredentialDraft();
            window.Close();
        }
    }

    private sealed class FakeTheme : IApplicationThemeController
    {
        public void ApplyDarkMode(bool darkMode)
        {
        }
    }
}
