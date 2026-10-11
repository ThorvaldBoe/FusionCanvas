using FusionCanvas.Application.Stores;
using FusionCanvas.Application.Stores.Printify;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.Application.Tests.Stores;

public sealed class PrintifyListingImportServiceTests
{
    [Fact]
    public async Task Duplicate_check_searches_other_niches_and_import_preserves_catalog_and_design_state()
    {
        var fixture = new Fixture();
        var otherNiche = new Niche(Guid.NewGuid(), fixture.Store.Id, "Other niche", null, false, fixture.Now, fixture.Now, "{}");
        var existing = new Item(Guid.NewGuid(), fixture.Store.Id, otherNiche.Id, null, "Moonlit Foz", "A quiet fox beneath the moon", ItemStatus.Draft, WorkflowStage.Design, false, fixture.Now, fixture.Now, "{}");
        fixture.Repository.Snapshot = fixture.Repository.Snapshot with { Niches = [fixture.Niche, otherNiche], Items = [existing] };

        var matches = await fixture.Service.CheckDuplicatesAsync(fixture.Scope, ["visible-product"], TestContext.Current.CancellationToken);
        Assert.Equal(existing.Id, Assert.Single(matches["visible-product"]).ItemId);
        Assert.True(matches["visible-product"][0].TitleSimilarity >= 0.90);
        Assert.Contains("Other niche", matches["visible-product"][0].Location);
        Assert.True(matches["visible-product"][0].CanConnect);

        var outcome = await fixture.Service.ImportAsync(fixture.Scope, fixture.Niche.Id, [new("visible-product", existing.Id)], TestContext.Current.CancellationToken);

        Assert.True(Assert.Single(outcome).Succeeded);
        Assert.Equal(existing, fixture.Repository.Snapshot.Items.Single());
        Assert.Empty(fixture.Repository.Snapshot.Blueprints);
        Assert.Empty(fixture.Repository.Snapshot.DesignSlotAssignments);
        Assert.Single(fixture.Repository.Snapshot.Assets);
        Assert.Single(fixture.Repository.Snapshot.ExternalListingMappings);
        Assert.DoesNotContain("signature=", fixture.Repository.Snapshot.ExternalListingMappings[0].IntegrationValuesJson);
    }

    [Fact]
    public async Task Preview_hides_linked_products_by_default_and_no_selection_duplicate_check_is_rejected()
    {
        var fixture = new Fixture();
        var item = new Item(Guid.NewGuid(), fixture.Store.Id, fixture.Niche.Id, null, "Moonlit Fox", "A product description", ItemStatus.Draft, WorkflowStage.Listing, false, fixture.Now, fixture.Now, "{}");
        var mapping = new ExternalListingMapping(fixture.Store.Id, item.Id, "printify", "42", "visible-product", null, null,
            ExternalListingSyncState.Synchronized, ExternalListingPublicationState.Published, ExternalListingOperationState.Succeeded, null, null, fixture.Now, fixture.Now, fixture.Now);
        fixture.Repository.Snapshot = fixture.Repository.Snapshot with { Items = [item], ExternalListingMappings = [mapping] };

        var preview = await fixture.Service.LoadPreviewAsync(fixture.Scope, fixture.Niche.Id, TestContext.Current.CancellationToken);

        Assert.True(preview.Single(value => value.Product.ProductId == "visible-product").IsLinked);
        Assert.False(preview.Single(value => value.Product.ProductId == "hidden-product").IsLinked);
        var match = Assert.Single(await fixture.Service.CheckDuplicatesAsync(fixture.Scope, ["visible-product"], TestContext.Current.CancellationToken));
        Assert.False(Assert.Single(match.Value).CanConnect);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CheckDuplicatesAsync(fixture.Scope, [], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Missing_credential_stops_preview_before_printify_request()
    {
        var fixture = new Fixture();
        var service = new PrintifyListingImportService(new StoresStub(fixture.Store), new CredentialStub { StatusKind = PrintifyConfigurationKind.Missing }, fixture.Client, fixture.Repository, fixture.Files);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoadPreviewAsync(fixture.Scope, fixture.Niche.Id, TestContext.Current.CancellationToken));

        Assert.Equal(0, fixture.Client.ShopProductCalls);
        Assert.Empty(fixture.Repository.Snapshot.Items);
    }

    [Fact]
    public async Task Duplicate_check_matches_description_and_marks_an_already_linked_item_non_linkable()
    {
        var fixture = new Fixture();
        var existing = new Item(Guid.NewGuid(), fixture.Store.Id, fixture.Niche.Id, null, "Different local title", "A product description", ItemStatus.Draft, WorkflowStage.Design, false, fixture.Now, fixture.Now, "{}");
        var mapping = new ExternalListingMapping(fixture.Store.Id, existing.Id, "printify", "42", "another-product", null, null,
            ExternalListingSyncState.Synchronized, ExternalListingPublicationState.Published, ExternalListingOperationState.Succeeded, null, null, fixture.Now, fixture.Now, fixture.Now);
        fixture.Repository.Snapshot = fixture.Repository.Snapshot with { Items = [existing], ExternalListingMappings = [mapping] };

        var matches = await fixture.Service.CheckDuplicatesAsync(fixture.Scope, ["visible-product"], TestContext.Current.CancellationToken);

        var candidate = Assert.Single(matches["visible-product"]);
        Assert.Equal(existing.Id, candidate.ItemId);
        Assert.True(candidate.DescriptionSimilarity >= 0.90);
        Assert.False(candidate.CanConnect);
    }

    [Fact]
    public async Task Duplicate_check_leaves_unrelated_items_as_new_import_candidates()
    {
        var fixture = new Fixture();
        var unrelated = new Item(Guid.NewGuid(), fixture.Store.Id, fixture.Niche.Id, null, "Completely unrelated", "Different description", ItemStatus.Draft, WorkflowStage.Design, false, fixture.Now, fixture.Now, "{}");
        fixture.Repository.Snapshot = fixture.Repository.Snapshot with { Items = [unrelated] };

        var matches = await fixture.Service.CheckDuplicatesAsync(fixture.Scope, ["visible-product"], TestContext.Current.CancellationToken);

        Assert.Empty(matches["visible-product"]);
    }

    [Fact]
    public async Task New_visible_and_hidden_products_share_a_unique_dated_group_and_retry_is_idempotent()
    {
        var fixture = new Fixture();
        fixture.Repository.Snapshot = fixture.Repository.Snapshot with
        {
            Groups = [new TopicGroup(Guid.NewGuid(), fixture.Store.Id, fixture.Niche.Id, null, $"Printify {fixture.Now.ToLocalTime():yyyy-MM-dd}", null, false, fixture.Now, fixture.Now, "{}")]
        };

        var outcomes = await fixture.Service.ImportAsync(fixture.Scope, fixture.Niche.Id,
            [new("visible-product"), new("hidden-product")], TestContext.Current.CancellationToken);

        Assert.All(outcomes, value => Assert.True(value.Succeeded, value.Message));
        var items = fixture.Repository.Snapshot.Items.OrderBy(value => value.Name).ToArray();
        Assert.Equal(ItemStatus.Draft, items.Single(value => value.Name == "Hidden shirt").Status);
        Assert.Equal(ItemStatus.Published, items.Single(value => value.Name == "Moonlit Fox").Status);
        Assert.All(items, item => Assert.Equal(WorkflowStage.Listing, item.Stage));
        Assert.Single(items.Select(item => item.GroupId).Distinct());
        Assert.Contains("(2)", fixture.Repository.Snapshot.Groups.Single(value => value.Id == items[0].GroupId).Name);
        Assert.Equal(2, fixture.Repository.Snapshot.ExternalListingMappings.Count);

        var retry = await fixture.Service.ImportAsync(fixture.Scope, fixture.Niche.Id, [new("visible-product")], TestContext.Current.CancellationToken);
        Assert.False(Assert.Single(retry).Succeeded);
        Assert.Equal(2, fixture.Repository.Snapshot.Items.Count);
    }

    [Fact]
    public async Task Variant_setup_download_is_idempotent_and_assigns_original_artwork_without_printify_transforms()
    {
        var fixture = new Fixture();
        var item = new Item(Guid.NewGuid(), fixture.Store.Id, fixture.Niche.Id, null, "My local title", "Local description", ItemStatus.Published, WorkflowStage.Listing, false, fixture.Now, fixture.Now, "{}");
        var mapping = new ExternalListingMapping(fixture.Store.Id, item.Id, "printify", "42", "visible-product", null, null,
            ExternalListingSyncState.Synchronized, ExternalListingPublicationState.Published, ExternalListingOperationState.Succeeded, null, null, fixture.Now, fixture.Now, fixture.Now);
        var assetId = Guid.NewGuid();
        var asset = new Asset(assetId, fixture.Store.Id, "Artwork", null, AssetKind.ExportedImage, "assets/artwork.png", null, false, false, fixture.Now, fixture.Now, "{\"imageId\":\"image-visible-product\"}");
        fixture.Repository.Snapshot = fixture.Repository.Snapshot with { Items = [item], ExternalListingMappings = [mapping], Assets = [asset] };
        var catalog = new CatalogStub();
        var service = new PrintifyListingImportService(new StoresStub(fixture.Store), new CredentialStub(), fixture.Client, fixture.Repository, new FileStub(), catalog, () => fixture.Now, Guid.NewGuid);

        var first = await service.DownloadVariantSetupAsync(fixture.Scope, item.Id, TestContext.Current.CancellationToken);
        var second = await service.DownloadVariantSetupAsync(fixture.Scope, item.Id, TestContext.Current.CancellationToken);

        Assert.True(first.Succeeded, first.Message);
        Assert.True(second.Succeeded, second.Message);
        Assert.Single(fixture.Repository.Snapshot.ItemListingConfigurations);
        Assert.Single(fixture.Repository.Snapshot.DesignVariantRows);
        Assert.Equal(["Black", "White"], fixture.Repository.Snapshot.DesignSelectedColors.Select(value => value.ColorValue));
        Assert.Equal(assetId, Assert.Single(fixture.Repository.Snapshot.DesignSlotAssignments).AssetId);
        Assert.Equal(2, fixture.Repository.Snapshot.DesignVariantRowColors.Count);
        Assert.DoesNotContain("scale", fixture.Repository.Snapshot.Assets.Single().MetadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rotation", fixture.Repository.Snapshot.Assets.Single().MetadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("My local title", fixture.Repository.Snapshot.Items.Single().Name);
        Assert.Equal("Local description", fixture.Repository.Snapshot.Items.Single().Description);
        Assert.Equal(2, catalog.Calls);
    }

    [Fact]
    public async Task Persistence_failure_cleans_staged_artwork_and_leaves_no_item_mapping_or_empty_group()
    {
        var fixture = new Fixture();
        fixture.Repository.SaveException = new IOException("synthetic persistence failure");

        var result = await fixture.Service.ImportAsync(fixture.Scope, fixture.Niche.Id, [new("visible-product")], TestContext.Current.CancellationToken);

        Assert.False(Assert.Single(result).Succeeded);
        Assert.Equal(1, fixture.Files.SaveCount);
        Assert.Equal(1, fixture.Files.DeleteCount);
        Assert.Empty(fixture.Repository.Snapshot.Items);
        Assert.Empty(fixture.Repository.Snapshot.Groups);
        Assert.Empty(fixture.Repository.Snapshot.ExternalListingMappings);
        Assert.Empty(fixture.Repository.Snapshot.Assets);
    }

    [Fact]
    public async Task Unsupported_layer_artwork_remains_linked_and_variant_setup_leaves_slot_empty()
    {
        var fixture = new Fixture();
        var imported = await fixture.Service.ImportAsync(fixture.Scope, fixture.Niche.Id, [new("unsupported-product")], TestContext.Current.CancellationToken);
        Assert.True(Assert.Single(imported).Succeeded);
        var item = Assert.Single(fixture.Repository.Snapshot.Items);
        var catalog = new CatalogStub();
        var service = new PrintifyListingImportService(new StoresStub(fixture.Store), new CredentialStub(), fixture.Client, fixture.Repository, new FileStub(), catalog, () => fixture.Now, Guid.NewGuid);

        var result = await service.DownloadVariantSetupAsync(fixture.Scope, item.Id, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Message);
        Assert.True(result.IsPartial);
        Assert.Single(fixture.Repository.Snapshot.Assets);
        Assert.Null(Assert.Single(fixture.Repository.Snapshot.DesignSlotAssignments).AssetId);
    }

    [Fact]
    public async Task Variant_setup_catalog_failure_does_not_write_partial_item_configuration()
    {
        var fixture = new Fixture();
        var item = new Item(Guid.NewGuid(), fixture.Store.Id, fixture.Niche.Id, null, "Local title", "Local description", ItemStatus.Draft, WorkflowStage.Listing, false, fixture.Now, fixture.Now, "{}");
        var mapping = new ExternalListingMapping(fixture.Store.Id, item.Id, "printify", "42", "visible-product", null, null,
            ExternalListingSyncState.Synchronized, ExternalListingPublicationState.Published, ExternalListingOperationState.Succeeded, null, null, fixture.Now, fixture.Now, fixture.Now);
        fixture.Repository.Snapshot = fixture.Repository.Snapshot with { Items = [item], ExternalListingMappings = [mapping] };
        var catalog = new CatalogStub { Result = new(PrintifyCatalogResultKind.NetworkFailure, "temporary catalog failure") };
        var service = new PrintifyListingImportService(new StoresStub(fixture.Store), new CredentialStub(), fixture.Client, fixture.Repository, new FileStub(), catalog, () => fixture.Now, Guid.NewGuid);

        var result = await service.DownloadVariantSetupAsync(fixture.Scope, item.Id, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Empty(fixture.Repository.Snapshot.ItemListingConfigurations);
        Assert.Empty(fixture.Repository.Snapshot.DesignVariantRows);
        Assert.Equal(item, Assert.Single(fixture.Repository.Snapshot.Items));
    }

    [Fact]
    public async Task Variant_setup_catalog_conflict_does_not_overwrite_existing_store_offering()
    {
        var fixture = new Fixture();
        var item = new Item(Guid.NewGuid(), fixture.Store.Id, fixture.Niche.Id, null, "Local title", "Local description", ItemStatus.Draft, WorkflowStage.Listing, false, fixture.Now, fixture.Now, "{}");
        var mapping = new ExternalListingMapping(fixture.Store.Id, item.Id, "printify", "42", "visible-product", null, null,
            ExternalListingSyncState.Synchronized, ExternalListingPublicationState.Published, ExternalListingOperationState.Succeeded, null, null, fixture.Now, fixture.Now, fixture.Now);
        var offeringId = Guid.NewGuid();
        var conflictingOffering = new FusionCanvas.Domain.Catalog.BlueprintOffering(offeringId, Guid.NewGuid(), fixture.Store.Id,
            "Locally edited offering", "Local description", FusionCanvas.Domain.Catalog.BlueprintOfferingKind.FixedPrintProvider,
            null, null, null, "visible-product:9", false, fixture.Now, fixture.Now, "{}");
        fixture.Repository.Snapshot = fixture.Repository.Snapshot with { Items = [item], ExternalListingMappings = [mapping], BlueprintOfferings = [conflictingOffering] };
        var service = new PrintifyListingImportService(new StoresStub(fixture.Store), new CredentialStub(), fixture.Client, fixture.Repository, new FileStub(), new CatalogStub(), () => fixture.Now, Guid.NewGuid);

        var result = await service.DownloadVariantSetupAsync(fixture.Scope, item.Id, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("conflict", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(conflictingOffering, Assert.Single(fixture.Repository.Snapshot.BlueprintOfferings));
        Assert.Empty(fixture.Repository.Snapshot.ItemListingConfigurations);
        Assert.Empty(fixture.Repository.Snapshot.DesignVariantRows);
    }

    [Fact]
    public async Task Product_failure_is_isolated_and_cancellation_before_product_details_makes_no_local_changes()
    {
        var fixture = new Fixture();
        var outcomes = await fixture.Service.ImportAsync(fixture.Scope, fixture.Niche.Id,
            [new("visible-product"), new("missing-product")], TestContext.Current.CancellationToken);

        Assert.True(outcomes[0].Succeeded);
        Assert.False(outcomes[1].Succeeded);
        Assert.Single(fixture.Repository.Snapshot.Items);
        Assert.Single(fixture.Repository.Snapshot.Groups);
        Assert.Single(fixture.Repository.Snapshot.ExternalListingMappings);

        var cancelled = new Fixture();
        using var source = new CancellationTokenSource();
        source.Cancel();
        var cancelledResult = await cancelled.Service.ImportAsync(cancelled.Scope, cancelled.Niche.Id, [new("visible-product")], source.Token);
        Assert.False(Assert.Single(cancelledResult).Succeeded);
        Assert.Empty(cancelled.Repository.Snapshot.Items);
        Assert.Empty(cancelled.Repository.Snapshot.Groups);
        Assert.Equal(0, cancelled.Files.SaveCount);
    }

    [Fact]
    public async Task Cancellation_after_a_success_stops_before_the_next_product_and_keeps_the_success()
    {
        var fixture = new Fixture();
        using var source = new CancellationTokenSource();
        fixture.Repository.OnSave = source.Cancel;

        var outcomes = await fixture.Service.ImportAsync(fixture.Scope, fixture.Niche.Id,
            [new("visible-product"), new("hidden-product")], source.Token);

        Assert.True(outcomes[0].Succeeded);
        Assert.False(outcomes[1].Succeeded);
        Assert.Single(fixture.Repository.Snapshot.Items);
        Assert.Single(fixture.Repository.Snapshot.ExternalListingMappings);
        Assert.Single(fixture.Repository.Snapshot.Groups);
    }

    private sealed class Fixture
    {
        public DateTimeOffset Now { get; } = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
        public StoreSummary Store { get; }
        public Niche Niche { get; }
        public StoreCredentialScope Scope { get; }
        public Repository Repository { get; }
        public FileStub Files { get; } = new();
        public Service Client { get; }
        public PrintifyListingImportService Service { get; }

        public Fixture()
        {
            var workspaceId = Guid.NewGuid();
            var storeId = Guid.NewGuid();
            Store = new(storeId, workspaceId, "Store", new(PrintifyShopId: 42), false, Now, Now, FulfillmentStrategy.Printify);
            Niche = new(Guid.NewGuid(), storeId, "Wildlife", null, false, Now, Now, "{}");
            Scope = new(workspaceId, storeId);
            Repository = new(WorkspaceSnapshot.Empty with
            {
                Stores = [new Store(storeId, workspaceId, "Store", null, false, Now, Now, "{}", fulfillmentStrategy: FulfillmentStrategy.Printify)],
                Niches = [Niche]
            });
            Client = new();
            Service = new StoresStub(Store) is { } stores
                ? new PrintifyListingImportService(stores, new CredentialStub(), Client, Repository, Files, clock: () => Now, newId: Guid.NewGuid)
                : throw new InvalidOperationException();
        }
    }

    private sealed class StoresStub(StoreSummary store) : IStoreContextReader
    {
        public Task<StoreSummary?> ResolveActiveStoreAsync(Guid workspaceId, Guid storeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<StoreSummary?>(workspaceId == store.WorkspaceId && storeId == store.Id ? store : null);
    }

    private sealed class CredentialStub : IStorePrintifyCredentialStore
    {
        public PrintifyConfigurationKind StatusKind { get; init; } = PrintifyConfigurationKind.Available;
        public Task<PrintifyCredentialReadResult> ReadAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default) => Task.FromResult(new PrintifyCredentialReadResult(new(StatusKind, "ready"), StatusKind == PrintifyConfigurationKind.Available ? "synthetic-key" : null));
        public Task<PrintifyConfigurationResult> SaveAsync(StoreCredentialScope scope, string key, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class Service : IPrintifyListingImportClient
    {
        public int ShopProductCalls { get; private set; }
        private static readonly PrintifyListingProductDetail[] Products =
        [
            Product("visible-product", "Moonlit Fox", true),
            Product("hidden-product", "Hidden shirt", false),
            Product("unsupported-product", "Layered artwork", true) with
            {
                PrintAreas = [new("front", [11, 12], [new("layered-art", "https://images.printify.com/layered.png", "layered", "image/png", "front", [11, 12])], true)]
            }
        ];
        public Task<IReadOnlyList<PrintifyListingProductSummary>> GetShopProductsAsync(string apiKey, int shopId, CancellationToken cancellationToken = default)
        {
            ShopProductCalls++;
            return Task.FromResult<IReadOnlyList<PrintifyListingProductSummary>>(Products.Select(value => new PrintifyListingProductSummary(value.ProductId, value.Title, value.Description, value.IsVisible, value.BlueprintId, value.ProviderId)).ToArray());
        }
        public Task<PrintifyListingProductDetail?> GetProductAsync(string apiKey, int shopId, string productId, CancellationToken cancellationToken = default) => Task.FromResult(Products.SingleOrDefault(value => value.ProductId == productId));
        public Task<PrintifyArtworkDownload> DownloadArtworkAsync(string sourceUrl, CancellationToken cancellationToken = default) => Task.FromResult(new PrintifyArtworkDownload([1, 2, 3], "image/png", ".png"));
        private static PrintifyListingProductDetail Product(string id, string title, bool visible) => new(id, title, "A product description", visible, 68, 9, null,
            [new("Color", "color", [new(1, "Black"), new(2, "White")])],
            [new(11, "Black / M", 2500, true, true, [1]), new(12, "White / M", 2500, true, true, [2])],
            [new("front", [11, 12], [new("image-" + id, "https://images.printify.com/" + id + ".png?signature=secret", title, "image/png", "front", [11, 12])], false)]);
    }

    private sealed class CatalogStub : IPrintifyCatalogClient
    {
        public int Calls { get; private set; }
        public PrintifyCatalogResult Result { get; init; } = new(PrintifyCatalogResultKind.Succeeded, "loaded", SelectedProducts:
            [new PrintifyCatalogBlueprint(new(68, "Tee", "Description", "Brand", "Model"),
                [new PrintifyCatalogProvider(9, "Provider", [new("Color", "color", [new(1, "Black"), new(2, "White")])],
                    [new(11, "Black / M", true, true, [1], [new("front", "dtg", 100, 200)]),
                     new(12, "White / M", true, true, [2], [new("front", "dtg", 100, 200)])])]) { ProductId = "visible-product" }]);
        public Task<PrintifyCatalogResult> LoadBlueprintsAsync(string key, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PrintifyCatalogResult> LoadSelectedAsync(string key, IReadOnlyCollection<int> blueprintIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PrintifyCatalogResult> LoadSelectedProductsAsync(string key, int shopId, IReadOnlyCollection<string> productIds, CancellationToken cancellationToken = default)
        {
            Calls++;
            var productId = productIds.Single();
            return Task.FromResult(Result with
            {
                SelectedProducts = Result.SelectedProducts?.Select(value => value with { ProductId = productId }).ToArray()
            });
        }
    }

    private sealed class Repository(WorkspaceSnapshot initial) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; set; } = initial;
        public Exception? SaveException { get; set; }
        public Action? OnSave { get; set; }
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default) { if (SaveException is not null) throw SaveException; Snapshot = snapshot; OnSave?.Invoke(); return Task.CompletedTask; }
    }

    private sealed class FileStub : IWorkspaceFileOutputStore
    {
        private int _next;
        public int SaveCount { get; private set; }
        public int DeleteCount { get; private set; }
        public bool Exists(string workspaceRelativePath) => true;
        public Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream());
        public Task<ManagedWorkspaceFile> SaveAsync(string fileName, AssetKind kind, Stream content, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            var path = $"assets/{++_next}.png";
            return Task.FromResult(new ManagedWorkspaceFile(fileName, kind, path, path, string.Empty));
        }
        public bool TryDelete(string workspaceRelativePath) { DeleteCount++; return true; }
    }
}
