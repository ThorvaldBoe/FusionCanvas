using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FusionCanvas.App.Settings;
using FusionCanvas.Application.AI;
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

    [AvaloniaFact]
    public async Task AiSection_RendersEmptyGuidanceWhenNoCredential()
    {
        var ai = new AiSettingsViewModel(
            AiConfigurationSettings.Default,
            new CredentialStore(AiCredentialReadResult.NotFound),
            new Validator(),
            new CatalogProvider(),
            new CatalogCache());
        var settings = new SettingsViewModel(
            new InMemoryApplicationSettingsStore(),
            new FakeTheme(),
            ApplicationSettings.Default,
            null,
            ai);
        settings.OpenCommand.Execute(null);
        var window = new SettingsWindow { DataContext = settings };
        try
        {
            window.Show();
            settings.SelectedSection = SettingsSection.AI;
            await ai.EnsureLoadedAsync();
            window.UpdateLayout();

            Assert.Contains("API key", ai.Message);
            var message = window.GetVisualDescendants().OfType<TextBlock>()
                .FirstOrDefault(t => t.Text == ai.Message);
            Assert.NotNull(message);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task AiSection_ListsOnlyZdrCompatibleModelsAfterValidation()
    {
        var ai = new AiSettingsViewModel(
            AiConfigurationSettings.Default,
            new CredentialStore(AiCredentialReadResult.Available("secret")),
            new Validator(),
            new CatalogProvider
            {
                Models = [Descriptor("zdr/model", true), Descriptor("plain/model", false)]
            },
            new CatalogCache
            {
                Cached = new AiModelCatalog(true, DateTimeOffset.UtcNow, [Descriptor("seed/model", true)])
            });
        var settings = new SettingsViewModel(
            new InMemoryApplicationSettingsStore(),
            new FakeTheme(),
            ApplicationSettings.Default,
            null,
            ai);
        settings.OpenCommand.Execute(null);
        var window = new SettingsWindow { DataContext = settings };
        try
        {
            window.Show();
            settings.SelectedSection = SettingsSection.AI;
            await ai.EnsureLoadedAsync();
            await ai.ValidateCredentialAsync();
            window.UpdateLayout();

            var modelBox = window.GetVisualDescendants().OfType<AiSettingsView>().Single()
                .GetVisualDescendants().OfType<ComboBox>().First();
            Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), text =>
                text.Text == "Search the models allowed by the privacy setting below. Requiring Zero Data Retention narrows the list to compatible models.");
            var items = modelBox.Items.OfType<string>().ToArray();
            Assert.Single(items);
            Assert.Equal("zdr/model", items[0]);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void AiSection_ProgressivelyDisclosesAdvancedPurposeProfiles()
    {
        var ai = new AiSettingsViewModel(
            AiConfigurationSettings.Default,
            new CredentialStore(AiCredentialReadResult.NotFound),
            new Validator(),
            new CatalogProvider(),
            new CatalogCache());
        var settings = new SettingsViewModel(
            new InMemoryApplicationSettingsStore(),
            new FakeTheme(),
            ApplicationSettings.Default,
            null,
            ai);
        settings.OpenCommand.Execute(null);
        settings.SelectedSection = SettingsSection.AI;
        var window = new SettingsWindow { DataContext = settings };
        window.Show();
        try
        {
            window.UpdateLayout();
            var ideationText = window.GetVisualDescendants().OfType<TextBlock>()
                .Single(text => text.Text == "Ideation");
            Assert.Equal(0, ideationText.Bounds.Height);

            ai.AdvancedMode = true;
            window.UpdateLayout();

            Assert.True(ideationText.Bounds.Height > 0);
            Assert.True(window.GetVisualDescendants().OfType<TextBlock>()
                .Single(text => text.Text == "Concept").Bounds.Height > 0);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ProfileEditor_ShowsGuidanceForSupportedAdditionalParameters()
    {
        var viewModel = new AiProfileEditorViewModel(AiProfileSettings.Empty)
        {
            Models = [new AiModelDescriptor("model", "Model", null, null, ["text"], ["text"],
                [AiParameterRegistry.TopP, AiParameterRegistry.Seed], 1000, null, null, null, false, null)]
        };
        var window = new Window { Content = new AiProfileEditorView { DataContext = viewModel } };
        try
        {
            window.Show();
            window.UpdateLayout();

            var labels = window.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text).ToArray();
            Assert.Contains("Top P", labels);
            Assert.Contains("Narrow the pool of likely next tokens (0–1).", labels);
            Assert.Contains("Seed", labels);
            Assert.DoesNotContain("Top K", labels);
        }
        finally
        {
            window.Close();
        }
    }

    private static AiModelDescriptor Descriptor(string id, bool zdr) =>
        new(id, id, null, null, ["text"], ["text"], [], 1000, null, null, null, zdr, null);

    private sealed class CredentialStore : IAiCredentialStore
    {
        private readonly AiCredentialReadResult _result;
        public CredentialStore(AiCredentialReadResult result) => _result = result;
        public Task<AiCredentialReadResult> ReadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_result);
        public Task<AiCredentialOperationResult> SaveAsync(string apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(AiCredentialOperationResult.Success);
        public Task<AiCredentialOperationResult> RemoveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(AiCredentialOperationResult.Success);
    }

    private sealed class Validator : IAiCredentialValidator
    {
        public Task<AiCredentialValidationResult> ValidateAsync(string apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiCredentialValidationResult(AiCredentialValidationKind.Valid));
    }

    private sealed class CatalogProvider : IAiModelCatalogProvider
    {
        public IReadOnlyList<AiModelDescriptor> Models { get; set; } = [];
        public Task<AiModelCatalog> GetModelsAsync(
            string apiKey,
            bool requireZeroDataRetention,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiModelCatalog(requireZeroDataRetention, DateTimeOffset.UtcNow, Models));
    }

    private sealed class CatalogCache : IAiModelCatalogCache
    {
        public AiModelCatalog? Cached { get; set; }
        public Task<AiModelCatalog?> LoadAsync(bool requireZeroDataRetention, CancellationToken cancellationToken = default) =>
            Task.FromResult(Cached);
        public Task SaveAsync(AiModelCatalog catalog, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeTheme : IApplicationThemeController
    {
        public void ApplyDarkMode(bool darkMode)
        {
        }
    }
}
