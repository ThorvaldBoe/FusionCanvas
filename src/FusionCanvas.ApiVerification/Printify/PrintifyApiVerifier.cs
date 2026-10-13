using System.Security.Cryptography;
using FusionCanvas.ApiVerification.Inventory;
using FusionCanvas.ApiVerification.Sessions;
using FusionCanvas.Application.Listings;
using FusionCanvas.Application.Stores.Printify;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Stores.Printify;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace FusionCanvas.ApiVerification.Printify;

internal sealed class PrintifyApiVerifier(VerificationConfiguration configuration, PrintifyApiVerifierRuntime? runtime = null)
{
    private readonly PrintifyApiVerifierRuntime _runtime = runtime ?? PrintifyApiVerifierRuntime.Production;

    public async Task<int> RecoverAsync(string? sourceRunId, CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var runId = Guid.NewGuid().ToString("N");
        Console.WriteLine($"Session run ID: {runId}");
        var operations = new List<VerificationOperationResult>();
        var findings = new List<VerificationFinding>();
        var cleanup = new List<string>();
        var scenario = "printify-recovery";
        var status = "blocked";
        if (!Guid.TryParseExact(sourceRunId, "N", out _) || string.IsNullOrWhiteSpace(configuration.PrintifyApiKey) || configuration.PrintifyDevTestShopId is not { } shopId)
        {
            operations.Add(new(scenario, "blocked", "A valid --run-id and configured DevTest credential/shop ID are required."));
            await WriteReportAsync(runId, scenario, startedAt, status, operations, findings, cleanup, cancellationToken).ConfigureAwait(false);
            return 3;
        }

        var journalPath = JournalPath(sourceRunId!);
        PrintifyResourceJournal? journal = null;
        if (File.Exists(journalPath))
        {
            await using var journalStream = File.OpenRead(journalPath);
            journal = await System.Text.Json.JsonSerializer.DeserializeAsync<PrintifyResourceJournal>(journalStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        var sourcePath = Directory.Exists(configuration.DataDirectory)
            ? Directory.EnumerateFiles(configuration.DataDirectory, $"*-Printify-*-{sourceRunId}.json", SearchOption.TopDirectoryOnly).SingleOrDefault()
            : null;
        if (sourcePath is null && journal is null)
        {
            operations.Add(new(scenario, "blocked", "The requested source session report was not found."));
            await WriteReportAsync(runId, scenario, startedAt, status, operations, findings, cleanup, cancellationToken).ConfigureAwait(false);
            return 3;
        }

        VerificationSessionReport? source = null;
        if (sourcePath is not null)
        {
            await using var reportStream = File.OpenRead(sourcePath);
            source = await System.Text.Json.JsonSerializer.DeserializeAsync<VerificationSessionReport>(reportStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        var recoveryTarget = PrintifyRecoveryEligibility.Resolve(sourceRunId!, shopId, journal, source);
        var productId = recoveryTarget?.ProductId;
        if (recoveryTarget is null || recoveryTarget.AlreadyDeleted)
        {
            operations.Add(new(scenario, "blocked", "The source report does not identify a pending session-owned product eligible for recovery."));
            await WriteReportAsync(runId, scenario, startedAt, status, operations, findings, cleanup, cancellationToken).ConfigureAwait(false);
            return 3;
        }

        try
        {
            using var credentialClient = _runtime.CredentialClient();
            var credentials = await new PrintifyCredentialVerifier(credentialClient).VerifyAsync(configuration.PrintifyApiKey!, cancellationToken).ConfigureAwait(false);
            var selectedShop = credentials.Shops?.SingleOrDefault(shop => shop.Id == shopId);
            if (credentials.Kind != PrintifyConfigurationKind.Verified || selectedShop is null
                || !selectedShop.Title.Equals(configuration.PrintifyDevTestShopName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The configured shop did not match the verified FusionCanvas DevTest identity; recovery is blocked.");

            if (!await ConfirmAsync($"Delete only session-owned Printify product {productId} from {configuration.PrintifyDevTestShopName} (shop {shopId}).", "RECOVER PRINTIFY PRODUCT", cancellationToken).ConfigureAwait(false))
            {
                operations.Add(new("printify-product-delete", "cancelled", "Recovery was declined.", ResourceId: productId));
                status = "cancelled";
            }
            else
            {
                using var listingHttp = _runtime.ListingClient();
                var storeId = Guid.NewGuid();
                var listing = new PrintifyListingClient(storeId, listingHttp, _ => Task.FromResult<string?>(configuration.PrintifyApiKey));
                var ready = await listing.CheckAsync(new(storeId, shopId.ToString(System.Globalization.CultureInfo.InvariantCulture), false), cancellationToken).ConfigureAwait(false);
                if (!ready.IsReady) throw new InvalidOperationException("The Printify connection is not ready for recovery.");
                var existing = await listing.GetAsync(shopId.ToString(System.Globalization.CultureInfo.InvariantCulture), productId!, cancellationToken).ConfigureAwait(false);
                if (existing?.PublicationState == ListingPublicationState.Published)
                    throw new InvalidOperationException("The session-owned product is still published. Unpublish it through the normal workflow before retrying recovery deletion.");
                if (existing is not null)
                {
                    var deleted = await listing.DeleteAsync(shopId.ToString(System.Globalization.CultureInfo.InvariantCulture), productId!, cancellationToken).ConfigureAwait(false);
                    if (!deleted.IsDefinitive) throw new InvalidOperationException(deleted.ErrorMessage ?? "The delete result is uncertain.");
                    if (await listing.GetAsync(shopId.ToString(System.Globalization.CultureInfo.InvariantCulture), productId!, cancellationToken).ConfigureAwait(false) is not null)
                        throw new InvalidOperationException("Printify still returns the product after recovery deletion.");
                }
                operations.Add(new("printify-product-delete", "passed", $"Deleted and verified absence of session-owned product {productId}.", ResourceId: productId));
                await SaveJournalAsync(sourceRunId!, shopId, "product-delete", productId, "deleted", cancellationToken).ConfigureAwait(false);
                status = "passed";
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var diagnostic = SafeDiagnostic.Redact(exception.Message);
            operations.Add(new("printify-product-delete", "failed", Diagnostic: diagnostic, ResourceId: productId));
            findings.Add(new(Guid.NewGuid().ToString("N"), "printify-recovery", "open", "recovery-delete", "Delete only the session-owned product and verify its absence.", diagnostic, "src/FusionCanvas.Integration/Stores/Printify/PrintifyListingClient.cs", DateTimeOffset.UtcNow));
            status = "failed";
        }

        await WriteReportAsync(runId, scenario, startedAt, status, operations, findings, cleanup, cancellationToken).ConfigureAwait(false);
        return status == "passed" ? 0 : 1;
    }

    public async Task<int> RunAsync(string scenario, CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var runId = Guid.NewGuid().ToString("N");
        Console.WriteLine($"Session run ID: {runId}");
        var operations = new List<VerificationOperationResult>();
        var findings = new List<VerificationFinding>();
        var cleanup = new List<string>();
        var status = "passed";
        if (!ApiVerificationScenarios.Printify.Contains(scenario))
        {
            operations.Add(new(scenario, "blocked", "Unsupported Printify scenario; no provider request was sent."));
            await WriteReportAsync(runId, scenario, startedAt, "blocked", operations, findings, cleanup, cancellationToken).ConfigureAwait(false);
            return 2;
        }
        var plannedOperations = ApiOperationInventory.Operations
            .Where(operation => operation.Provider == "Printify"
                && (operation.Scenario.Equals(scenario, StringComparison.OrdinalIgnoreCase)
                    || operation.AdditionalScenarios?.Contains(scenario, StringComparer.OrdinalIgnoreCase) == true))
            .ToArray();
        if (plannedOperations.Length == 0 || plannedOperations.Any(operation => operation.CostClassification is ApiOperationCostClass.Unknown or ApiOperationCostClass.BillableCapped))
        {
            operations.Add(new(scenario, "blocked", "The selected operation has an unknown or disallowed cost classification; no provider request was sent."));
            await WriteReportAsync(runId, scenario, startedAt, "blocked", operations, findings, cleanup, cancellationToken).ConfigureAwait(false);
            return 3;
        }
        if (string.IsNullOrWhiteSpace(configuration.PrintifyApiKey) || configuration.PrintifyDevTestShopId is not { } shopId)
        {
            Console.Error.WriteLine("Set FUSIONCANVAS_PRINTIFY_DEVTEST_API_KEY and FUSIONCANVAS_PRINTIFY_DEVTEST_SHOP_ID. No request was sent.");
            operations.Add(new(scenario, "blocked", "Dedicated Printify DevTest credential or shop ID is not configured."));
            await WriteReportAsync(runId, scenario, startedAt, "blocked", operations, findings, cleanup, cancellationToken).ConfigureAwait(false);
            return 3;
        }

        var runPlan = BuildRunPlan(scenario, runId, shopId, plannedOperations);
        Console.WriteLine(runPlan);
        if (!await ConfirmAsync(
                runPlan + "\nThe next network step verifies the configured shop identity. No writes occur until their separate confirmations.",
                $"RUN PRINTIFY {scenario.ToUpperInvariant()}", cancellationToken).ConfigureAwait(false))
        {
            operations.Add(new("session-confirmation", "cancelled", "No provider request was sent."));
            await WriteReportAsync(runId, scenario, startedAt, "cancelled", operations, findings, cleanup, cancellationToken).ConfigureAwait(false);
            return 2;
        }

        try
        {
            using var credentialClient = _runtime.CredentialClient();
            var verifier = new PrintifyCredentialVerifier(credentialClient);
            var credentials = await verifier.VerifyAsync(configuration.PrintifyApiKey!, cancellationToken).ConfigureAwait(false);
            var selectedShop = credentials.Shops?.SingleOrDefault(shop => shop.Id == shopId);
            if (credentials.Kind != PrintifyConfigurationKind.Verified || selectedShop is null
                || !selectedShop.Title.Equals(configuration.PrintifyDevTestShopName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The configured Printify shop ID/name did not match a shop returned by the dedicated API key. Writes are blocked.");
            }
            operations.Add(new("printify-shops", "passed", $"Verified configured DevTest shop `{selectedShop.Title}` (ID {shopId})."));

            using var catalogHttp = _runtime.CatalogClient();
            var catalog = new PrintifyCatalogClient(catalogHttp);
            if (scenario is "printify-smoke" or "printify-catalog" or "printify-product-lifecycle")
            {
                var blueprints = await catalog.LoadBlueprintsAsync(configuration.PrintifyApiKey!, cancellationToken).ConfigureAwait(false);
                EnsureSucceeded(blueprints, "Printify blueprint catalog");
                operations.Add(new("printify-blueprints", "passed", $"Loaded {blueprints.Blueprints!.Count} blueprints."));
                var selected = await catalog.LoadSelectedAsync(configuration.PrintifyApiKey!, [blueprints.Blueprints[0].Id], cancellationToken).ConfigureAwait(false);
                EnsureSucceeded(selected, "Printify providers and variants");
                var blueprint = selected.SelectedCatalog!.Single();
                operations.Add(new("printify-blueprint", "passed", $"Loaded blueprint {blueprint.Summary.Id} details."));
                operations.Add(new("printify-providers", "passed", $"Loaded {blueprint.Providers.Count} print providers."));
                operations.Add(new("printify-variants", "passed", $"Loaded {blueprint.Providers.Sum(provider => provider.Variants.Count)} variants."));
            }

            using var productApi = _runtime.ImportApiClient();
            using var artworkApi = _runtime.ImportImageClient();
            var importer = new PrintifyListingImportClient(productApi, artworkApi);
            if (scenario is "printify-smoke" or "printify-shop-products" or "printify-product-detail" or "printify-artwork-import")
            {
                var catalogProducts = await catalog.LoadShopProductsAsync(configuration.PrintifyApiKey!, shopId, cancellationToken).ConfigureAwait(false);
                if (catalogProducts.Kind is not (PrintifyCatalogResultKind.Succeeded or PrintifyCatalogResultKind.Empty))
                    EnsureSucceeded(catalogProducts, "Printify catalog shop products");
                operations.Add(new("printify-shop-products-catalog", catalogProducts.Kind == PrintifyCatalogResultKind.Succeeded ? "passed" : "empty",
                    $"Catalog path loaded {catalogProducts.Products?.Count ?? 0} product summaries."));

                var products = await importer.GetShopProductsAsync(configuration.PrintifyApiKey!, shopId, cancellationToken).ConfigureAwait(false);
                operations.Add(new("printify-shop-products-import", "passed", $"Import path loaded {products.Count} product summaries across all pages."));
                if (products.Count == 0)
                {
                    operations.Add(new("printify-selected-products", "not-run", "The DevTest shop has no products to select."));
                    operations.Add(new("printify-product-detail", "not-run", "The DevTest shop has no products to inspect."));
                    operations.Add(new("printify-artwork-download", "not-run", "A product with artwork is required."));
                }
                else
                {
                    var selectedCatalogProducts = await catalog.LoadSelectedProductsAsync(configuration.PrintifyApiKey!, shopId, [products[0].ProductId], cancellationToken).ConfigureAwait(false);
                    EnsureSucceeded(selectedCatalogProducts, "Printify selected product catalog");
                    operations.Add(new("printify-selected-products", "passed", $"Loaded selected product {products[0].ProductId} with catalog variant details."));
                    var detail = await importer.GetProductAsync(configuration.PrintifyApiKey!, shopId, products[0].ProductId, cancellationToken).ConfigureAwait(false);
                    if (detail is null) throw new InvalidOperationException("The selected product disappeared before detail retrieval.");
                    operations.Add(new("printify-product-detail", "passed", $"Loaded product `{detail.ProductId}` with {detail.Variants.Count} variants and {detail.PrintAreas.Sum(area => area.Images.Count)} artwork layer(s)."));
                    var artwork = detail.PrintAreas.SelectMany(area => area.Images).FirstOrDefault();
                    if (artwork is null && scenario == "printify-artwork-import")
                    {
                        operations.Add(new("printify-artwork-download", "not-run", "The selected product has no artwork URL."));
                    }
                    else if (artwork is not null && scenario == "printify-artwork-import")
                    {
                        var download = await importer.DownloadArtworkAsync(artwork.SourceUrl, cancellationToken).ConfigureAwait(false);
                        operations.Add(new("printify-artwork-download", "passed", $"Downloaded {download.Content.Length} bytes ({download.MediaType})."));
                    }
                }
            }

            if (scenario == "printify-product-lifecycle")
            {
                using var listingHttp = _runtime.ListingClient();
                var storeId = Guid.NewGuid();
                var listing = new PrintifyListingClient(storeId, listingHttp, _ => Task.FromResult<string?>(configuration.PrintifyApiKey), new InMemoryFileReader(TestPng));
                await RunWriteLifecycleAsync(listing, catalog, storeId, shopId, runId, operations, findings, cleanup, cancellationToken).ConfigureAwait(false);
            }
            else if (!ApiVerificationScenarios.Printify.Contains(scenario))
            {
                throw new InvalidOperationException($"Unsupported Printify scenario `{SafeDiagnostic.Redact(scenario)}`.");
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
            findings.Add(new(Guid.NewGuid().ToString("N"), scenario, "open", "request-or-response", "The production Printify operation completes with a documented contract result.", diagnostic, "src/FusionCanvas.Integration/Stores/Printify", DateTimeOffset.UtcNow));
            Console.Error.WriteLine(diagnostic);
        }

        if (operations.Any(operation => operation.Status is "failed" or "blocked"))
            status = operations.Any(operation => operation.Status == "failed") ? "failed" : "blocked";
        await WriteReportAsync(runId, scenario, startedAt, status, operations, findings, cleanup, cancellationToken).ConfigureAwait(false);
        return status == "passed" ? 0 : 1;
    }

    private async Task RunWriteLifecycleAsync(
        PrintifyListingClient listing,
        PrintifyCatalogClient catalog,
        Guid storeId,
        int shopId,
        string runId,
        ICollection<VerificationOperationResult> operations,
        ICollection<VerificationFinding> findings,
        ICollection<string> cleanup,
        CancellationToken cancellationToken)
    {
        var ready = await listing.CheckAsync(new(storeId, shopId.ToString(System.Globalization.CultureInfo.InvariantCulture), RequirePublication: true), cancellationToken).ConfigureAwait(false);
        // This connection check is shop-bound by client; it is also verified against the configured key above.
        if (!ready.IsReady)
            throw new InvalidOperationException("The scoped Printify listing connection is not ready.");

        var blueprintResult = await catalog.LoadBlueprintsAsync(configuration.PrintifyApiKey!, cancellationToken).ConfigureAwait(false);
        EnsureSucceeded(blueprintResult, "Printify blueprints for product creation");
        var details = await catalog.LoadSelectedAsync(configuration.PrintifyApiKey!, [blueprintResult.Blueprints![0].Id], cancellationToken).ConfigureAwait(false);
        EnsureSucceeded(details, "Printify provider variants for product creation");
        var blueprint = details.SelectedCatalog!.Single();
        var provider = blueprint.Providers.FirstOrDefault(value => value.Variants.Any(variant => variant.IsEnabled && variant.IsAvailable && variant.Placeholders.Count > 0))
            ?? throw new InvalidOperationException("No catalog provider has an enabled, available variant with an artwork placeholder; product writes are blocked.");
        var variant = provider.Variants.First(value => value.IsEnabled && value.IsAvailable && value.Placeholders.Count > 0);
        var placeholder = variant.Placeholders[0];
        Console.WriteLine("Write plan: upload one generated PNG; create an unpublished product; read it back; update its title; optionally publish and unpublish; then delete and verify absence.");
        Console.WriteLine($"Target: {configuration.PrintifyDevTestShopName} (shop {shopId}), blueprint {blueprint.Summary.Id}, provider {provider.Id}, variant {variant.Id} ({variant.Title}), print position {placeholder.Position}.");
        var assetId = Guid.NewGuid();
        var fingerprint = Convert.ToHexString(SHA256.HashData(TestPng));
        var uploadRequest = new ListingImageUpload(assetId, "api-verification.png", fingerprint);
        if (!await ConfirmAsync($"Upload one generated {TestPng.Length}-byte PNG to configured DevTest shop context.", "UPLOAD PRINTIFY IMAGE", cancellationToken).ConfigureAwait(false))
        {
            operations.Add(new("printify-image-upload", "cancelled", "Write declined."));
            return;
        }
        var uploaded = await listing.UploadAsync(uploadRequest, cancellationToken).ConfigureAwait(false);
        operations.Add(new("printify-image-upload", "passed", "Printify returned an uploaded image identity."));

        var sourceVariantId = Guid.NewGuid();
        var price = new ListingPrice(sourceVariantId, 0m, 10m);
        var projection = new ListingProductProjection(Guid.NewGuid(), storeId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            $"FusionCanvas API verification {DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}", "Temporary FusionCanvas API verification product.", null, null,
            new(ListingPricingPolicy.FixedRetailPrice, 10m),
            [new(sourceVariantId, variant.Title, null, [], 0m, price) { ExternalVariantId = variant.Id }],
            [new(Guid.NewGuid(), assetId, "api-verification.png", new(Math.Max(placeholder.Width, 1), Math.Max(placeholder.Height, 1)), new(Math.Max(placeholder.Width, 1), Math.Max(placeholder.Height, 1)), new(0.5, 0.5, 0.5, 0)) { Position = placeholder.Position, DecorationMethod = placeholder.DecorationMethod }])
        { ExternalBlueprintId = blueprint.Summary.Id, ExternalProviderId = provider.Id };
        var now = DateTimeOffset.UtcNow;
        var applicationMapping = new ExternalListingMapping(storeId, projection.ItemId, "printify",
            shopId.ToString(System.Globalization.CultureInfo.InvariantCulture), null, null, null,
            ExternalListingSyncState.Draft, ExternalListingPublicationState.NotApplicable, ExternalListingOperationState.None,
            null, null, null, now, now,
            System.Text.Json.JsonSerializer.Serialize(new Dictionary<Guid, ListingImageReference>
            {
                [assetId] = new(uploaded.ReferenceId, $"{assetId:N}:{projection.Artwork[0].Artwork.Width}x{projection.Artwork[0].Artwork.Height}:{projection.Artwork[0].AssetPath}")
            }));
        var repository = new VerificationWorkspaceRepository(WorkspaceSnapshot.Empty with
        {
            ExternalListingMappings = [applicationMapping]
        });
        var lifecycle = new ListingLifecycleService(repository, listing, listing, listing, images: listing);

        var shop = shopId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!await ConfirmAsync("Create one unpublished, zero-variant-cost test product using the selected catalog blueprint.", "CREATE PRINTIFY PRODUCT", cancellationToken).ConfigureAwait(false))
        {
            operations.Add(new("printify-product-create", "cancelled", "Write declined."));
            return;
        }
        await SaveJournalAsync(runId, shopId, "product-create", null, "creating", cancellationToken).ConfigureAwait(false);
        var created = await lifecycle.CreateOrUpdateAsync(new(storeId, shop, RequirePublication: true), projection, cancellationToken).ConfigureAwait(false);
        var productId = created.Mapping?.Identity?.ProductId;
        if (created.Kind != ListingLifecycleResultKind.Succeeded || string.IsNullOrWhiteSpace(productId))
        {
            if (!string.IsNullOrWhiteSpace(productId))
                cleanup.Add($"Create result is uncertain for product {productId}; verify it in DevTest before any retry.");
            var uncertain = created.Kind is ListingLifecycleResultKind.Uncertain or ListingLifecycleResultKind.Unavailable;
            operations.Add(new("printify-product-create", uncertain ? "uncertain" : "failed", created.Message ?? "Printify did not return a definitive product identity.", ResourceId: productId));
            await SaveJournalAsync(runId, shopId, "product-create", productId, string.IsNullOrWhiteSpace(productId) ? "uncertain" : "cleanup-pending", cancellationToken).ConfigureAwait(false);
            findings.Add(new(Guid.NewGuid().ToString("N"), "printify-product-create", "open", uncertain ? "write-result-uncertain" : "application-lifecycle-rejected", "The application lifecycle completes with a definitive product identity.", SafeDiagnostic.Redact(created.Message ?? "No product identity was returned."), "src/FusionCanvas.Application/Listings/ListingLifecycleService.cs", DateTimeOffset.UtcNow));
            if (string.IsNullOrWhiteSpace(productId)) cleanup.Add("Create result uncertain with no returned product ID; inspect DevTest shop manually before retrying.");
            return;
        }
        cleanup.Add($"Created product {productId}; cleanup pending until delete is confirmed.");
        operations.Add(new("printify-product-create", "ownership-journal-pending", $"Product {productId} was returned; ownership record is being saved.", ResourceId: productId));
        await SaveJournalAsync(runId, shopId, "product-create", productId, "owned", cancellationToken).ConfigureAwait(false);
        operations.Remove(operations.Last());
        operations.Add(new("printify-product-create", "passed", $"Created product {productId}.", ResourceId: productId));

        var readback = await listing.GetAsync(shop, productId, cancellationToken).ConfigureAwait(false);
        if (readback is null || !string.Equals(readback.ProductId, productId, StringComparison.Ordinal))
            throw new InvalidOperationException("Created product read-back did not return the created product identity.");
        operations.Add(new("printify-product-detail", "passed", "Read back the created product identity."));

        var updatedProjection = projection with { Title = projection.Title + " updated" };
        if (await ConfirmAsync($"Update session-owned product {productId} title.", "UPDATE PRINTIFY PRODUCT", cancellationToken).ConfigureAwait(false))
        {
            await SaveJournalAsync(runId, shopId, "product-update", productId, "owned", cancellationToken).ConfigureAwait(false);
            var updated = await lifecycle.CreateOrUpdateAsync(new(storeId, shop, RequirePublication: true), updatedProjection, cancellationToken).ConfigureAwait(false);
            if (updated.Kind != ListingLifecycleResultKind.Succeeded) throw new InvalidOperationException(updated.Message ?? "The application listing update did not complete definitively.");
            readback = await listing.GetAsync(shop, productId, cancellationToken).ConfigureAwait(false);
            if (readback?.Snapshot.Fields.GetValueOrDefault("title") == updatedProjection.Title)
                operations.Add(new("printify-product-update", "passed", "Updated and read-back verified the session-owned product title.", ResourceId: productId));
            else
            {
                operations.Add(new("printify-product-update", "failed", "Update response completed but title read-back did not match.", ResourceId: productId));
                findings.Add(new(Guid.NewGuid().ToString("N"), "printify-product-update", "open", "read-back-mismatch", updatedProjection.Title, readback?.Snapshot.Fields.GetValueOrDefault("title") ?? "missing title", "src/FusionCanvas.Integration/Stores/Printify/PrintifyListingClient.cs", DateTimeOffset.UtcNow));
            }
        }
        else operations.Add(new("printify-product-update", "cancelled", "Write declined.", ResourceId: productId));

        if (await ConfirmAsync($"Publish session-owned product {productId}. This may expose a sellable listing on connected channels; a resulting external order can incur production/shipping charges. Check the DevTest store and all connected channels before proceeding. This runner will not create orders.", "PUBLISH PRINTIFY PRODUCT AFTER CHANNEL CHECK", cancellationToken).ConfigureAwait(false))
        {
            await SaveJournalAsync(runId, shopId, "product-publish", productId, "owned", cancellationToken).ConfigureAwait(false);
            var published = await lifecycle.PublishAsync(new(storeId, shop, RequirePublication: true), projection.ItemId, cancellationToken).ConfigureAwait(false);
            if (published.Kind == ListingLifecycleResultKind.Succeeded)
                operations.Add(new("printify-product-publish", "passed", "Published and read-back verified the session-owned product.", ResourceId: productId));
            else
            {
                var actual = SafeDiagnostic.Redact(published.Message ?? "The application lifecycle did not verify the published state.");
                operations.Add(new("printify-product-publish", "failed", actual, ResourceId: productId));
                findings.Add(new(Guid.NewGuid().ToString("N"), "printify-product-publish", "open", "publication-state-mismatch", "Published", actual, "src/FusionCanvas.Application/Listings/ListingLifecycleService.cs", DateTimeOffset.UtcNow));
            }
            if (await ConfirmAsync($"Unpublish session-owned product {productId}.", "UNPUBLISH PRINTIFY PRODUCT", cancellationToken).ConfigureAwait(false))
            {
                await SaveJournalAsync(runId, shopId, "product-unpublish", productId, "owned", cancellationToken).ConfigureAwait(false);
                var unpublished = await lifecycle.UnpublishAsync(new(storeId, shop, RequirePublication: true), projection.ItemId, cancellationToken).ConfigureAwait(false);
                if (unpublished.Kind == ListingLifecycleResultKind.Succeeded)
                    operations.Add(new("printify-product-unpublish", "passed", "Unpublished and read-back verified the session-owned product.", ResourceId: productId));
                else
                {
                    var actual = SafeDiagnostic.Redact(unpublished.Message ?? "The application lifecycle did not verify the unpublished state.");
                    operations.Add(new("printify-product-unpublish", "failed", actual, ResourceId: productId));
                    findings.Add(new(Guid.NewGuid().ToString("N"), "printify-product-unpublish", "open", "publication-state-mismatch", "Unpublished", actual, "src/FusionCanvas.Application/Listings/ListingLifecycleService.cs", DateTimeOffset.UtcNow));
                }
            }
            else operations.Add(new("printify-product-unpublish", "cancelled", "Write declined.", ResourceId: productId));
        }
        else
        {
            operations.Add(new("printify-product-publish", "cancelled", "Write declined; unpublish was skipped." , ResourceId: productId));
            operations.Add(new("printify-product-unpublish", "not-run", "Publishing was declined." , ResourceId: productId));
        }

        if (await ConfirmAsync($"Delete session-owned product {productId}; this permanently removes the test product.", "DELETE PRINTIFY PRODUCT", cancellationToken).ConfigureAwait(false))
        {
            await SaveJournalAsync(runId, shopId, "product-delete", productId, "delete-uncertain", cancellationToken).ConfigureAwait(false);
            var deleted = await lifecycle.DeleteRemoteAsync(new(storeId, shop, RequirePublication: true), projection.ItemId, cancellationToken).ConfigureAwait(false);
            if (deleted.Kind != ListingLifecycleResultKind.Succeeded) throw new InvalidOperationException(deleted.Message ?? "The application listing deletion did not complete definitively.");
            var verifyDeleted = await listing.GetAsync(shop, productId, cancellationToken).ConfigureAwait(false);
            if (verifyDeleted is not null) throw new InvalidOperationException("Printify still returns the deleted session product.");
            operations.Add(new("printify-product-delete", "passed", "Deleted and verified absence of the session-owned product.", ResourceId: productId));
            cleanup.Clear();
            await SaveJournalAsync(runId, shopId, "product-delete", productId, "deleted", cancellationToken).ConfigureAwait(false);
        }
        else
        {
            operations.Add(new("printify-product-delete", "cleanup-pending", "Delete declined; the report retains the session-owned product ID.", ResourceId: productId));
            await SaveJournalAsync(runId, shopId, "product-delete", productId, "cleanup-pending", cancellationToken).ConfigureAwait(false);
        }

    }

    private async Task SaveJournalAsync(string runId, int shopId, string operation, string? productId, string status, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(configuration.DataDirectory);
        var path = JournalPath(runId);
        var journal = new PrintifyResourceJournal(1, runId, shopId, VerificationConfiguration.RequiredPrintifyDevTestShopName,
            operation, productId, status, DateTimeOffset.UtcNow);
        await AtomicTextFile.WriteAsync(path, System.Text.Json.JsonSerializer.Serialize(journal), cancellationToken).ConfigureAwait(false);
    }

    private Task<bool> ConfirmAsync(string instruction, string phrase, CancellationToken cancellationToken) =>
        _runtime.Confirm(instruction, phrase, cancellationToken);

    private string BuildRunPlan(string scenario, string runId, int shopId, IReadOnlyList<ApiOperationDefinition> operations)
    {
        var lines = new List<string>
        {
            $"Printify preflight — scenario: {scenario}",
            $"Configured target: {configuration.PrintifyDevTestShopName} (shop ID {shopId}); the first request must verify this exact shop against the dedicated key.",
            "Planned provider operations:"
        };
        lines.AddRange(operations.Select(operation => $"- {operation.Id}: {operation.Method} {operation.Path} [{operation.Effect}; {operation.CostClassification}]"));
        lines.Add("Possible writes:");
        var writes = operations.Where(operation => operation.Effect is "write" or "external-visibility").ToArray();
        lines.AddRange(writes.Length == 0
            ? ["- None."]
            : writes.Select(operation => $"- {operation.Id}: {operation.Method} {operation.Path} [{operation.CostClassification}]; requires its own typed confirmation."));
        lines.Add("Recovery:");
        lines.Add(scenario == "printify-product-lifecycle"
            ? $"- If product deletion is declined or uncertain, inspect the DevTest shop and run `recover --provider printify --run-id {runId}` for this session-owned resource."
            : "- No session-created product is expected; no resource recovery action is planned.");
        lines.Add("Excluded: orders and chargeable operations.");
        return string.Join(Environment.NewLine, lines);
    }

    private string JournalPath(string runId) => Path.Combine(configuration.DataDirectory, $"printify-resource-{runId}.json");

    private async Task WriteReportAsync(string runId, string scenario, DateTimeOffset startedAt, string status,
        IReadOnlyList<VerificationOperationResult> operations, IReadOnlyList<VerificationFinding> findings,
        IReadOnlyList<string> cleanup, CancellationToken cancellationToken)
    {
        var report = new VerificationSessionReport(1, runId, "Printify", scenario, startedAt, DateTimeOffset.UtcNow,
            status, operations, findings, null, null, cleanup);
        await new VerificationReportWriter(configuration.DataDirectory).WriteAsync(report, cancellationToken).ConfigureAwait(false);
    }

    private static void EnsureSucceeded(PrintifyCatalogResult result, string operation)
    {
        if (!result.Succeeded) throw new InvalidOperationException($"{operation} failed: {SafeDiagnostic.Redact(result.Message)}");
    }

    private static readonly byte[] TestPng = CreateTestPng();

    private static byte[] CreateTestPng()
    {
        using var image = new Image<Rgba32>(1024, 1024, new Rgba32(35, 104, 218));
        using var buffer = new MemoryStream();
        image.SaveAsPng(buffer);
        return buffer.ToArray();
    }

    private sealed class InMemoryFileReader(byte[] content) : IWorkspaceFileReader
    {
        public bool Exists(string workspaceRelativePath) => workspaceRelativePath == "api-verification.png";
        public Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Exists(workspaceRelativePath)) throw new FileNotFoundException("Verification fixture not found.");
            return Task.FromResult<Stream>(new MemoryStream(content, writable: false));
        }
    }

    private sealed class VerificationWorkspaceRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = snapshot;

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_snapshot);
        }

        public Task SaveAsync(WorkspaceSnapshot next, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _snapshot = next;
            return Task.CompletedTask;
        }
    }
}
