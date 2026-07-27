namespace FusionCanvas.Application.AI;

public sealed class AiTextGenerationService : IAiTextGenerationService
{
    private readonly IAiConfigurationProvider _configuration;
    private readonly IAiCredentialStore _credentials;
    private readonly IAiModelCatalogCache _catalogCache;
    private readonly IAiTextProvider _provider;

    public AiTextGenerationService(
        IAiConfigurationProvider configuration,
        IAiCredentialStore credentials,
        IAiModelCatalogCache catalogCache,
        IAiTextProvider provider)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        _catalogCache = catalogCache ?? throw new ArgumentNullException(nameof(catalogCache));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public async Task<AiTextResult> GenerateAsync(
        AiTextRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (!Enum.IsDefined(request.Purpose) ||
            request.Messages.Count == 0 ||
            request.Messages.Any(message => !Enum.IsDefined(message.Role) || string.IsNullOrWhiteSpace(message.Text)))
        {
            return AiTextResult.Failure(AiTextFailureKind.InvalidRequest, "Provide at least one valid text message.");
        }

        var settings = _configuration.Current;
        var catalog = await _catalogCache.LoadAsync(settings.RequireZeroDataRetention, cancellationToken)
            .ConfigureAwait(false);
        var resolution = AiConfigurationResolver.Resolve(settings, request.Purpose, catalog?.Models ?? []);
        if (!resolution.IsReady || resolution.Profile is null || resolution.Model is null)
        {
            var kind = resolution.Availability == AiConfigurationAvailability.MissingModel
                ? AiTextFailureKind.NotConfigured
                : AiTextFailureKind.InvalidConfiguration;
            return AiTextResult.Failure(kind, string.Join(" ", resolution.Errors), resolution.Profile?.ModelId);
        }

        var credential = await _credentials.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (credential.State == AiCredentialStateKind.NotFound)
        {
            return AiTextResult.Failure(AiTextFailureKind.NotConfigured, "Add an OpenRouter API key in AI settings.", resolution.Model.Id);
        }

        if (credential.State != AiCredentialStateKind.Available || string.IsNullOrWhiteSpace(credential.Secret))
        {
            return AiTextResult.Failure(
                AiTextFailureKind.CredentialUnavailable,
                credential.Message ?? "The saved OpenRouter credential is unavailable.",
                resolution.Model.Id);
        }

        return await _provider.GenerateAsync(
            new AiProviderTextRequest(
                credential.Secret,
                resolution.Model.Id,
                request.Messages,
                resolution.Profile,
                settings.RequireZeroDataRetention),
            cancellationToken).ConfigureAwait(false);
    }
}
