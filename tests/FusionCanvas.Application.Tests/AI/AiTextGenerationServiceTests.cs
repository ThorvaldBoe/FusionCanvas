using FusionCanvas.Application.AI;
using FusionCanvas.Integration.Testing;
using System.Text.Json;

namespace FusionCanvas.Application.Tests.AI;

public class AiTextGenerationServiceTests
{
    [Fact]
    public async Task GenerateAsync_InvalidRequestMakesNoExternalCalls()
    {
        var fixture = new Fixture(AiConfigurationSettings.Default);

        var result = await fixture.Service.GenerateAsync(
            new AiTextRequest(AiRequestPurpose.General, []),
            TestContext.Current.CancellationToken);

        Assert.Equal(AiTextFailureKind.InvalidRequest, result.FailureKind);
        Assert.Equal(0, fixture.Credentials.Reads);
        Assert.Empty(fixture.Provider.TextRequests);
    }

    [Fact]
    public async Task GenerateAsync_IncompleteConfigurationMakesNoCredentialOrProviderCall()
    {
        var fixture = new Fixture(AiConfigurationSettings.Default);

        var result = await fixture.Service.GenerateAsync(
            Request(),
            TestContext.Current.CancellationToken);

        Assert.Equal(AiTextFailureKind.NotConfigured, result.FailureKind);
        Assert.Equal(0, fixture.Credentials.Reads);
        Assert.Empty(fixture.Provider.TextRequests);
    }

    [Fact]
    public async Task GenerateAsync_ResolvesCurrentSettingsCredentialAndEffectiveProfile()
    {
        var profile = AiProfileSettings.Empty with { ModelId = "model", Temperature = 0.4, TopP = 0.9 };
        var fixture = new Fixture(AiConfigurationSettings.Default with { General = profile });
        fixture.Cache.Catalog = new AiModelCatalog(
            true,
            DateTimeOffset.UtcNow,
            [new AiModelDescriptor("model", "Model", null, null, ["text"], ["text"],
                [AiParameterRegistry.Temperature, AiParameterRegistry.TopP], 1000, 100, null, null, true, null)]);
        fixture.Credentials.Result = AiCredentialReadResult.Available("secret");

        var result = await fixture.Service.GenerateAsync(Request(), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var providerRequest = Assert.Single(fixture.Provider.TextRequests);
        Assert.Equal(0.4, providerRequest.Profile.Temperature);
        Assert.Equal(0.9, providerRequest.Profile.TopP);
        Assert.DoesNotContain("secret", JsonSerializer.Serialize(fixture.Provider.TextRequests), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateAsync_UsesCustomPurposeProfileWhenAdvancedModeIsEnabled()
    {
        var general = AiProfileSettings.Empty with { ModelId = "general/model" };
        var ideation = AiProfileSettings.Empty with { ModelId = "ideation/model", Temperature = 0.8 };
        var settings = AiConfigurationSettings.Default with
        {
            AdvancedMode = true,
            General = general,
            Ideation = new AiPurposeProfileSettings(false, true, ideation)
        };
        var fixture = new Fixture(settings);
        fixture.Cache.Catalog = new AiModelCatalog(
            true,
            DateTimeOffset.UtcNow,
            [
                new AiModelDescriptor("general/model", "General", null, null, ["text"], ["text"], [], 1000, 100, null, null, true, null),
                new AiModelDescriptor("ideation/model", "Ideation", null, null, ["text"], ["text"], [AiParameterRegistry.Temperature], 1000, 100, null, null, true, null)
            ]);
        fixture.Credentials.Result = AiCredentialReadResult.Available("secret");

        var result = await fixture.Service.GenerateAsync(
            new AiTextRequest(AiRequestPurpose.Ideation, [new AiTextMessage(AiMessageRole.User, "idea")]),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var providerRequest = Assert.Single(fixture.Provider.TextRequests);
        Assert.Equal("ideation/model", providerRequest.ModelId);
        Assert.Equal(0.8, providerRequest.Profile.Temperature);
    }

    [Fact]
    public async Task GenerateAsync_RejectsImageInputWhenSelectedModelIsTextOnly()
    {
        var profile = AiProfileSettings.Empty with { ModelId = "model" };
        var fixture = new Fixture(AiConfigurationSettings.Default with { General = profile });
        fixture.Cache.Catalog = new AiModelCatalog(
            true,
            DateTimeOffset.UtcNow,
            [new AiModelDescriptor("model", "Model", null, null, ["text"], ["text"], [], 1000, 100, null, null, true, null)]);
        fixture.Credentials.Result = AiCredentialReadResult.Available("secret");

        var result = await fixture.Service.GenerateAsync(
            new AiTextRequest(AiRequestPurpose.General, [new AiTextMessage(AiMessageRole.User, "look", [new AiImageInput("image/png", [1])])]),
            TestContext.Current.CancellationToken);

        Assert.Equal(AiTextFailureKind.InvalidConfiguration, result.FailureKind);
        Assert.Empty(fixture.Provider.TextRequests);
    }

    [Fact]
    public async Task Availability_ReportsSelectedModelImageInputCapability()
    {
        var profile = AiProfileSettings.Empty with { ModelId = "vision" };
        var fixture = new Fixture(AiConfigurationSettings.Default with { General = profile });
        fixture.Cache.Catalog = new AiModelCatalog(
            true,
            DateTimeOffset.UtcNow,
            [new AiModelDescriptor("vision", "Vision", null, null, ["text", "image"], ["text"], [], 1000, 100, null, null, true, null)]);
        fixture.Credentials.Result = AiCredentialReadResult.Available("secret");

        var result = await fixture.Service.GetAvailabilityAsync(AiRequestPurpose.General, TestContext.Current.CancellationToken);

        Assert.True(result.IsReady);
        Assert.True(result.SupportsImageInput);
    }

    [Fact]
    public async Task GenerateAsync_CancellationIsPropagatedWithoutProviderDispatch()
    {
        var fixture = new Fixture(AiConfigurationSettings.Default);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Service.GenerateAsync(
            Request(),
            new CancellationToken(canceled: true)));

        Assert.Equal(0, fixture.Credentials.Reads);
        Assert.Empty(fixture.Provider.TextRequests);
    }

    [Fact]
    public async Task Availability_RequiresConfiguredModelThenNativeCredentialWithoutDispatch()
    {
        var missingModel = new Fixture(AiConfigurationSettings.Default);
        var modelResult = await missingModel.Service.GetAvailabilityAsync(
            AiRequestPurpose.Ideation,
            TestContext.Current.CancellationToken);
        Assert.Equal(AiAvailabilityKind.MissingModel, modelResult.Kind);
        Assert.Equal(0, missingModel.Credentials.Reads);

        var profile = AiProfileSettings.Empty with { ModelId = "model" };
        var fixture = new Fixture(AiConfigurationSettings.Default with { General = profile });
        fixture.Cache.Catalog = new AiModelCatalog(
            true,
            DateTimeOffset.UtcNow,
            [new AiModelDescriptor("model", "Model", null, null, ["text"], ["text"],
                [], 1000, 100, null, null, true, null)]);

        var missingKey = await fixture.Service.GetAvailabilityAsync(
            AiRequestPurpose.Ideation,
            TestContext.Current.CancellationToken);
        Assert.Equal(AiAvailabilityKind.MissingCredential, missingKey.Kind);

        fixture.Credentials.Result = AiCredentialReadResult.Available("never-return-this");
        var ready = await fixture.Service.GetAvailabilityAsync(
            AiRequestPurpose.Ideation,
            TestContext.Current.CancellationToken);
        Assert.Equal(AiAvailabilityKind.Ready, ready.Kind);
        Assert.DoesNotContain("never-return-this", ready.Message, StringComparison.Ordinal);
        Assert.Empty(fixture.Provider.TextRequests);
    }

    [Fact]
    public async Task Availability_CategorizesCredentialAndProfileFailures()
    {
        var profile = AiProfileSettings.Empty with { ModelId = "model" };
        var fixture = new Fixture(AiConfigurationSettings.Default with { General = profile });
        fixture.Cache.Catalog = new AiModelCatalog(
            true,
            DateTimeOffset.UtcNow,
            [new AiModelDescriptor("model", "Model", null, null, ["text"], ["text"],
                [], 1000, 100, null, null, true, null)]);
        fixture.Credentials.Result = AiCredentialReadResult.Failure(
            AiCredentialStateKind.Locked,
            "Credential store locked.");

        var unavailable = await fixture.Service.GetAvailabilityAsync(
            AiRequestPurpose.Ideation,
            TestContext.Current.CancellationToken);
        Assert.Equal(AiAvailabilityKind.CredentialUnavailable, unavailable.Kind);

        fixture.Configuration.Current = fixture.Configuration.Current with
        {
            General = profile with { Temperature = 0.7 }
        };
        var invalid = await fixture.Service.GetAvailabilityAsync(
            AiRequestPurpose.Ideation,
            TestContext.Current.CancellationToken);
        Assert.Equal(AiAvailabilityKind.InvalidConfiguration, invalid.Kind);
        Assert.Equal(1, fixture.Credentials.Reads);
    }

    private static AiTextRequest Request() =>
        new(AiRequestPurpose.General, [new AiTextMessage(AiMessageRole.User, "hello")]);

    private sealed class Fixture
    {
        public Fixture(AiConfigurationSettings settings)
        {
            Configuration = new ConfigurationProvider(settings);
            Credentials = new CredentialStore();
            Cache = new CatalogCache();
            Provider = new MockOpenRouterClient();
            Service = new AiTextGenerationService(Configuration, Credentials, Cache, Provider);
        }

        public ConfigurationProvider Configuration { get; }
        public CredentialStore Credentials { get; }
        public CatalogCache Cache { get; }
        public MockOpenRouterClient Provider { get; }
        public AiTextGenerationService Service { get; }
    }

    private sealed class ConfigurationProvider(AiConfigurationSettings settings) : IAiConfigurationProvider
    {
        public AiConfigurationSettings Current { get; set; } = settings;
        public IReadOnlyList<AiModelDescriptor> AvailableModels { get; } = [];

        public Task<string?> ReadApiKeyAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task<IReadOnlyList<AiImageEndpointCapabilities>> GetArtworkEndpointsAsync(
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AiImageEndpointCapabilities>>([]);
    }

    private sealed class CredentialStore : IAiCredentialReader
    {
        public int Reads { get; private set; }
        public AiCredentialReadResult Result { get; set; } = AiCredentialReadResult.NotFound;
        public Task<AiCredentialReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            Reads++;
            return Task.FromResult(Result);
        }
    }

    private sealed class CatalogCache : IAiModelCatalogReader
    {
        public AiModelCatalog? Catalog { get; set; }
        public Task<AiModelCatalog?> LoadAsync(bool requireZeroDataRetention, CancellationToken cancellationToken = default) =>
            Task.FromResult(Catalog);
    }

}
