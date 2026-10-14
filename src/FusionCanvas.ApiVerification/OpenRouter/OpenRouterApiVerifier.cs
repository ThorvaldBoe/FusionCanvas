using System.Net.Http;
using System.Text;
using FusionCanvas.ApiVerification.Inventory;
using FusionCanvas.ApiVerification.Sessions;
using FusionCanvas.Application.AI;
using FusionCanvas.Integration.AI;

namespace FusionCanvas.ApiVerification.OpenRouter;

internal sealed record OpenRouterQualificationRoute(
    OpenRouterQualificationCandidate Candidate,
    string EndpointId,
    decimal MaximumCost,
    bool EvidenceReused);

internal sealed class OpenRouterApiVerifier(VerificationConfiguration configuration)
{
    private const int MaximumCompletionTokens = 512;
    private const string ImageQualificationPrompt = "A simple solid blue circle on a plain white background.";

    public async Task<int> RunAsync(
        string scenario,
        IReadOnlyList<string> requestedModels,
        decimal? maximumCostUsd,
        bool force,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var runId = Guid.NewGuid().ToString("N");
        Console.WriteLine($"Session run ID: {runId}");
        var operations = new List<VerificationOperationResult>();
        var findings = new List<VerificationFinding>();
        var cleanup = new List<string>();
        var plannedOperations = ApiOperationInventory.Operations
            .Where(operation => operation.Provider == "OpenRouter"
                && (operation.Scenario.Equals(scenario, StringComparison.OrdinalIgnoreCase)
                    || operation.AdditionalScenarios?.Contains(scenario, StringComparer.OrdinalIgnoreCase) == true))
            .ToArray();
        if (plannedOperations.Length == 0 || plannedOperations.Any(operation => operation.CostClassification == ApiOperationCostClass.Unknown))
        {
            operations.Add(new(scenario, "blocked", "The selected operation has no inventory entry or an unknown cost classification; no provider request was sent."));
            await WriteReportAsync(runId, scenario, now, "blocked", operations, findings, null, null, cleanup, cancellationToken).ConfigureAwait(false);
            return 3;
        }
        if (string.IsNullOrWhiteSpace(configuration.OpenRouterApiKey))
        {
            Console.Error.WriteLine("Set FUSIONCANVAS_OPENROUTER_TEST_API_KEY to a dedicated test key. No request was sent.");
            operations.Add(new(scenario, "blocked", "Dedicated OpenRouter test credential is not configured."));
            await WriteReportAsync(runId, scenario, now, "blocked", operations, findings, null, null, cleanup, cancellationToken).ConfigureAwait(false);
            return 3;
        }

        var ceiling = maximumCostUsd ?? configuration.OpenRouterSessionCostCeiling;
        var preflight = BuildRunPlan(scenario, requestedModels, plannedOperations, ceiling);
        Console.WriteLine(preflight);
        decimal? estimatedMaximum = null;
        decimal? actualCost = null;
        var status = "passed";

        if (!await SessionConfirmation.RequirePhraseAsync(
                preflight + "\nThe discovery phase sends only the listed reads. Each billable generation requires a separate confirmation after its exact endpoint, effective profile, and conservative maximum are shown.",
                $"RUN OPENROUTER {scenario.ToUpperInvariant()}", cancellationToken).ConfigureAwait(false))
        {
            status = "cancelled";
            operations.Add(new("session-confirmation", "cancelled", "No provider request was sent."));
            await WriteReportAsync(runId, scenario, now, status, operations, findings, estimatedMaximum, actualCost, cleanup, cancellationToken).ConfigureAwait(false);
            return 2;
        }

        using var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan, BaseAddress = OpenRouterClient.DefaultBaseAddress };
        var service = new OpenRouterClient(client);
        try
        {
            switch (scenario)
            {
                case "openrouter-smoke":
                    await RunCredentialCheckAsync(service, operations, cancellationToken).ConfigureAwait(false);
                    break;
                case "openrouter-catalog":
                    await RunCatalogAsync(service, requestedModels, operations, cancellationToken).ConfigureAwait(false);
                    break;
                case "openrouter-text-generation":
                    if (requestedModels.Count != 1)
                    {
                        throw new InvalidOperationException("Text generation requires exactly one --models model ID.");
                    }
                    if (ceiling is null or <= 0)
                    {
                        throw new InvalidOperationException("Text generation requires --max-cost or FUSIONCANVAS_OPENROUTER_SESSION_MAX_COST_USD.");
                    }
                    (estimatedMaximum, actualCost) = await RunTextGenerationAsync(service, requestedModels[0], ceiling.Value, operations, findings, cancellationToken, force: force).ConfigureAwait(false);
                    break;
                case "openrouter-image-generation":
                    if (requestedModels.Count != 1)
                    {
                        throw new InvalidOperationException("Image generation requires exactly one --models model ID.");
                    }
                    if (ceiling is null or <= 0)
                    {
                        throw new InvalidOperationException("Image generation requires --max-cost or FUSIONCANVAS_OPENROUTER_SESSION_MAX_COST_USD.");
                    }
                    (estimatedMaximum, actualCost) = await RunImageGenerationAsync(service, requestedModels[0], ceiling.Value, operations, findings, cancellationToken, force: force).ConfigureAwait(false);
                    break;
                case "openrouter-qualification":
                    if (ceiling is null or <= 0)
                    {
                        throw new InvalidOperationException("Model qualification requires --max-cost or FUSIONCANVAS_OPENROUTER_SESSION_MAX_COST_USD.");
                    }
                    (estimatedMaximum, actualCost) = await RunQualificationAsync(service, requestedModels, ceiling.Value, force, operations, findings, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported OpenRouter scenario: {SafeDiagnostic.Redact(scenario)}");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            status = "cancelled";
            operations.Add(new(scenario, "cancelled", "Session cancellation was requested."));
        }
        catch (Exception exception)
        {
            status = "failed";
            var diagnostic = SafeDiagnostic.Redact(exception.Message);
            operations.Add(new(scenario, "failed", Diagnostic: diagnostic));
            findings.Add(new(Guid.NewGuid().ToString("N"), scenario, "open", "request-or-response", "The production OpenRouter operation completes with a documented contract result.", diagnostic, "src/FusionCanvas.Integration/AI/OpenRouterClient.cs", DateTimeOffset.UtcNow));
            Console.Error.WriteLine(diagnostic);
        }

        status = ResolveSessionStatus(scenario, status, operations);

        await WriteReportAsync(runId, scenario, now, status, operations, findings, estimatedMaximum, actualCost, cleanup, cancellationToken).ConfigureAwait(false);
        return status == "passed" ? 0 : 1;
    }

    internal static string ResolveSessionStatus(
        string scenario,
        string currentStatus,
        IReadOnlyCollection<VerificationOperationResult> operations)
    {
        if (operations.Any(operation => operation.Status is "failed" or "blocked"))
        {
            return operations.Any(operation => operation.Status == "failed") ? "failed" : "blocked";
        }

        if (operations.Any(operation => operation.Status == "rate-limited"))
        {
            return "partial";
        }

        if (scenario.Equals("openrouter-qualification", StringComparison.OrdinalIgnoreCase)
            && operations.Any(operation => operation.Status is "budget-deferred" or "budget-blocked"))
        {
            return "partial";
        }

        return currentStatus;
    }

    private async Task RunCredentialCheckAsync(OpenRouterClient service, ICollection<VerificationOperationResult> operations, CancellationToken cancellationToken)
    {
        var result = await service.ValidateAsync(configuration.OpenRouterApiKey!, cancellationToken).ConfigureAwait(false);
        operations.Add(new("openrouter-key", result.Kind == AiCredentialValidationKind.Valid ? "passed" : "failed", result.Kind.ToString(), SafeDiagnostic.Redact(result.Message)));
    }

    private async Task RunCatalogAsync(OpenRouterClient service, IReadOnlyList<string> requestedModels, ICollection<VerificationOperationResult> operations, CancellationToken cancellationToken)
    {
        await RunCredentialCheckAsync(service, operations, cancellationToken).ConfigureAwait(false);
        var text = await service.GetModelsAsync(configuration.OpenRouterApiKey!, false, cancellationToken).ConfigureAwait(false);
        operations.Add(new("openrouter-models", "passed", $"Loaded {text.Models.Count} text-capable models."));
        var textZdr = await service.GetModelsAsync(configuration.OpenRouterApiKey!, true, cancellationToken).ConfigureAwait(false);
        operations.Add(new("openrouter-zdr-endpoints", "passed", $"Loaded {textZdr.Models.Count} ZDR-compatible text models through the production catalog path."));
        var images = await service.GetImageModelsAsync(configuration.OpenRouterApiKey!, false, cancellationToken).ConfigureAwait(false);
        operations.Add(new("openrouter-image-models", "passed", $"Loaded {images.Models.Count} image models."));
        var imageZdr = await service.GetImageModelsAsync(configuration.OpenRouterApiKey!, true, cancellationToken).ConfigureAwait(false);
        operations.Add(new("openrouter-zdr-endpoints", "passed", $"Loaded {imageZdr.Models.Count} ZDR-compatible image models through the production catalog path."));

        if (requestedModels.Count == 0)
        {
            operations.Add(new("openrouter-text-endpoints", "not-run", "Pass --models with one or more model IDs to exercise endpoint discovery."));
            operations.Add(new("openrouter-image-endpoints", "not-run", "Pass --models with one or more model IDs to exercise image endpoint discovery."));
            return;
        }

        foreach (var modelId in requestedModels)
        {
            if (text.Models.Any(model => model.Id.Equals(modelId, StringComparison.OrdinalIgnoreCase)))
            {
                var endpoints = await service.GetEndpointsAsync(configuration.OpenRouterApiKey!, modelId, false, cancellationToken).ConfigureAwait(false);
                operations.Add(new("openrouter-text-endpoints", "passed", $"{modelId}: loaded {endpoints.Endpoints.Count} endpoints.", ModelId: modelId));
            }

            if (images.Models.Any(model => model.Id.Equals(modelId, StringComparison.OrdinalIgnoreCase)))
            {
                var endpoints = await service.GetImageEndpointsAsync(configuration.OpenRouterApiKey!, modelId, false, cancellationToken).ConfigureAwait(false);
                var endpointSummary = string.Join("; ", endpoints.Select(endpoint =>
                    $"{endpoint.ProviderName ?? "unknown provider"}/{endpoint.EndpointId}: parameters={FormatImageEndpointCapabilities(endpoint)}"));
                operations.Add(new("openrouter-image-endpoints", "passed", $"{modelId}: loaded {endpoints.Count} endpoints. {endpointSummary}", ModelId: modelId));
            }
        }
    }

    private static string FormatImageEndpointCapabilities(AiImageEndpointCapabilities endpoint)
    {
        var parameters = endpoint.Parameters;
        return string.Join(", ", new[]
        {
            parameters?.Resolutions.Count > 0 ? $"resolution[{string.Join("|", parameters.Resolutions)}]" : null,
            parameters?.AspectRatios.Count > 0 ? $"aspect_ratio[{string.Join("|", parameters.AspectRatios)}]" : null,
            parameters?.SupportsExplicitSize == true ? "size" : null,
            parameters?.SupportsOutputFormat == true ? "output_format" : null,
            parameters?.SupportsImageCount == true ? "n" : null,
            parameters?.Backgrounds.Count > 0 ? $"background[{string.Join("|", parameters.Backgrounds)}]" : null
        }.Where(value => value is not null));
    }

    private async Task<(decimal Estimated, decimal? Actual)> RunTextGenerationAsync(
        OpenRouterClient service,
        string modelId,
        decimal ceiling,
        ICollection<VerificationOperationResult> operations,
        ICollection<VerificationFinding> findings,
        CancellationToken cancellationToken,
        bool allowEvidenceReuse = false,
        bool force = false,
        string? selectedEndpointId = null)
    {
        var models = await service.GetModelsAsync(configuration.OpenRouterApiKey!, false, cancellationToken).ConfigureAwait(false);
        var model = models.Models.SingleOrDefault(candidate => candidate.Id.Equals(modelId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"OpenRouter did not list the selected text model `{SafeDiagnostic.Redact(modelId)}`.");
        var endpointCatalog = await service.GetEndpointsAsync(configuration.OpenRouterApiKey!, model.Id, false, cancellationToken).ConfigureAwait(false);
        var endpoint = endpointCatalog.Endpoints
            .Where(candidate => candidate.PromptPrice is not null && candidate.CompletionPrice is not null)
            .Where(candidate => candidate.SupportedParameters.Contains(AiParameterRegistry.MaxCompletionTokens, StringComparer.Ordinal)
                || candidate.SupportedParameters.Contains("max_tokens", StringComparer.Ordinal))
            .Where(candidate => SelectCompletionTokenParameter(model.SupportedParameters, candidate.SupportedParameters) is not null)
            .Where(candidate => selectedEndpointId is null || candidate.EndpointId.Equals(selectedEndpointId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(candidate => candidate.PromptPrice + candidate.CompletionPrice)
            .FirstOrDefault() ?? throw new InvalidOperationException("No exact model endpoint advertises bounded completion tokens and current prompt/completion prices; generation is blocked.");
        var maximumTokens = SelectMaximumCompletionTokens(model, endpoint);
        var profileTemplate = BuildTestProfile(model, endpoint.SupportedParameters, maximumTokens);
        var completionTokenParameter = SelectCompletionTokenParameter(model.SupportedParameters, endpoint.SupportedParameters);
        var profile = profileTemplate with { Routing = AiRoutingPolicy.ForEndpoint(endpoint.EndpointId, endpoint.ProviderId, completionTokenParameter) };
        var prompt = "Reply with exactly the word OK.";
        var promptBytes = Encoding.UTF8.GetByteCount(prompt);
        var estimate = OpenRouterCostBound.EstimateTextMaximum(endpoint, promptBytes, maximumTokens)
            ?? throw new InvalidOperationException("A conservative text generation cost bound could not be calculated.");
        var fingerprint = CreateTextQualificationFingerprint(model, endpoint, profile);
        if (allowEvidenceReuse && !force && await TryLoadQualificationEvidenceAsync(model.Id, "text", fingerprint, cancellationToken).ConfigureAwait(false) is { Status: "passed" } evidence)
        {
            operations.Add(new("openrouter-chat-completion", "reused", $"Matching passing evidence from {evidence.VerifiedAt:u}; no generation request sent. provider={endpoint.ProviderId}; request={ProfileSummary(profile)}", ModelId: model.Id, ProfileFingerprint: fingerprint, ResourceId: endpoint.EndpointId));
            return (0, null);
        }
        if (estimate > ceiling)
        {
            operations.Add(new("openrouter-chat-completion", "blocked", $"Estimated maximum {FormatUsd(estimate)} USD exceeds the session ceiling {FormatUsd(ceiling)} USD. provider={endpoint.ProviderId}; request={ProfileSummary(profile)}", ModelId: model.Id, ResourceId: endpoint.EndpointId));
            return (estimate, null);
        }
        Console.WriteLine($"Text model: {model.Id}");
        Console.WriteLine($"Endpoint: {endpoint.ProviderName} / {endpoint.EndpointId}");
        var endpointParameters = endpoint.SupportedParameters.ToHashSet(StringComparer.Ordinal);
        var endpointGaps = model.SupportedParameters.Where(parameter => !endpointParameters.Contains(parameter)).OrderBy(parameter => parameter, StringComparer.Ordinal).ToArray();
        if (endpointGaps.Length > 0)
            Console.WriteLine($"Model parameters omitted for this endpoint: {string.Join(", ", endpointGaps)}");
        Console.WriteLine($"Endpoint-supported parameters: {string.Join(", ", endpoint.SupportedParameters.OrderBy(parameter => parameter, StringComparer.Ordinal))}");
        Console.WriteLine($"Effective request parameters: {ProfileSummary(profile)}");
        Console.WriteLine($"Cumulative conservative maximum: {FormatUsd(estimate)} USD / ceiling {FormatUsd(ceiling)} USD");
        if (!await SessionConfirmation.RequirePhraseAsync("This sends one short generation request to OpenRouter.", $"GENERATE {model.Id}", cancellationToken).ConfigureAwait(false))
        {
            operations.Add(new("openrouter-chat-completion", "cancelled", "Generation was declined.", ModelId: model.Id, ProfileFingerprint: fingerprint));
            return (estimate, null);
        }

        var request = new AiProviderTextRequest(
            configuration.OpenRouterApiKey!, model.Id, [new AiTextMessage(AiMessageRole.User, prompt)], profile, false, profile.Routing);
        var result = await service.GenerateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            var diagnostic = SafeDiagnostic.Redact(result.Message ?? result.FailureKind?.ToString() ?? "OpenRouter rejected the generation.");
            var failureStatus = result.FailureKind == AiTextFailureKind.RateLimited ? "rate-limited" : "failed";
            operations.Add(new("openrouter-chat-completion", failureStatus, Diagnostic: diagnostic, ModelId: model.Id, ProfileFingerprint: fingerprint, ResourceId: endpoint.EndpointId, Summary: $"provider={endpoint.ProviderId}; request={ProfileSummary(profile)}"));
            if (failureStatus == "failed")
            {
                var category = result.FailureKind switch
                {
                    AiTextFailureKind.RouteNotFulfilled or AiTextFailureKind.NoEligibleProvider or AiTextFailureKind.ModelUnavailable => "provider-route",
                    AiTextFailureKind.InvalidProviderResponse => "response-contract",
                    AiTextFailureKind.InvalidConfiguration => "effective-parameters",
                    _ => "generation-contract"
                };
                findings.Add(new(Guid.NewGuid().ToString("N"), "openrouter-chat-completion", "open", category, "The selected model and pinned endpoint accept the effective FusionCanvas request profile.", diagnostic, "src/FusionCanvas.Integration/AI/OpenRouterClient.cs", DateTimeOffset.UtcNow));
            }
            return (estimate, result.Usage?.Cost);
        }

        var actual = result.Usage?.Cost;
        var status = actual is { } actualCost && actualCost > ceiling ? "failed" : "passed";
        operations.Add(new("openrouter-chat-completion", status, $"Resolved model {result.ActualModel ?? result.RequestedModel}; finish reason {result.FinishReason ?? "not reported"}. provider={endpoint.ProviderId}; request={ProfileSummary(profile)}", ModelId: model.Id, ProfileFingerprint: fingerprint, CostUsd: actual, ResourceId: endpoint.EndpointId));
        if (status == "failed")
        {
            findings.Add(new(Guid.NewGuid().ToString("N"), "openrouter-chat-completion", "open", "session-cost-ceiling", $"Charge at or below {FormatUsd(ceiling)} USD", $"Provider reported {FormatUsd(actual ?? 0m)} USD", "src/FusionCanvas.Integration/AI/OpenRouterClient.cs", DateTimeOffset.UtcNow));
        }
        else
        {
            await SaveQualificationEvidenceAsync(model.Id, "text", fingerprint, status, actual, cancellationToken).ConfigureAwait(false);
        }

        return (estimate, actual);
    }

    private async Task<(decimal Estimated, decimal? Actual)> RunImageGenerationAsync(
        OpenRouterClient service,
        string modelId,
        decimal ceiling,
        ICollection<VerificationOperationResult> operations,
        ICollection<VerificationFinding> findings,
        CancellationToken cancellationToken,
        bool allowEvidenceReuse = false,
        bool force = false,
        string? selectedEndpointId = null)
    {
        var models = await service.GetImageModelsAsync(configuration.OpenRouterApiKey!, false, cancellationToken).ConfigureAwait(false);
        var model = models.Models.SingleOrDefault(candidate => candidate.Id.Equals(modelId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"OpenRouter did not list the selected image model `{SafeDiagnostic.Redact(modelId)}`.");
        var endpoints = await service.GetImageEndpointsAsync(configuration.OpenRouterApiKey!, model.Id, false, cancellationToken).ConfigureAwait(false);
        var endpoint = endpoints
            .Where(candidate => candidate.Pricing is { Count: > 0 })
            .Where(candidate => candidate.SupportsImageOutput)
            .Where(candidate => selectedEndpointId is null || candidate.EndpointId.Equals(selectedEndpointId, StringComparison.OrdinalIgnoreCase))
            .Select(candidate => new
            {
                Endpoint = candidate,
                Selection = AiImageEndpointPolicy.SelectEndpoint([candidate], model.Id, false, false, new AiImageSize(1024, 1024))
            })
            .Where(candidate => candidate.Selection is not null)
            .Select(candidate => new
            {
                candidate.Endpoint,
                Selection = candidate.Selection!,
                MaximumCost = OpenRouterCostBound.EstimateImageMaximum(
                    candidate.Endpoint,
                    candidate.Selection!.ProviderSize,
                    1,
                    Encoding.UTF8.GetByteCount(ImageQualificationPrompt))
            })
            .Where(candidate => candidate.MaximumCost is not null)
            .OrderBy(candidate => candidate.MaximumCost)
            .FirstOrDefault() ?? throw new InvalidOperationException("No image endpoint has supported output dimensions and recognized per-image pricing; generation is blocked.");
        var maximumCost = endpoint.MaximumCost ?? throw new InvalidOperationException("An image cost bound is unavailable.");
        var fingerprint = OpenRouterQualificationEvidenceStore.CreateFingerprint(new { model.Id, model.SupportedParameters, endpoint.Endpoint, endpoint.Selection, AdapterRevision = OpenRouterQualificationEvidenceStore.GetAdapterRevision(typeof(OpenRouterClient).Assembly) });
        if (allowEvidenceReuse && !force && await TryLoadQualificationEvidenceAsync(model.Id, "image", fingerprint, cancellationToken).ConfigureAwait(false) is { Status: "passed" } evidence)
        {
            operations.Add(new("openrouter-image-generation", "reused", $"Matching passing evidence from {evidence.VerifiedAt:u}; no generation request sent. provider={endpoint.Endpoint.ProviderName}; options={FormatImageOptions(endpoint.Selection.Options)}.", ModelId: model.Id, ProfileFingerprint: fingerprint, ResourceId: endpoint.Endpoint.EndpointId));
            return (0, null);
        }
        if (maximumCost > ceiling)
        {
            operations.Add(new("openrouter-image-generation", "blocked", $"Estimated maximum {FormatUsd(maximumCost)} USD exceeds the session ceiling {FormatUsd(ceiling)} USD; provider={endpoint.Endpoint.ProviderName}; options={FormatImageOptions(endpoint.Selection.Options)}.", ModelId: model.Id, ResourceId: endpoint.Endpoint.EndpointId));
            return (maximumCost, null);
        }
        Console.WriteLine($"Image model: {model.Id}");
        Console.WriteLine($"Endpoint: {endpoint.Endpoint.ProviderName} / {endpoint.Endpoint.EndpointId}");
        Console.WriteLine($"Output: {endpoint.Selection.ProviderSize.Width} x {endpoint.Selection.ProviderSize.Height}, one image");
        Console.WriteLine($"Effective image parameters: {FormatImageOptions(endpoint.Selection.Options)}");
        Console.WriteLine($"Conservative maximum: {FormatUsd(endpoint.MaximumCost.Value)} USD / ceiling {FormatUsd(ceiling)} USD");
        if (!await SessionConfirmation.RequirePhraseAsync("This sends one image generation request to OpenRouter. The generated image is discarded after verifying the response.", $"GENERATE {model.Id}", cancellationToken).ConfigureAwait(false))
        {
            operations.Add(new("openrouter-image-generation", "cancelled", "Generation was declined.", ModelId: model.Id, ProfileFingerprint: fingerprint, ResourceId: endpoint.Endpoint.EndpointId));
            return (maximumCost, null);
        }

        var result = await service.GenerateAsync(new AiImageGenerationRequest(
            model.Id,
            ImageQualificationPrompt,
            endpoint.Selection.ProviderSize,
            TransparentBackground: false,
            ApiKey: configuration.OpenRouterApiKey!,
            RequireZeroDataRetention: false,
            ProviderTag: endpoint.Endpoint.EndpointId,
            Options: endpoint.Selection.Options), cancellationToken).ConfigureAwait(false);
        if (result.Failure is not null || result.Result is null)
        {
            var diagnostic = SafeDiagnostic.Redact(result.Failure?.Message ?? "The image API returned no image result.");
            var failureStatus = result.Failure?.Kind == AiImageGenerationFailureKind.RateLimited ? "rate-limited" : "failed";
            operations.Add(new("openrouter-image-generation", failureStatus, $"provider={endpoint.Endpoint.ProviderName}; options={FormatImageOptions(endpoint.Selection.Options)}.", Diagnostic: diagnostic, ModelId: model.Id, ProfileFingerprint: fingerprint, ResourceId: endpoint.Endpoint.EndpointId));
            if (failureStatus == "failed")
            {
                var category = result.Failure?.Kind == AiImageGenerationFailureKind.InvalidResponse ? "image-response-contract" : "image-provider-route";
                findings.Add(new(Guid.NewGuid().ToString("N"), "openrouter-image-generation", "open", category, "The selected image endpoint accepts the effective FusionCanvas request.", diagnostic, "src/FusionCanvas.Integration/AI/OpenRouterClient.cs", DateTimeOffset.UtcNow));
            }
            return (maximumCost, result.Result?.Usage?.Cost);
        }

        var actual = result.Result.Usage?.Cost;
        var status = actual is { } actualCost && actualCost > ceiling ? "failed" : "passed";
        operations.Add(new("openrouter-image-generation", status, $"Received {result.Result.MediaType}, {result.Result.ImageBytes.Length} bytes from {result.Result.ResolvedModelId}; provider={endpoint.Endpoint.ProviderName}; options={FormatImageOptions(endpoint.Selection.Options)}.", ModelId: model.Id, ProfileFingerprint: fingerprint, CostUsd: actual, ResourceId: endpoint.Endpoint.EndpointId));
        if (status == "passed")
        {
            await SaveQualificationEvidenceAsync(model.Id, "image", fingerprint, status, actual, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            findings.Add(new(Guid.NewGuid().ToString("N"), "openrouter-image-generation", "open", "session-cost-ceiling", $"Charge at or below {FormatUsd(ceiling)} USD", $"Provider reported {FormatUsd(actual ?? 0m)} USD", "src/FusionCanvas.Integration/AI/OpenRouterClient.cs", DateTimeOffset.UtcNow));
        }

        return (maximumCost, actual);
    }

    private async Task<(decimal Estimated, decimal? Actual)> RunQualificationAsync(
        OpenRouterClient service,
        IReadOnlyList<string> requestedModels,
        decimal ceiling,
        bool force,
        ICollection<VerificationOperationResult> operations,
        ICollection<VerificationFinding> findings,
        CancellationToken cancellationToken)
    {
        if (requestedModels.Count == 0)
        {
            throw new InvalidOperationException("Qualification requires --models all or one or more comma-separated model IDs.");
        }

        var textCatalog = await service.GetModelsAsync(configuration.OpenRouterApiKey!, false, cancellationToken).ConfigureAwait(false);
        var imageCatalog = await service.GetImageModelsAsync(configuration.OpenRouterApiKey!, false, cancellationToken).ConfigureAwait(false);
        var selection = OpenRouterQualificationMatrix.Select(
            requestedModels,
            textCatalog.Models.Select(model => model.Id),
            imageCatalog.Models.Select(model => model.Id));
        var candidates = selection.Candidates;
        if (selection.InvalidAllCombination || selection.UnknownModelIds.Count > 0 || candidates.Count == 0)
        {
            var reason = selection.InvalidAllCombination
                ? "The `all` keyword cannot be combined with explicit model IDs. No generation requests were sent."
                : selection.UnknownModelIds.Count > 0
                ? $"The requested model IDs were not present in the corresponding OpenRouter catalogs: {string.Join(", ", selection.UnknownModelIds.Select(SafeDiagnostic.Redact))}. No generation requests were sent."
                : "The selected catalogs contained no qualifying models. No generation requests were sent.";
            operations.Add(new("openrouter-qualification", "blocked", reason));
            return (0, null);
        }

        Console.WriteLine($"Qualification candidates: {candidates.Count} model/modality pairs; every priced endpoint with a bounded profile is considered separately.");
        var plannedRoutes = new List<OpenRouterQualificationRoute>();
        decimal plannedMaximum = 0;
        foreach (var candidate in candidates)
        {
            IReadOnlyList<OpenRouterQualificationRoute> routes;
            try
            {
                routes = await EstimateCandidateRoutesAsync(
                    service, candidate, textCatalog.Models, imageCatalog.Models, operations, findings, force, cancellationToken).ConfigureAwait(false);
            }
            catch (AiModelCatalogFetchException exception)
            {
                var diagnostic = SafeDiagnostic.Redact(exception.Message);
                operations.Add(new(
                    $"openrouter-{candidate.Kind}-endpoints",
                    "failed",
                    $"Could not qualify {candidate.Kind} model {candidate.Id}: {diagnostic}",
                    ModelId: candidate.Id));
                findings.Add(new(
                    Guid.NewGuid().ToString("N"),
                    $"openrouter-{candidate.Kind}-endpoints",
                    "open",
                    "request-or-response",
                    "The model endpoint catalog returns a supported endpoint contract.",
                    diagnostic,
                    "src/FusionCanvas.Integration/AI/OpenRouterClient.cs",
                    DateTimeOffset.UtcNow));
                continue;
            }

            foreach (var route in routes)
            {
                plannedRoutes.Add(route);
                if (!route.EvidenceReused)
                    plannedMaximum += route.MaximumCost;
                Console.WriteLine($"  {candidate.Kind}: {candidate.Id} / {route.EndpointId} (max {(route.EvidenceReused ? "0.000000" : FormatUsd(route.MaximumCost))} USD{(route.EvidenceReused ? ", passing evidence" : string.Empty)})");
            }
        }

        if (plannedRoutes.Count == 0)
        {
            operations.Add(new("openrouter-qualification", "budget-deferred", "No endpoint/profile pair had a conservative cost bound; no generation request was sent."));
            return (0, null);
        }

        Console.WriteLine($"Aggregate conservative maximum for new calls: {FormatUsd(plannedMaximum)} USD / session ceiling {FormatUsd(ceiling)} USD");
        IReadOnlySet<(string Id, string Kind)>? selectedKeys = null;
        if (plannedMaximum > ceiling)
        {
            Console.WriteLine("Select models by entering comma-separated `text:model/id` and/or `image:model/id` entries from the preflight above. A selected model includes each bounded endpoint/profile pair. Use `text:all` or `image:all` for every model in that modality. Press Enter to defer all.");
            var selected = await Console.In.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (!TryResolveQualificationSubset(selected ?? string.Empty, plannedRoutes.Select(route => (route.Candidate.Id, route.Candidate.Kind)), out var canonicalSelectedKeys))
            {
                operations.Add(new("openrouter-qualification", "budget-deferred", "No valid subset was selected; no generation requests were sent."));
                return (plannedMaximum, null);
            }
            selectedKeys = canonicalSelectedKeys;
            var selectedRoutes = plannedRoutes.Where(route => selectedKeys.Contains((route.Candidate.Id, route.Candidate.Kind))).ToArray();
            plannedMaximum = selectedRoutes.Where(route => !route.EvidenceReused).Sum(route => route.MaximumCost);
            if (plannedMaximum > ceiling)
            {
                operations.Add(new("openrouter-qualification", "budget-blocked", $"Selected subset maximum {FormatUsd(plannedMaximum)} USD still exceeds the {FormatUsd(ceiling)} USD ceiling; no generation requests were sent."));
                return (plannedMaximum, null);
            }
            foreach (var route in plannedRoutes.Where(route => !selectedKeys.Contains((route.Candidate.Id, route.Candidate.Kind))))
                operations.Add(new($"openrouter-{route.Candidate.Kind}-generation", "budget-deferred", "Not selected from the preflight matrix.", ModelId: route.Candidate.Id, ResourceId: route.EndpointId));
        }

        var selectedPlan = selectedKeys is null
            ? plannedRoutes
            : plannedRoutes.Where(route => selectedKeys.Contains((route.Candidate.Id, route.Candidate.Kind))).ToList();
        Console.WriteLine($"Existing evidence is {(force ? "ignored" : "reused when fingerprints match")}; each new endpoint generation still needs its own confirmation.");
        if (!await SessionConfirmation.RequirePhraseAsync("Catalog discovery has completed. Qualification may send bounded generation calls for the listed model and endpoint profiles.", "QUALIFY OPENROUTER", cancellationToken).ConfigureAwait(false))
        {
            operations.Add(new("openrouter-qualification", "cancelled", "Qualification calls were declined."));
            return (0, null);
        }

        decimal estimatedTotal = 0;
        decimal actualTotal = 0;
        var hasActual = false;
        foreach (var route in selectedPlan)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var before = operations.Count;
            decimal estimate;
            decimal? actual;
            if (route.Candidate.Kind == "text")
            {
                (estimate, actual) = await RunTextGenerationAsync(
                    service, route.Candidate.Id, ceiling - estimatedTotal, operations, findings, cancellationToken,
                    allowEvidenceReuse: true, force: force, selectedEndpointId: route.EndpointId).ConfigureAwait(false);
            }
            else
            {
                (estimate, actual) = await RunImageGenerationAsync(
                    service, route.Candidate.Id, ceiling - estimatedTotal, operations, findings, cancellationToken,
                    allowEvidenceReuse: true, force: force, selectedEndpointId: route.EndpointId).ConfigureAwait(false);
            }

            estimatedTotal += estimate;
            if (actual is { } actualCost)
            {
                hasActual = true;
                actualTotal += actualCost;
            }

            if (actualTotal > ceiling)
            {
                operations.Add(new("openrouter-qualification", "failed", $"Provider-reported cumulative charge {FormatUsd(actualTotal)} USD exceeded the {FormatUsd(ceiling)} USD ceiling; stopped further generation."));
                findings.Add(new(Guid.NewGuid().ToString("N"), "openrouter-qualification", "open", "session-cost-ceiling", $"Cumulative charge at or below {FormatUsd(ceiling)} USD", $"Provider-reported {FormatUsd(actualTotal)} USD", "src/FusionCanvas.ApiVerification/OpenRouter/OpenRouterApiVerifier.cs", DateTimeOffset.UtcNow));
                break;
            }

            if (estimatedTotal > ceiling)
            {
                operations.Add(new("openrouter-qualification", "budget-blocked", $"Stopped before exceeding the {FormatUsd(ceiling)} USD ceiling."));
                break;
            }

            if (operations.Count == before)
            {
                operations.Add(new($"openrouter-qualification-{route.Candidate.Kind}", "not-run", route.Candidate.Id, ModelId: route.Candidate.Id, ResourceId: route.EndpointId));
            }
        }

        return (estimatedTotal, hasActual ? actualTotal : null);
    }

    private async Task<IReadOnlyList<OpenRouterQualificationRoute>> EstimateCandidateRoutesAsync(
        OpenRouterClient service,
        OpenRouterQualificationCandidate candidate,
        IReadOnlyCollection<AiModelDescriptor> textModels,
        IReadOnlyCollection<AiModelDescriptor> imageModels,
        ICollection<VerificationOperationResult> operations,
        ICollection<VerificationFinding> findings,
        bool force,
        CancellationToken cancellationToken)
    {
        var routes = new List<OpenRouterQualificationRoute>();
        if (candidate.Kind == "text")
        {
            var model = textModels.SingleOrDefault(model => model.Id.Equals(candidate.Id, StringComparison.OrdinalIgnoreCase));
            if (model is null) return routes;
            var endpoints = await service.GetEndpointsAsync(configuration.OpenRouterApiKey!, model.Id, false, cancellationToken).ConfigureAwait(false);
            var eligible = endpoints.Endpoints
                .Where(endpoint => endpoint.PromptPrice is not null && endpoint.CompletionPrice is not null)
                .Where(endpoint => SelectCompletionTokenParameter(model.SupportedParameters, endpoint.SupportedParameters) is not null)
                .OrderBy(endpoint => endpoint.PromptPrice + endpoint.CompletionPrice)
                .ToArray();
            if (eligible.Length == 0)
            {
                operations.Add(new($"openrouter-{candidate.Kind}-generation", "budget-deferred", "No endpoint exposes both a conservative prompt/completion price and a jointly supported completion-token limit; no request was sent.", ModelId: model.Id));
                return routes;
            }

            foreach (var endpoint in eligible)
            {
                if (!TrySelectMaximumCompletionTokens(model, endpoint, out var maxTokens))
                {
                    RecordQualificationPlanningFailure(
                        candidate,
                        endpoint.EndpointId,
                        "The model or endpoint catalog did not provide a positive completion-token limit; no generation request was sent.",
                        operations,
                        findings);
                    continue;
                }

                var completionTokenParameter = SelectCompletionTokenParameter(model.SupportedParameters, endpoint.SupportedParameters)!;
                AiProfileSettings profile;
                try
                {
                    profile = BuildTestProfile(model, endpoint.SupportedParameters, maxTokens) with
                    {
                        Routing = AiRoutingPolicy.ForEndpoint(endpoint.EndpointId, endpoint.ProviderId, completionTokenParameter)
                    };
                }
                catch (InvalidOperationException exception)
                {
                    RecordQualificationPlanningFailure(
                        candidate,
                        endpoint.EndpointId,
                        SafeDiagnostic.Redact(exception.Message),
                        operations,
                        findings);
                    continue;
                }

                var maximumCost = OpenRouterCostBound.EstimateTextMaximum(
                    endpoint, Encoding.UTF8.GetByteCount("Reply with exactly the word OK."), maxTokens);
                if (maximumCost is null)
                {
                    operations.Add(new($"openrouter-{candidate.Kind}-generation", "budget-deferred", "A conservative request cost could not be calculated for this endpoint; no request was sent.", ModelId: model.Id, ResourceId: endpoint.EndpointId));
                    continue;
                }

                var fingerprint = CreateTextQualificationFingerprint(model, endpoint, profile);
                var evidence = !force
                    ? await TryLoadQualificationEvidenceAsync(model.Id, "text", fingerprint, cancellationToken).ConfigureAwait(false)
                    : null;
                var reused = evidence?.Status == "passed";
                Console.WriteLine($"    endpoint: {endpoint.ProviderName} / {endpoint.EndpointId}; maximum completion tokens: {maxTokens}");
                Console.WriteLine($"    effective parameters: {ProfileSummary(profile)}");
                routes.Add(new(candidate, endpoint.EndpointId, maximumCost.Value, reused));
            }

            return routes;
        }

        var imageModel = imageModels.SingleOrDefault(model => model.Id.Equals(candidate.Id, StringComparison.OrdinalIgnoreCase));
        if (imageModel is null) return routes;
        var imageEndpoints = await service.GetImageEndpointsAsync(configuration.OpenRouterApiKey!, imageModel.Id, false, cancellationToken).ConfigureAwait(false);
        var eligibleImageEndpoints = imageEndpoints
            .Where(endpoint => endpoint.Pricing is { Count: > 0 } && endpoint.SupportsImageOutput)
            .Select(endpoint => new
            {
                Endpoint = endpoint,
                Selection = AiImageEndpointPolicy.SelectEndpoint([endpoint], imageModel.Id, false, false, new AiImageSize(1024, 1024))
            })
            .Where(selection => selection.Selection is not null)
            .Select(selection => new
            {
                selection.Endpoint,
                Selection = selection.Selection!,
                MaximumCost = OpenRouterCostBound.EstimateImageMaximum(
                    selection.Endpoint,
                    selection.Selection!.ProviderSize,
                    1,
                    Encoding.UTF8.GetByteCount(ImageQualificationPrompt))
            })
            .ToArray();
        if (eligibleImageEndpoints.Length == 0)
        {
            operations.Add(new($"openrouter-{candidate.Kind}-generation", "budget-deferred", "No image endpoint has supported output dimensions and recognized per-image pricing; no request was sent.", ModelId: imageModel.Id));
            return routes;
        }

        foreach (var endpoint in eligibleImageEndpoints)
        {
            if (endpoint.MaximumCost is not { } maximumCost)
            {
                operations.Add(new($"openrouter-{candidate.Kind}-generation", "budget-deferred", "A conservative image cost could not be calculated for this endpoint; no request was sent.", ModelId: imageModel.Id, ResourceId: endpoint.Endpoint.EndpointId));
                continue;
            }

            var fingerprint = CreateImageQualificationFingerprint(imageModel, endpoint.Endpoint, endpoint.Selection);
            var evidence = !force
                ? await TryLoadQualificationEvidenceAsync(imageModel.Id, "image", fingerprint, cancellationToken).ConfigureAwait(false)
                : null;
            var reused = evidence?.Status == "passed";
            Console.WriteLine($"    endpoint: {endpoint.Endpoint.ProviderName} / {endpoint.Endpoint.EndpointId}");
            Console.WriteLine($"    image output: {endpoint.Selection.ProviderSize.Width} x {endpoint.Selection.ProviderSize.Height}, 1 image");
            Console.WriteLine($"    effective image parameters: {FormatImageOptions(endpoint.Selection.Options)}");
            routes.Add(new(candidate, endpoint.Endpoint.EndpointId, maximumCost, reused));
        }

        return routes;
    }

    private static void RecordQualificationPlanningFailure(
        OpenRouterQualificationCandidate candidate,
        string endpointId,
        string diagnostic,
        ICollection<VerificationOperationResult> operations,
        ICollection<VerificationFinding> findings)
    {
        const string operationId = "openrouter-chat-completion";
        operations.Add(new(
            operationId,
            "blocked",
            "The advertised model/endpoint profile cannot be sent safely.",
            diagnostic,
            candidate.Id,
            ResourceId: endpointId));
        findings.Add(new(
            Guid.NewGuid().ToString("N"),
            operationId,
            "open",
            "request-or-response",
            "The selected model and endpoint expose a valid, conservatively bounded FusionCanvas request profile.",
            diagnostic,
            "src/FusionCanvas.ApiVerification/OpenRouter/OpenRouterApiVerifier.cs",
            DateTimeOffset.UtcNow));
    }

    internal static AiProfileSettings BuildTestProfile(AiModelDescriptor model, int maximumTokens)
        => BuildTestProfile(model, model.SupportedParameters, maximumTokens);

    internal static int SelectMaximumCompletionTokens(AiModelDescriptor model, AiModelEndpointDescriptor endpoint)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(endpoint);
        var maximum = Math.Min(MaximumCompletionTokens, model.MaxCompletionTokens ?? MaximumCompletionTokens);
        return Math.Min(maximum, endpoint.MaxCompletionTokens ?? maximum);
    }

    internal static string CreateTextQualificationFingerprint(
        AiModelDescriptor model,
        AiModelEndpointDescriptor endpoint,
        AiProfileSettings profile) => OpenRouterQualificationEvidenceStore.CreateFingerprint(new
        {
            model.Id,
            ModelParameters = model.SupportedParameters.OrderBy(parameter => parameter, StringComparer.Ordinal).ToArray(),
            model.ContextLength,
            model.MaxCompletionTokens,
            Endpoint = new
            {
                endpoint.ModelId,
                endpoint.ProviderId,
                endpoint.EndpointId,
                endpoint.ContextLength,
                endpoint.MaxCompletionTokens,
                SupportedParameters = endpoint.SupportedParameters.OrderBy(parameter => parameter, StringComparer.Ordinal).ToArray(),
                endpoint.ZeroDataRetentionCompatible,
                endpoint.PromptPrice,
                endpoint.CompletionPrice
            },
            profile,
            OutputModalities = new[] { "text" },
            AdapterRevision = OpenRouterQualificationEvidenceStore.GetAdapterRevision(typeof(OpenRouterClient).Assembly)
        });

    internal static string CreateImageQualificationFingerprint(
        AiModelDescriptor model,
        AiImageEndpointCapabilities endpoint,
        AiImageEndpointPolicy.EndpointSelection selection) => OpenRouterQualificationEvidenceStore.CreateFingerprint(new
        {
            model.Id,
            ModelParameters = model.SupportedParameters.OrderBy(parameter => parameter, StringComparer.Ordinal).ToArray(),
            Endpoint = new
            {
                endpoint.EndpointId,
                endpoint.ModelId,
                endpoint.ZeroDataRetentionCompatible,
                endpoint.SupportsImageOutput,
                RasterFormats = endpoint.RasterFormats.OrderBy(format => format, StringComparer.Ordinal).ToArray(),
                SupportedSizes = endpoint.SupportedSizes.OrderBy(size => size.Width).ThenBy(size => size.Height).ToArray(),
                endpoint.SupportsTransparency,
                Parameters = endpoint.Parameters is null ? null : new
                {
                    AspectRatios = endpoint.Parameters.AspectRatios.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                    Resolutions = endpoint.Parameters.Resolutions.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                    endpoint.Parameters.SupportsExplicitSize,
                    endpoint.Parameters.SupportsOutputFormat,
                    Backgrounds = endpoint.Parameters.Backgrounds.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                    endpoint.Parameters.SupportsImageCount
                },
                Pricing = endpoint.Pricing?.OrderBy(price => price.Billable, StringComparer.Ordinal)
                    .ThenBy(price => price.Unit, StringComparer.Ordinal)
                    .ThenBy(price => price.CostUsd).ToArray()
            },
            selection.ProviderSize,
            selection.Options,
            AdapterRevision = OpenRouterQualificationEvidenceStore.GetAdapterRevision(typeof(OpenRouterClient).Assembly)
        });

    internal static bool TrySelectMaximumCompletionTokens(
        AiModelDescriptor model,
        AiModelEndpointDescriptor endpoint,
        out int maximum)
    {
        maximum = SelectMaximumCompletionTokens(model, endpoint);
        return maximum > 0;
    }

    internal static AiProfileSettings BuildTestProfile(
        AiModelDescriptor model,
        IReadOnlyCollection<string> endpointSupportedParameters,
        int maximumTokens)
    {
        var supported = model.SupportedParameters
            .Intersect(endpointSupportedParameters, StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);
        var reasoning = AiReasoningSettings.ProviderDefault;
        if (supported.Contains(AiParameterRegistry.Reasoning) && model.Reasoning is { } capabilities)
        {
            reasoning = capabilities.SupportedEfforts.FirstOrDefault() is { Length: > 0 } effort
                ? new AiReasoningSettings(AiReasoningMode.Effort, effort)
                : capabilities.SupportsTokenBudget
                    ? new AiReasoningSettings(AiReasoningMode.TokenBudget, TokenBudget: Math.Min(8, maximumTokens))
                    : AiReasoningSettings.ProviderDefault;
        }

        var profile = AiProfileSettings.Empty with
        {
            ModelId = model.Id,
            MaxCompletionTokens = supported.Contains(AiParameterRegistry.MaxCompletionTokens) || supported.Contains("max_tokens") ? maximumTokens : null,
            Temperature = supported.Contains(AiParameterRegistry.Temperature) ? 0.2 : null,
            TopP = supported.Contains(AiParameterRegistry.TopP) ? 0.9 : null,
            TopK = supported.Contains(AiParameterRegistry.TopK) ? 20 : null,
            MinP = supported.Contains(AiParameterRegistry.MinP) ? 0.05 : null,
            TopA = supported.Contains(AiParameterRegistry.TopA) ? 0.1 : null,
            FrequencyPenalty = supported.Contains(AiParameterRegistry.FrequencyPenalty) ? 0.1 : null,
            PresencePenalty = supported.Contains(AiParameterRegistry.PresencePenalty) ? 0.1 : null,
            RepetitionPenalty = supported.Contains(AiParameterRegistry.RepetitionPenalty) ? 1.05 : null,
            Seed = supported.Contains(AiParameterRegistry.Seed) ? 7 : null,
            StopSequences = supported.Contains(AiParameterRegistry.Stop) ? ["<END>"] : [],
            Reasoning = reasoning
        };
        var effective = AiParameterRegistry.Effective(profile, model);
        var validation = AiParameterRegistry.Validate(effective, model);
        if (validation.Count > 0)
        {
            throw new InvalidOperationException($"The model catalog advertised a profile rejected by FusionCanvas validation: {SafeDiagnostic.Redact(string.Join("; ", validation))}");
        }

        return effective;
    }

    private static string? SelectCompletionTokenParameter(
        IReadOnlyCollection<string> modelParameters,
        IReadOnlyCollection<string> endpointParameters)
    {
        var supported = modelParameters.Intersect(endpointParameters, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        return supported.Contains(AiParameterRegistry.MaxCompletionTokens)
            ? AiParameterRegistry.MaxCompletionTokens
            : supported.Contains("max_tokens") ? "max_tokens" : null;
    }

    private static string FormatImageOptions(AiImageGenerationOptions options) =>
        string.Join("; ", new[]
        {
            options.Size is { } size ? $"size={size.Width}x{size.Height}" : null,
            options.Resolution is { } resolution ? $"resolution={resolution}" : null,
            options.AspectRatio is { } aspectRatio ? $"aspect_ratio={aspectRatio}" : null,
            options.OutputFormat is { } format ? $"output_format={format}" : null,
            options.Background is { } background ? $"background={background}" : null,
            options.Count is { } count ? $"n={count}" : null
        }.Where(value => value is not null));

    internal static string ProfileSummary(AiProfileSettings profile)
    {
        var completionTokenParameter = profile.Routing?.CompletionTokenParameter ?? AiParameterRegistry.MaxCompletionTokens;
        if (completionTokenParameter is not (AiParameterRegistry.MaxCompletionTokens or "max_tokens"))
        {
            completionTokenParameter = AiParameterRegistry.MaxCompletionTokens;
        }

        return $"modalities=[text]; {completionTokenParameter}={profile.MaxCompletionTokens?.ToString() ?? "omitted"}; temperature={profile.Temperature?.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) ?? "omitted"}; top_p={profile.TopP?.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) ?? "omitted"}; top_k={profile.TopK?.ToString() ?? "omitted"}; min_p={profile.MinP?.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) ?? "omitted"}; top_a={profile.TopA?.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) ?? "omitted"}; frequency_penalty={profile.FrequencyPenalty?.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) ?? "omitted"}; presence_penalty={profile.PresencePenalty?.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) ?? "omitted"}; repetition_penalty={profile.RepetitionPenalty?.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) ?? "omitted"}; seed={profile.Seed?.ToString() ?? "omitted"}; stop=[{string.Join(",", profile.StopSequences)}]; reasoning={profile.Reasoning.Mode}({profile.Reasoning.Effort ?? "default"},{profile.Reasoning.TokenBudget?.ToString() ?? "no-budget"})";
    }

    internal static bool TryResolveQualificationSubset(
        string selection,
        IEnumerable<(string Id, string Kind)> knownPairs,
        out IReadOnlySet<(string Id, string Kind)> selectedPairs)
    {
        var known = knownPairs.ToArray();
        var requested = selection.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(value =>
            {
                var separator = value.IndexOf(':');
                return separator > 0
                    ? (Id: value[(separator + 1)..], Kind: value[..separator].ToLowerInvariant())
                    : (Id: string.Empty, Kind: string.Empty);
            }).ToArray();
        var resolved = new HashSet<(string Id, string Kind)>();
        foreach (var key in requested)
        {
            var matching = known.Where(candidate => candidate.Kind.Equals(key.Kind, StringComparison.OrdinalIgnoreCase));
            if (key.Id.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                if (key.Kind is not ("text" or "image"))
                {
                    selectedPairs = resolved;
                    return false;
                }

                foreach (var candidate in matching) resolved.Add(candidate);
                continue;
            }

            var pair = matching.FirstOrDefault(candidate => candidate.Id.Equals(key.Id, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(key.Id) || !matching.Any(candidate => candidate.Id.Equals(key.Id, StringComparison.OrdinalIgnoreCase)))
            {
                selectedPairs = resolved;
                return false;
            }
            resolved.Add(pair);
        }

        selectedPairs = resolved;
        return resolved.Count > 0;
    }

    private static string FormatUsd(decimal amount) => amount.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture);

    private static string BuildRunPlan(
        string scenario,
        IReadOnlyList<string> requestedModels,
        IReadOnlyList<ApiOperationDefinition> operations,
        decimal? ceiling)
    {
        var lines = new List<string>
        {
            $"OpenRouter preflight — scenario: {scenario}",
            $"Endpoint: {OpenRouterClient.DefaultBaseAddress}",
            $"Selected models: {(requestedModels.Count == 0 ? "catalog discovery" : string.Join(", ", requestedModels.Select(SafeDiagnostic.Redact)))}",
            $"Hard session ceiling: {ceiling?.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture) ?? "[missing]"} USD",
            "Planned operations:"
        };
        lines.AddRange(operations.Select(operation => $"- {operation.Id}: {operation.Method} {operation.Path} [{operation.Effect}; {operation.CostClassification}]"));
        lines.Add("Generation cost is not assumed: endpoint pricing is loaded first, and unknown or over-budget calls are blocked before submission.");
        lines.Add("Recovery: no remote resource is created; any generated image bytes are discarded after verification.");
        return string.Join(Environment.NewLine, lines);
    }

    private async Task SaveQualificationEvidenceAsync(string modelId, string operation, string fingerprint, string status, decimal? actualCost, CancellationToken cancellationToken)
    {
        await new OpenRouterQualificationEvidenceStore(configuration.DataDirectory).SaveAsync(
            new(modelId, operation, fingerprint, status, actualCost, DateTimeOffset.UtcNow), cancellationToken).ConfigureAwait(false);
    }

    private async Task<OpenRouterQualificationEvidence?> TryLoadQualificationEvidenceAsync(string modelId, string operation, string fingerprint, CancellationToken cancellationToken)
    {
        return await new OpenRouterQualificationEvidenceStore(configuration.DataDirectory)
            .FindAsync(modelId, operation, fingerprint, cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteReportAsync(
        string runId,
        string scenario,
        DateTimeOffset startedAt,
        string status,
        IReadOnlyList<VerificationOperationResult> operations,
        IReadOnlyList<VerificationFinding> findings,
        decimal? estimatedMaximum,
        decimal? actualCost,
        IReadOnlyList<string> cleanup,
        CancellationToken cancellationToken)
    {
        var report = new VerificationSessionReport(1, runId, "OpenRouter", scenario, startedAt, DateTimeOffset.UtcNow,
            status, operations, findings, estimatedMaximum, actualCost, cleanup);
        await new VerificationReportWriter(configuration.DataDirectory).WriteAsync(report, cancellationToken).ConfigureAwait(false);
    }

}
