using FusionCanvas.App.Settings;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.Settings;

namespace FusionCanvas.App.Tests;

public class AiSettingsViewModelTests
{
    [Fact]
    public async Task EnsureLoaded_IsLazyAndRunsOnlyOnce()
    {
        var credentials = new CredentialStore { Result = AiCredentialReadResult.Available("secret") };
        var vm = Create(credentials: credentials);

        Assert.Equal(0, credentials.Reads);
        await vm.EnsureLoadedAsync();
        await vm.EnsureLoadedAsync();

        Assert.Equal(1, credentials.Reads);
        Assert.True(vm.HasCredential);
        Assert.Equal("Saved — not verified", vm.CredentialStatus);
    }

    [Fact]
    public void AdvancedProfiles_CopyGeneralOnceAndRestoreRetainedCustomProfile()
    {
        var vm = Create();
        vm.General.ModelId = "general/model";
        vm.AdvancedMode = true;

        vm.IdeationUseGeneral = false;
        Assert.Equal("general/model", vm.Ideation.ModelId);
        vm.Ideation.ModelId = "custom/model";
        vm.IdeationUseGeneral = true;
        vm.General.ModelId = "new/general";
        vm.IdeationUseGeneral = false;

        Assert.Equal("custom/model", vm.Ideation.ModelId);
        Assert.True(vm.Current.Ideation.HasCustomProfile);
    }

    [Fact]
    public void ZdrOptOutRequiresConfirmationAndDoesNotReplaceModel()
    {
        var vm = Create();
        vm.General.ModelId = "saved/model";

        vm.RequireZeroDataRetention = false;

        Assert.True(vm.RequireZeroDataRetention);
        Assert.True(vm.ConfirmZdrOptOut);
        vm.ConfirmZdrOptOutCommand.Execute(null);
        Assert.False(vm.RequireZeroDataRetention);
        Assert.Equal("saved/model", vm.General.ModelId);
    }

    [Fact]
    public void SettingsClose_WithKeyDraftRequiresExplicitDiscard()
    {
        var ai = Create();
        var settings = new SettingsViewModel(
            new InMemoryApplicationSettingsStore(),
            new FakeTheme(),
            ApplicationSettings.Default,
            null,
            ai);
        settings.OpenCommand.Execute(null);
        ai.AddOrReplaceCommand.Execute(null);
        ai.CredentialDraft = "unsaved";

        var closed = settings.RequestClose();

        Assert.False(closed);
        Assert.True(settings.IsOpen);
        Assert.True(settings.ConfirmDiscardCredentialDraft);
        settings.ConfirmDiscardCommand.Execute(null);
        Assert.False(settings.IsOpen);
        Assert.False(ai.HasUnsavedCredentialDraft);
    }

    private static AiSettingsViewModel Create(CredentialStore? credentials = null) =>
        new(
            AiConfigurationSettings.Default,
            credentials ?? new CredentialStore(),
            new Validator(),
            new CatalogProvider(),
            new CatalogCache());

    private sealed class CredentialStore : IAiCredentialStore
    {
        public int Reads { get; private set; }
        public AiCredentialReadResult Result { get; set; } = AiCredentialReadResult.NotFound;
        public Task<AiCredentialReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            Reads++;
            return Task.FromResult(Result);
        }
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
        public Task<AiModelCatalog> GetModelsAsync(
            string apiKey,
            bool requireZeroDataRetention,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiModelCatalog(requireZeroDataRetention, DateTimeOffset.UtcNow, []));
    }

    private sealed class CatalogCache : IAiModelCatalogCache
    {
        public Task<AiModelCatalog?> LoadAsync(bool requireZeroDataRetention, CancellationToken cancellationToken = default) =>
            Task.FromResult<AiModelCatalog?>(null);
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
