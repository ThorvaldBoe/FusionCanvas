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

        Assert.Equal(2, credentials.Reads);
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

    [Fact]
    public async Task ValidationSuccess_AutoLoadsCatalog()
    {
        var credentials = new CredentialStore { Result = AiCredentialReadResult.Available("secret") };
        var catalog = new CatalogProvider { Models = [Model("a/model")] };
        var cache = new CatalogCache { Cached = null };
        var vm = Create(credentials, catalogProvider: catalog, catalogCache: cache);
        await vm.EnsureLoadedAsync();

        await vm.ValidateCredentialAsync();

        Assert.True(vm.HasCredential);
        Assert.Equal(2, catalog.Calls);
        Assert.Equal("a/model", vm.General.Models[0].Id);
    }

    [Fact]
    public async Task ValidationFailure_DoesNotLoadCatalog()
    {
        var credentials = new CredentialStore { Result = AiCredentialReadResult.Available("secret") };
        var catalog = new CatalogProvider { Models = [Model("a/model")] };
        var validator = new Validator { Kind = AiCredentialValidationKind.Invalid };
        var cache = new CatalogCache { Cached = null };
        var vm = Create(credentials, validator, catalog, cache);
        await vm.EnsureLoadedAsync();

        await vm.ValidateCredentialAsync();

        Assert.Equal(1, catalog.Calls);
    }

    [Fact]
    public async Task EnsureLoaded_FetchesWhenCredentialPresentButNoCache()
    {
        var credentials = new CredentialStore { Result = AiCredentialReadResult.Available("secret") };
        var catalog = new CatalogProvider { Models = [Model("a/model")] };
        var cache = new CatalogCache { Cached = null };
        var vm = Create(credentials, catalogProvider: catalog, catalogCache: cache);

        await vm.EnsureLoadedAsync();

        Assert.Equal(1, catalog.Calls);
        Assert.Single(vm.General.Models);
    }

    [Fact]
    public async Task EnsureLoaded_DoesNotFetchWhenCacheExists()
    {
        var credentials = new CredentialStore { Result = AiCredentialReadResult.Available("secret") };
        var catalog = new CatalogProvider { Models = [Model("a/model")] };
        var cache = new CatalogCache
        {
            Cached = new AiModelCatalog(true, DateTimeOffset.UtcNow, [Model("cached/model")])
        };
        var vm = Create(credentials, catalogProvider: catalog, catalogCache: cache);

        await vm.EnsureLoadedAsync();

        Assert.Equal(0, catalog.Calls);
        Assert.Equal("cached/model", vm.General.Models[0].Id);
    }

    [Fact]
    public async Task EnsureLoaded_NeverFetchesWithoutCredential()
    {
        var credentials = new CredentialStore { Result = AiCredentialReadResult.NotFound };
        var catalog = new CatalogProvider { Models = [Model("a/model")] };
        var vm = Create(credentials, catalogProvider: catalog);

        await vm.EnsureLoadedAsync();

        Assert.Equal(0, catalog.Calls);
        Assert.Empty(vm.General.Models);
        Assert.Contains("API key", vm.Message);
    }

    [Fact]
    public async Task CatalogLoad_DoesNotDuplicateWhileBusy()
    {
        var credentials = new CredentialStore { Result = AiCredentialReadResult.Available("secret") };
        var tcs = new TaskCompletionSource<AiModelCatalog>();
        var catalog = new CatalogProvider { Pending = tcs };
        var cache = new CatalogCache
        {
            Cached = new AiModelCatalog(true, DateTimeOffset.UtcNow, [Model("seed/model")])
        };
        var vm = Create(credentials, catalogProvider: catalog, catalogCache: cache);
        await vm.EnsureLoadedAsync();
        cache.Cached = null;

        var first = vm.EnsureCatalogAsync(true);
        var second = vm.EnsureCatalogAsync(true);
        tcs.SetResult(new AiModelCatalog(true, DateTimeOffset.UtcNow, [Model("a/model")]));
        await Task.WhenAll(first, second);

        Assert.Equal(1, catalog.Calls);
    }

    [Fact]
    public async Task CacheSaveFailure_KeepsFetchedModelsAndWarns()
    {
        var credentials = new CredentialStore { Result = AiCredentialReadResult.Available("secret") };
        var catalog = new CatalogProvider { Models = [Model("a/model")] };
        var cache = new CatalogCache
        {
            Cached = new AiModelCatalog(true, DateTimeOffset.UtcNow, [Model("seed/model")]),
            SaveThrow = new IOException("disk")
        };
        var vm = Create(credentials, catalogProvider: catalog, catalogCache: cache);
        await vm.EnsureLoadedAsync();

        await vm.EnsureCatalogAsync(true);

        Assert.Equal("a/model", vm.General.Models[0].Id);
        Assert.Equal(1, cache.Saves);
        Assert.Contains("could not be cached", vm.Message);
    }

    [Fact]
    public async Task CatalogFailure_MapsAuthenticationAndKeepsCache()
    {
        var credentials = new CredentialStore { Result = AiCredentialReadResult.Available("secret") };
        var catalog = new CatalogProvider
        {
            Throw = new AiModelCatalogFetchException(
                AiModelCatalogFailureKind.Authentication,
                "OpenRouter rejected the saved API key.")
        };
        var cache = new CatalogCache
        {
            Cached = new AiModelCatalog(true, DateTimeOffset.UtcNow, [Model("cached/model")])
        };
        var vm = Create(credentials, catalogProvider: catalog, catalogCache: cache);
        await vm.EnsureLoadedAsync();

        await vm.EnsureCatalogAsync(true);

        Assert.Equal("cached/model", vm.General.Models[0].Id);
        Assert.Contains("rejected", vm.Message);
        Assert.DoesNotContain("secret", vm.Message);
    }

    [Fact]
    public async Task RequireZeroDataRetention_NarrowsSelectorToCompatibleModels()
    {
        var credentials = new CredentialStore { Result = AiCredentialReadResult.Available("secret") };
        var catalog = new CatalogProvider
        {
            Models = [Model("zdr/model", zdr: true), Model("plain/model", zdr: false)]
        };
        var cache = new CatalogCache { Cached = null };
        var vm = Create(credentials, catalogProvider: catalog, catalogCache: cache);
        await vm.EnsureLoadedAsync();

        Assert.True(vm.RequireZeroDataRetention);
        Assert.Single(vm.General.Models);
        Assert.Equal("zdr/model", vm.General.Models[0].Id);

        vm.RequireZeroDataRetention = false;
        Assert.True(vm.ConfirmZdrOptOut);
        vm.ConfirmZdrOptOutCommand.Execute(null);

        Assert.False(vm.RequireZeroDataRetention);
        Assert.Equal(2, vm.General.Models.Count);
    }

    private static AiModelDescriptor Model(string id, bool zdr = true) =>
        new(id, id, null, null, ["text"], ["text"], [], 1000, null, null, null, zdr, null);

    private static AiSettingsViewModel Create(
        CredentialStore? credentials = null,
        Validator? validator = null,
        CatalogProvider? catalogProvider = null,
        CatalogCache? catalogCache = null) =>
        new(
            AiConfigurationSettings.Default,
            credentials ?? new CredentialStore(),
            validator ?? new Validator(),
            catalogProvider ?? new CatalogProvider(),
            catalogCache ?? new CatalogCache());

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
        public AiCredentialValidationKind Kind { get; set; } = AiCredentialValidationKind.Valid;
        public Task<AiCredentialValidationResult> ValidateAsync(string apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiCredentialValidationResult(Kind));
    }

    private sealed class CatalogProvider : IAiModelCatalogProvider
    {
        public int Calls { get; private set; }
        public IReadOnlyList<AiModelDescriptor> Models { get; set; } = [];
        public Exception? Throw { get; set; }
        public TaskCompletionSource<AiModelCatalog>? Pending { get; set; }
        public Task<AiModelCatalog> GetModelsAsync(
            string apiKey,
            bool requireZeroDataRetention,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Pending is not null)
            {
                return Pending.Task;
            }

            if (Throw is not null)
            {
                return Task.FromException<AiModelCatalog>(Throw);
            }

            return Task.FromResult(new AiModelCatalog(
                requireZeroDataRetention,
                DateTimeOffset.UtcNow,
                Models));
        }
    }

    private sealed class CatalogCache : IAiModelCatalogCache
    {
        public AiModelCatalog? Cached { get; set; }
        public int Saves { get; private set; }
        public Exception? SaveThrow { get; set; }
        public Task<AiModelCatalog?> LoadAsync(bool requireZeroDataRetention, CancellationToken cancellationToken = default) =>
            Task.FromResult(Cached);
        public Task SaveAsync(AiModelCatalog catalog, CancellationToken cancellationToken = default)
        {
            Saves++;
            return SaveThrow is null ? Task.CompletedTask : Task.FromException(SaveThrow);
        }
    }

    private sealed class FakeTheme : IApplicationThemeController
    {
        public void ApplyDarkMode(bool darkMode)
        {
        }
    }
}
