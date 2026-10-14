using FusionCanvas.ApiVerification.Inventory;
using FusionCanvas.ApiVerification.OpenRouter;
using FusionCanvas.ApiVerification.Printify;
using FusionCanvas.ApiVerification.Sessions;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.Listings;
using FusionCanvas.Application.Stores.Printify;
using FusionCanvas.Integration.AI;
using System.Net;
using System.Text;
using System.Text.Json;

namespace FusionCanvas.Integration.Tests.ApiVerification;

public sealed class ApiVerificationCoreTests
{
    [Fact]
    public void OperationInventoryIncludesCurrentProviderWritesAndExcludesOrders()
    {
        var ids = ApiOperationInventory.Operations.Select(operation => operation.Id).ToHashSet(StringComparer.Ordinal);

        Assert.Contains("openrouter-chat-completion", ids);
        Assert.Contains("openrouter-image-generation", ids);
        Assert.Contains("printify-image-upload", ids);
        Assert.Contains("printify-product-create", ids);
        Assert.Contains("printify-product-update", ids);
        Assert.Contains("printify-product-delete", ids);
        Assert.Contains("printify-product-publish", ids);
        Assert.Contains("printify-product-unpublish", ids);
        AssertScenarioPlans("printify-smoke", "printify-shops", "printify-blueprints", "printify-blueprint", "printify-providers", "printify-variants",
            "printify-shop-products-catalog", "printify-shop-products-import", "printify-selected-products", "printify-shop-product");
        AssertScenarioPlans("printify-product-detail", "printify-shops", "printify-shop-products-catalog", "printify-shop-products-import",
            "printify-selected-products", "printify-shop-product");
        AssertScenarioPlans("printify-artwork-import", "printify-shops", "printify-shop-products-catalog", "printify-shop-products-import",
            "printify-selected-products", "printify-shop-product", "printify-artwork-download");
        Assert.DoesNotContain(ApiOperationInventory.Operations, operation => operation.Path.Contains("order", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(ApiOperationInventory.Operations, operation => operation.CostClassification == ApiOperationCostClass.Unknown);
        Assert.All(ApiOperationInventory.Operations.Where(operation => operation.Provider == "Printify"), operation =>
            Assert.True(operation.CostClassification is ApiOperationCostClass.NoDirectCharge or ApiOperationCostClass.MayExposeSales));
        Assert.All(ApiOperationInventory.Operations.Where(operation => operation.Provider == "OpenRouter"), operation =>
        {
            Assert.Contains(operation.Scenario, ApiVerificationScenarios.OpenRouter);
            Assert.All(operation.AdditionalScenarios ?? [], scenario => Assert.Contains(scenario, ApiVerificationScenarios.OpenRouter));
        });
        Assert.All(ApiOperationInventory.Operations.Where(operation => operation.Provider == "Printify"), operation =>
        {
            Assert.Contains(operation.Scenario, ApiVerificationScenarios.Printify);
            Assert.All(operation.AdditionalScenarios ?? [], scenario => Assert.Contains(scenario, ApiVerificationScenarios.Printify));
        });
        Assert.Contains("printify-product-detail", ApiVerificationScenarios.Printify);
    }

    private static void AssertScenarioPlans(string scenario, params string[] expectedOperationIds)
    {
        var planned = ApiOperationInventory.Operations
            .Where(operation => operation.Scenario.Equals(scenario, StringComparison.OrdinalIgnoreCase)
                || operation.AdditionalScenarios?.Contains(scenario, StringComparer.OrdinalIgnoreCase) == true)
            .Select(operation => operation.Id)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Subset(planned, expectedOperationIds.ToHashSet(StringComparer.Ordinal));
    }

    [Fact]
    public void PrintifyConflictDiagnosticNamesChangedFieldsWithoutExposingValues()
    {
        var conflict = new ListingLifecycleConflict(new ListingDriftComparison(
        [
            new("variants", "private-last", "private-remote", "private-local", true, false),
            new("title", "private-title", "private-title", "private-title updated", false, true)
        ]));

        var diagnostic = PrintifyConflictDiagnostic.Format(conflict);

        Assert.Equal("title (remote changed: no; local changed: yes), variants (remote changed: yes; local changed: no)", diagnostic);
        Assert.DoesNotContain("private-", diagnostic, StringComparison.Ordinal);
    }

    [Fact]
    public void TextCostBoundIsNullWhenAnyProviderPriceIsUnknown()
    {
        var endpoint = new AiModelEndpointDescriptor("model", "provider", "Provider", "endpoint", null, null, 24, [], false, null, 0.000001m, null, null);

        Assert.Null(OpenRouterCostBound.EstimateTextMaximum(endpoint, 40, 24));
    }

    [Fact]
    public void TextCostBoundCapsAndAddsSafetyMargin()
    {
        var endpoint = new AiModelEndpointDescriptor("model", "provider", "Provider", "endpoint", null, null, 24, [], false, 0.000001m, 0.000001m, null, null);

        var actual = OpenRouterCostBound.EstimateTextMaximum(endpoint, 40, 24);

        Assert.Equal(0.000080m, actual);
    }

    [Fact]
    public void ImageCostBoundBlocksUnknownBillingUnitsAndAppliesPixelQuantity()
    {
        var endpoint = new AiImageEndpointCapabilities("endpoint", "model", false, true, ["png"], [new AiImageSize(1024, 1024)], false,
            Pricing: [new("output_megapixel", 0.01m, "megapixel")]);

        Assert.Equal(0.05m, OpenRouterCostBound.EstimateImageMaximum(endpoint, new AiImageSize(1024, 1024), 2));
        Assert.Null(OpenRouterCostBound.EstimateImageMaximum(endpoint with { Pricing = [new("output_image", 0.01m, "token")] }, new AiImageSize(1024, 1024), 1));
    }

    [Fact]
    public void ImageCostBoundUsesDocumentedGoogleTokenCountAndBoundsInputText()
    {
        var endpoint = new AiImageEndpointCapabilities(
            "google-ai-studio",
            "google/gemini-3.1-flash-image",
            false,
            true,
            ["png"],
            [],
            false,
            Pricing: [new("output_image", 0.00006m, "token"), new("input_text", 0.0000005m, "token")]);

        var actual = OpenRouterCostBound.EstimateImageMaximum(endpoint, new AiImageSize(1024, 1024), 1, 54);

        Assert.Equal(0.08403375m, actual);
        Assert.Null(OpenRouterCostBound.EstimateImageMaximum(
            endpoint with { ModelId = "unknown/image-model" }, new AiImageSize(1024, 1024), 1, 54));
    }

    [Fact]
    public void InvocationRequiresPositiveMaximumCost()
    {
        var exception = Assert.Throws<ArgumentException>(() => VerificationInvocation.Parse(["run", "--provider", "openrouter", "--max-cost", "0"]));

        Assert.Contains("positive USD", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void InvocationParsesExplicitProviderScenarioModelsBudgetAndForce()
    {
        var invocation = VerificationInvocation.Parse(["qualify", "--provider", "openrouter", "--models", "model/a,model/b", "--max-cost", "0.25", "--force"]);

        Assert.Equal("qualify", invocation.Command);
        Assert.Equal("openrouter", invocation.Provider);
        Assert.Equal(["model/a", "model/b"], invocation.Models);
        Assert.Equal(0.25m, invocation.MaximumCostUsd);
        Assert.True(invocation.Force);
    }

    [Fact]
    public void QualificationSelectionUsesOnlyCatalogModelsAndDeduplicatesPerOperation()
    {
        var selection = OpenRouterQualificationMatrix.Select(
            ["TEXT/model-a", "text/model-a", "image/model-b", "missing/model"],
            ["text/model-a"],
            ["image/model-b"]);

        Assert.Collection(selection.Candidates,
            candidate => Assert.Equal(new OpenRouterQualificationCandidate("text/model-a", "text"), candidate),
            candidate => Assert.Equal(new OpenRouterQualificationCandidate("image/model-b", "image"), candidate));
        Assert.Equal(["missing/model"], selection.UnknownModelIds);
        Assert.False(selection.RequestedAll);
    }

    [Fact]
    public void QualificationRejectsAllMixedWithExplicitModels()
    {
        var selection = OpenRouterQualificationMatrix.Select(["all", "text/model-a"], ["text/model-a"], []);

        Assert.Empty(selection.Candidates);
        Assert.Empty(selection.UnknownModelIds);
        Assert.True(selection.RequestedAll);
        Assert.True(selection.InvalidAllCombination);
    }

    [Fact]
    public void QualificationAllBuildsSeparateTextAndImageCandidates()
    {
        var selection = OpenRouterQualificationMatrix.Select(["all"], ["text/model-a"], ["image/model-b"]);

        Assert.Equal([
            new OpenRouterQualificationCandidate("text/model-a", "text"),
            new OpenRouterQualificationCandidate("image/model-b", "image")
        ], selection.Candidates);
        Assert.Empty(selection.UnknownModelIds);
    }

    [Fact]
    public void QualificationProfilesContainOnlyTheSelectedModelsAdvertisedEffectiveParameters()
    {
        var samplingModel = new AiModelDescriptor("provider/sampling", "Sampling", null, null, ["text"], ["text"],
            [AiParameterRegistry.MaxCompletionTokens, AiParameterRegistry.Temperature, AiParameterRegistry.TopP],
            1000, 1024, null, null, false, null);
        var reasoningModel = new AiModelDescriptor("provider/reasoning", "Reasoning", null, null, ["text"], ["text"],
            [AiParameterRegistry.Seed, AiParameterRegistry.Reasoning],
            1000, 32, null, null, false, new(false, false, ["low"], "low", false));

        var sampling = OpenRouterApiVerifier.BuildTestProfile(samplingModel, 512);
        var reasoning = OpenRouterApiVerifier.BuildTestProfile(reasoningModel, 24);

        Assert.Equal(512, sampling.MaxCompletionTokens);
        Assert.Equal(0.2, sampling.Temperature);
        Assert.Equal(0.9, sampling.TopP);
        Assert.Null(sampling.Seed);
        Assert.Equal(7, reasoning.Seed);
        Assert.Null(reasoning.Temperature);
        Assert.Equal(AiReasoningMode.Effort, reasoning.Reasoning.Mode);
        Assert.Equal("low", reasoning.Reasoning.Effort);
        Assert.Empty(AiParameterRegistry.Validate(sampling, samplingModel));
        Assert.Empty(AiParameterRegistry.Validate(reasoning, reasoningModel));
    }

    [Fact]
    public void QualificationProfileUsesIntersectionOfModelAndSelectedEndpointParameters()
    {
        var model = new AiModelDescriptor("provider/model", "Model", null, null, ["text"], ["text"],
            [AiParameterRegistry.MaxCompletionTokens, AiParameterRegistry.Temperature, AiParameterRegistry.Seed],
            1000, 1024, null, null, false, null);

        var profile = OpenRouterApiVerifier.BuildTestProfile(
            model,
            [AiParameterRegistry.MaxCompletionTokens, AiParameterRegistry.Temperature],
            512);

        Assert.Equal(512, profile.MaxCompletionTokens);
        Assert.Equal(0.2, profile.Temperature);
        Assert.Null(profile.Seed);
        Assert.Empty(AiParameterRegistry.Validate(profile, model));
    }

    [Fact]
    public void QualificationProfileRecognizesLegacyMaxTokensAliasForBoundedRequests()
    {
        var model = new AiModelDescriptor("provider/model", "Model", null, null, ["text"], ["text"],
            ["max_tokens", AiParameterRegistry.Temperature], 1000, 1024, null, null, false, null);

        var profile = OpenRouterApiVerifier.BuildTestProfile(model, ["max_tokens", AiParameterRegistry.Temperature], 512);

        Assert.Equal(512, profile.MaxCompletionTokens);
        Assert.Empty(AiParameterRegistry.Validate(profile, model));
    }

    [Fact]
    public void QualificationSummaryShowsTheCompletionTokenFieldActuallySentToTheEndpoint()
    {
        var profile = AiProfileSettings.Empty with
        {
            MaxCompletionTokens = 512,
            Routing = AiRoutingPolicy.ForEndpoint("openai/flex", "openai", "max_tokens")
        };

        var summary = OpenRouterApiVerifier.ProfileSummary(profile);

        Assert.Contains("max_tokens=512", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("max_completion_tokens=", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void QualificationSubsetExpandsAllOnlyWithinTheRequestedModality()
    {
        var known = new[] { ("text-a", "text"), ("text-b", "text"), ("image-a", "image") };

        var valid = OpenRouterApiVerifier.TryResolveQualificationSubset("text:all", known, out var selected);

        Assert.True(valid);
        Assert.Equal(new[] { ("text-a", "text"), ("text-b", "text") }, selected.OrderBy(value => value.Id));
        Assert.False(OpenRouterApiVerifier.TryResolveQualificationSubset("audio:all", known, out _));
        Assert.False(OpenRouterApiVerifier.TryResolveQualificationSubset("text:missing", known, out _));
    }

    [Fact]
    public void PrintifyDevTestShopResolutionUsesExactTitleAndReturnsNumericId()
    {
        var shops = new[]
        {
            new PrintifyShopOption(1, "Other Shop"),
            new PrintifyShopOption(987654, VerificationConfiguration.RequiredPrintifyDevTestShopName)
        };

        var selected = PrintifyApiVerifier.ResolveDevTestShop(shops, VerificationConfiguration.RequiredPrintifyDevTestShopName);

        Assert.Equal(987654, selected.Id);
    }

    [Fact]
    public void PrintifyDevTestShopResolutionRejectsMissingDuplicateAndNonExactTitles()
    {
        var title = VerificationConfiguration.RequiredPrintifyDevTestShopName;

        Assert.Throws<InvalidOperationException>(() => PrintifyApiVerifier.ResolveDevTestShop(
            [new PrintifyShopOption(1, "fusioncanvas devtest")], title));
        Assert.Throws<InvalidOperationException>(() => PrintifyApiVerifier.ResolveDevTestShop(
            [new PrintifyShopOption(1, title), new PrintifyShopOption(2, title)], title));
        Assert.Throws<InvalidOperationException>(() => PrintifyApiVerifier.ResolveDevTestShop(
            [new PrintifyShopOption(0, title)], title));
    }

    [Fact]
    public void PrintifyRecoveryRequiresSessionJournalAndExactDevTestOwnership()
    {
        var runId = Guid.NewGuid().ToString("N");
        var journal = new PrintifyResourceJournal(1, runId, 123, VerificationConfiguration.RequiredPrintifyDevTestShopName,
            "product-create", "product-456", "cleanup-pending", DateTimeOffset.UtcNow);

        var recovered = PrintifyRecoveryEligibility.Resolve(runId, 123, journal, null);

        Assert.Equal(new PrintifyRecoveryTarget("product-456", AlreadyDeleted: false), recovered);
        Assert.Null(PrintifyRecoveryEligibility.Resolve(runId, 124, journal, null));
        Assert.Null(PrintifyRecoveryEligibility.Resolve(runId, 123, journal with { ShopName = "Production" }, null));
        Assert.Null(PrintifyRecoveryEligibility.Resolve(runId, 123, journal with { RunId = Guid.NewGuid().ToString("N") }, null));
        Assert.Null(PrintifyRecoveryEligibility.Resolve(runId, 123, null, new(1, runId, "Printify", "printify-product-lifecycle",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "passed", [], [], null, null, [])));
    }

    [Fact]
    public void PrintifyRecoveryRefusesMismatchedReportAndAlreadyDeletedResource()
    {
        var runId = Guid.NewGuid().ToString("N");
        var journal = new PrintifyResourceJournal(1, runId, 123, VerificationConfiguration.RequiredPrintifyDevTestShopName,
            "product-create", "product-456", "owned", DateTimeOffset.UtcNow);
        var report = new VerificationSessionReport(1, runId, "Printify", "printify-product-lifecycle",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "passed",
            [new("printify-product-create", "passed", ResourceId: "product-456")], [], null, null, []);

        Assert.Equal("product-456", PrintifyRecoveryEligibility.Resolve(runId, 123, journal, report)?.ProductId);
        Assert.Null(PrintifyRecoveryEligibility.Resolve(runId, 123, journal, report with
        {
            Operations = [new("printify-product-create", "passed", ResourceId: "somebody-elses-product")]
        }));
        Assert.True(PrintifyRecoveryEligibility.Resolve(runId, 123, journal with { Status = "deleted" }, report)?.AlreadyDeleted);
    }

    [Fact]
    public async Task ReportWriterAddsUnrunInventoryRowsAndRedactsSecretLikeText()
    {
        var directory = Path.Combine(Path.GetTempPath(), "FusionCanvas.ApiVerification.Tests", Guid.NewGuid().ToString("N"));
        try
        {
            var report = new VerificationSessionReport(1, Guid.NewGuid().ToString("N"), "OpenRouter", "openrouter-smoke",
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "passed",
                [new("openrouter-key", "passed", "api_key=live-value")],
                [new(Guid.NewGuid().ToString("N"), "openrouter-key", "open", "token: live-value", "valid", "token: live-value", null, DateTimeOffset.UtcNow)],
                null, null, []);

            await new VerificationReportWriter(directory).WriteAsync(report);
            var jsonPath = Assert.Single(Directory.EnumerateFiles(directory, "*.json"));
            using var json = JsonDocument.Parse(await File.ReadAllTextAsync(jsonPath));
            var operations = json.RootElement.GetProperty("Operations").EnumerateArray().ToArray();

            Assert.Contains(operations, operation => operation.GetProperty("OperationId").GetString() == "openrouter-chat-completion"
                && operation.GetProperty("Status").GetString() == "not-run");
            Assert.DoesNotContain("live-value", await File.ReadAllTextAsync(jsonPath), StringComparison.Ordinal);
            var markdownPath = Assert.Single(Directory.EnumerateFiles(directory, "*.md"));
            Assert.DoesNotContain("live-value", await File.ReadAllTextAsync(markdownPath), StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task QualificationEvidenceMatchesModelOperationAndFingerprintAndReplacesDuplicate()
    {
        var directory = Path.Combine(Path.GetTempPath(), "FusionCanvas.ApiVerification.Tests", Guid.NewGuid().ToString("N"));
        try
        {
            var store = new OpenRouterQualificationEvidenceStore(directory);
            var first = new OpenRouterQualificationEvidence("provider/model", "text", "profile-a", "passed", 0.001m, DateTimeOffset.UtcNow);
            await store.SaveAsync(first);
            var replaced = first with { ActualCostUsd = 0.002m, VerifiedAt = DateTimeOffset.UtcNow.AddMinutes(1) };
            await store.SaveAsync(replaced);

            Assert.Equal(replaced, await store.FindAsync("PROVIDER/MODEL", "text", "profile-a"));
            Assert.Null(await store.FindAsync("provider/model", "image", "profile-a"));
            Assert.Null(await store.FindAsync("provider/model", "text", "profile-b"));
            using var json = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory, "openrouter-qualification.json")));
            Assert.Single(json.RootElement.EnumerateArray());
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void QualificationFingerprintChangesWhenEffectiveProfileOrAdapterRevisionChanges()
    {
        var baseline = OpenRouterQualificationEvidenceStore.CreateFingerprint(new { Model = "provider/model", Temperature = 0.2, Adapter = "1" });

        Assert.Equal(baseline, OpenRouterQualificationEvidenceStore.CreateFingerprint(new { Model = "provider/model", Temperature = 0.2, Adapter = "1" }));
        Assert.NotEqual(baseline, OpenRouterQualificationEvidenceStore.CreateFingerprint(new { Model = "provider/model", Temperature = 0.3, Adapter = "1" }));
        Assert.NotEqual(baseline, OpenRouterQualificationEvidenceStore.CreateFingerprint(new { Model = "provider/model", Temperature = 0.2, Adapter = "2" }));
    }

    [Fact]
    public void QualificationAdapterRevisionUsesCommitAwareInformationalVersion()
    {
        var assembly = typeof(OpenRouterClient).Assembly;
        var revision = OpenRouterQualificationEvidenceStore.GetAdapterRevision(assembly);
        var informationalVersion = assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .Single()
            .InformationalVersion;

        Assert.Equal(informationalVersion, revision);
        Assert.NotEqual(assembly.GetName().Version?.ToString(), revision);
    }

    [Theory]
    [InlineData("budget-deferred", "partial")]
    [InlineData("budget-blocked", "partial")]
    [InlineData("rate-limited", "partial")]
    [InlineData("passed", "passed")]
    [InlineData("reused", "passed")]
    public void OpenRouterQualificationStatusDoesNotReportDeferredCoverageAsPassed(string operationStatus, string expected)
    {
        var operations = new[] { new VerificationOperationResult("openrouter-text-generation", operationStatus, ModelId: "provider/model") };

        Assert.Equal(expected, OpenRouterApiVerifier.ResolveSessionStatus("openrouter-qualification", "passed", operations));
    }

    [Fact]
    public void OpenRouterQualificationFailureTakesPrecedenceOverDeferredCoverage()
    {
        var operations = new[]
        {
            new VerificationOperationResult("openrouter-text-generation", "budget-deferred", ModelId: "provider/model-a"),
            new VerificationOperationResult("openrouter-image-generation", "failed", ModelId: "provider/model-b")
        };

        Assert.Equal("failed", OpenRouterApiVerifier.ResolveSessionStatus("openrouter-qualification", "passed", operations));
    }

    [Fact]
    public void OpenRouterRateLimitedRunIsPartialRatherThanPassedOrModelFailure()
    {
        var operations = new[] { new VerificationOperationResult("openrouter-chat-completion", "rate-limited", ModelId: "provider/model") };

        Assert.Equal("partial", OpenRouterApiVerifier.ResolveSessionStatus("openrouter-text-generation", "passed", operations));
    }

    [Fact]
    public async Task ProviderVerifiersBlockBeforeNetworkWhenDedicatedCredentialsAreMissing()
    {
        var directory = Path.Combine(Path.GetTempPath(), "FusionCanvas.ApiVerification.Tests", Guid.NewGuid().ToString("N"));
        var configuration = new VerificationConfiguration(null, null, VerificationConfiguration.RequiredPrintifyDevTestShopName, null, directory);
        try
        {
            var openRouter = await new OpenRouterApiVerifier(configuration).RunAsync("openrouter-smoke", [], null, false, CancellationToken.None);
            var printify = await new PrintifyApiVerifier(configuration).RunAsync("printify-smoke", CancellationToken.None);

            Assert.Equal(3, openRouter);
            Assert.Equal(3, printify);
            Assert.Equal(2, Directory.EnumerateFiles(directory, "*.json").Count());
            var reports = Directory.EnumerateFiles(directory, "*.json").Select(File.ReadAllText).ToArray();
            Assert.All(reports, report => Assert.Contains("blocked", report, StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task PrintifyCancellationBeforeSessionConfirmationMakesNoProviderCalls()
    {
        var directory = Path.Combine(Path.GetTempPath(), "FusionCanvas.ApiVerification.Tests", Guid.NewGuid().ToString("N"));
        var configuration = new VerificationConfiguration(null, "devtest-key", VerificationConfiguration.RequiredPrintifyDevTestShopName, null, directory);
        string? displayedPlan = null;
        var runtime = new PrintifyApiVerifierRuntime(
            () => throw new InvalidOperationException("Network client should not be created after cancellation."),
            () => throw new InvalidOperationException("Network client should not be created after cancellation."),
            () => throw new InvalidOperationException("Network client should not be created after cancellation."),
            () => throw new InvalidOperationException("Network client should not be created after cancellation."),
            () => throw new InvalidOperationException("Network client should not be created after cancellation."),
            (instruction, _, _) =>
            {
                displayedPlan = instruction;
                return Task.FromResult(false);
            });
        try
        {
            var exitCode = await new PrintifyApiVerifier(configuration, runtime).RunAsync("printify-product-lifecycle", CancellationToken.None);

            Assert.Equal(2, exitCode);
            Assert.Contains("printify-image-upload", displayedPlan, StringComparison.Ordinal);
            Assert.Contains("printify-product-create", displayedPlan, StringComparison.Ordinal);
            Assert.Contains("recover --provider printify --run-id", displayedPlan, StringComparison.Ordinal);
            Assert.Contains("Excluded: orders and chargeable operations.", displayedPlan, StringComparison.Ordinal);
            var reportPath = Assert.Single(Directory.EnumerateFiles(directory, "*.json"));
            var json = await File.ReadAllTextAsync(reportPath);
            Assert.Contains("cancelled", json, StringComparison.Ordinal);
            Assert.DoesNotContain("devtest-key", json, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task PrintifyCredentialFailureReportPreservesSafeStatusAndMessage()
    {
        var directory = Path.Combine(Path.GetTempPath(), "FusionCanvas.ApiVerification.Tests", Guid.NewGuid().ToString("N"));
        var configuration = new VerificationConfiguration(null, "devtest-key", VerificationConfiguration.RequiredPrintifyDevTestShopName, null, directory);
        var handler = new JsonResponseHandler("{}", HttpStatusCode.Unauthorized);
        var runtime = new PrintifyApiVerifierRuntime(
            () => new HttpClient(handler, disposeHandler: false),
            () => throw new InvalidOperationException("Catalog access must be blocked after credential verification fails."),
            () => throw new InvalidOperationException("Import access must be blocked after credential verification fails."),
            () => throw new InvalidOperationException("Artwork access must be blocked after credential verification fails."),
            () => throw new InvalidOperationException("Listing access must be blocked after credential verification fails."),
            (_, _, _) => Task.FromResult(true));
        try
        {
            var exitCode = await new PrintifyApiVerifier(configuration, runtime).RunAsync("printify-catalog", CancellationToken.None);

            Assert.Equal(1, exitCode);
            Assert.Single(handler.Requests);
            var reportPath = Assert.Single(Directory.EnumerateFiles(directory, "*.json"));
            var report = await File.ReadAllTextAsync(reportPath);
            Assert.Contains("InvalidKey", report, StringComparison.Ordinal);
            Assert.Contains("invalid or expired", report, StringComparison.Ordinal);
            Assert.DoesNotContain("devtest-key", report, StringComparison.Ordinal);
        }
        finally
        {
            handler.Dispose();
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task PrintifyShopMismatchStopsBeforeCatalogAndAnyWriteOperation()
    {
        var directory = Path.Combine(Path.GetTempPath(), "FusionCanvas.ApiVerification.Tests", Guid.NewGuid().ToString("N"));
        var configuration = new VerificationConfiguration(null, "devtest-key", VerificationConfiguration.RequiredPrintifyDevTestShopName, null, directory);
        var handler = new JsonResponseHandler("""[{"id":123,"title":"Another Shop"}]""");
        var runtime = new PrintifyApiVerifierRuntime(
            () => new HttpClient(handler, disposeHandler: false),
            () => throw new InvalidOperationException("Catalog access must be blocked after shop mismatch."),
            () => throw new InvalidOperationException("Import access must be blocked after shop mismatch."),
            () => throw new InvalidOperationException("Artwork access must be blocked after shop mismatch."),
            () => throw new InvalidOperationException("Listing access must be blocked after shop mismatch."),
            (_, _, _) => Task.FromResult(true));
        try
        {
            var exitCode = await new PrintifyApiVerifier(configuration, runtime).RunAsync("printify-product-lifecycle", CancellationToken.None);

            Assert.Equal(1, exitCode);
            Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, handler.Methods[0]);
            Assert.Equal("/v1/shops.json", handler.Requests[0].AbsolutePath);
            Assert.All(handler.Methods, method => Assert.Equal(HttpMethod.Get, method));
            var reportPath = Assert.Single(Directory.EnumerateFiles(directory, "*.json"));
            Assert.Contains("No shop titled exactly", await File.ReadAllTextAsync(reportPath), StringComparison.Ordinal);
        }
        finally
        {
            handler.Dispose();
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class JsonResponseHandler(string body, HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];
        public List<HttpMethod> Methods { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            Methods.Add(request.Method);
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
