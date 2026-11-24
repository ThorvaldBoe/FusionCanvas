using System.Net;
using FusionCanvas.Application.Stores;
using FusionCanvas.Application.Stores.Printify;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Persistence;
using FusionCanvas.Integration.Stores.Printify;

namespace FusionCanvas.Integration.Tests.Stores.Printify;

public sealed class PrintifyCatalogImportPersistenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task BlueprintImportMapsObjectVariantChoicesFromShopDefinitionsAndIsStableAcrossRepeatedSqliteImports()
    {
        using var directory = new TemporaryDirectory();
        var databasePath = directory.GetPath("gildan-145-import.db");
        var workspaceId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var store = new Store(storeId, workspaceId, "Printify Store", null, false, Now, Now,
            "{\"printifyShopId\":\"42\"}", null, FulfillmentStrategy.ShopifyPrintify);
        var repository = new SqliteWorkspaceRepository(databasePath, useConnectionPooling: false);
        await repository.SaveAsync(new WorkspaceSnapshot(
            [new Workspace(workspaceId, "Workspace", null, false, Now, Now, "{}")],
            [store], [], [], [], [], [], [], [], []), TestContext.Current.CancellationToken);

        using var httpClient = new HttpClient(new SyntheticPrintifyHandler()) { BaseAddress = PrintifyCatalogClient.CatalogBaseUri };
        var client = new PrintifyCatalogClient(httpClient);
        var summary = new StoreSummary(storeId, workspaceId, store.Name, new(PrintifyShopId: 42), false, Now, Now,
            FulfillmentStrategy.ShopifyPrintify);
        var service = new PrintifyCatalogImportService(new StoresStub(summary), new CredentialStore(), client, repository);
        var scope = new StoreCredentialScope(workspaceId, storeId);

        var first = await service.LoadSelectedAsync(scope, [145], TestContext.Current.CancellationToken);
        Assert.True(first.Succeeded, first.Message);
        var firstSnapshot = await repository.LoadAsync(TestContext.Current.CancellationToken);
        var firstVariantIds = firstSnapshot.OfferingVariants.ToDictionary(
            variant => $"{firstSnapshot.BlueprintOfferings.Single(offering => offering.Id == variant.OfferingId).ExternalOfferingId}:{variant.Name}",
            variant => variant.Id);

        var second = await service.LoadSelectedAsync(scope, [145], TestContext.Current.CancellationToken);
        Assert.True(second.Succeeded, second.Message);
        var loaded = await new SqliteWorkspaceRepository(databasePath, useConnectionPooling: false)
            .LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Gildan 64000", Assert.Single(loaded.Blueprints).Name);
        Assert.Equal(["145:39", "145:99"], loaded.BlueprintOfferings.Select(value => value.ExternalOfferingId).Order(StringComparer.Ordinal));
        Assert.Equal(2, loaded.PrintProviders.Count);

        var provider39Offering = loaded.BlueprintOfferings.Single(value => value.ExternalOfferingId == "145:39");
        var provider39Options = loaded.OfferingOptions.Where(value => value.OfferingId == provider39Offering.Id).ToDictionary(value => value.Name);
        Assert.Equal(["Colors", "Size"], provider39Options.Keys.Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(OptionKind.Color, provider39Options["Colors"].OptionKind);
        Assert.Equal(OptionKind.Size, provider39Options["Size"].OptionKind);
        var provider39Values = loaded.OfferingOptionValues.Where(value => value.OfferingId == provider39Offering.Id).ToArray();
        var charcoal = Assert.Single(provider39Values, value => value.Value == "Charcoal");
        var white = Assert.Single(provider39Values, value => value.Value == "White");
        var small = Assert.Single(provider39Values, value => value.Value == "S");
        var medium = Assert.Single(provider39Values, value => value.Value == "M");
        var provider39Variants = loaded.OfferingVariants.Where(value => value.OfferingId == provider39Offering.Id).ToArray();
        Assert.Equal(["Charcoal / S", "White / M"], provider39Variants.Select(value => value.Name).Order(StringComparer.Ordinal));
        Assert.Equal(new[] { charcoal.Id, small.Id }.Order().ToArray(), provider39Variants.Single(value => value.Name == "Charcoal / S").OptionValueIds.Order().ToArray());
        Assert.Equal(new[] { white.Id, medium.Id }.Order().ToArray(), provider39Variants.Single(value => value.Name == "White / M").OptionValueIds.Order().ToArray());
        Assert.Contains(provider39Variants, value => value.Name == "White / M" && value.ProviderIsAvailable == true);

        var provider99Offering = loaded.BlueprintOfferings.Single(value => value.ExternalOfferingId == "145:99");
        var provider99Variants = loaded.OfferingVariants.Where(value => value.OfferingId == provider99Offering.Id).ToArray();
        Assert.Equal(["Charcoal / S"], provider99Variants.Select(value => value.Name));
        var provider99Placeholder = Assert.Single(loaded.OfferingPlaceholders, value => value.OfferingId == provider99Offering.Id);
        Assert.Equal(1200, provider99Placeholder.Width);
        Assert.Equal(1600, provider99Placeholder.Height);
        Assert.Equal(provider99Variants.Select(value => value.Id).Order().ToArray(), provider99Placeholder.VariantIds.Order().ToArray());

        Assert.Equal(firstVariantIds, loaded.OfferingVariants.ToDictionary(
            variant => $"{loaded.BlueprintOfferings.Single(offering => offering.Id == variant.OfferingId).ExternalOfferingId}:{variant.Name}",
            variant => variant.Id));
    }

    [Fact]
    public async Task RepeatedImportUpdatesProviderRecordsAndPreservesLocalOnlyBlueprint()
    {
        using var directory = new TemporaryDirectory();
        var databasePath = directory.GetPath("printify-import.db");
        var workspaceId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var store = new Store(storeId, workspaceId, "Printify Store", null, false, Now, Now, "{\"printifyShopId\":\"42\"}", null, FulfillmentStrategy.ShopifyPrintify);
        var localBlueprint = new Blueprint(Guid.NewGuid(), storeId, "Local Draft", null, false, Now, Now);
        var repository = new SqliteWorkspaceRepository(databasePath, useConnectionPooling: false);
        await repository.SaveAsync(new WorkspaceSnapshot([new Workspace(workspaceId, "Workspace", null, false, Now, Now, "{}")], [store], [], [], [], [], [], [], [], [])
        {
            Blueprints = [localBlueprint]
        }, TestContext.Current.CancellationToken);

        var summary = new StoreSummary(storeId, workspaceId, store.Name, new(PrintifyShopId: 42), false, Now, Now, FulfillmentStrategy.ShopifyPrintify);
        var client = new ClientStub { Catalog = Catalog("First title", 3000) };
        var service = new PrintifyCatalogImportService(new StoresStub(summary), new CredentialStore(), client, repository);

        var first = await service.LoadSelectedAsync(new(workspaceId, storeId), [68], TestContext.Current.CancellationToken);
        client.Catalog = Catalog("Changed title", 4500);
        var second = await service.LoadSelectedAsync(new(workspaceId, storeId), [68], TestContext.Current.CancellationToken);
        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(first.Succeeded, first.Message);
        Assert.True(second.Succeeded, second.Message);
        Assert.Single(loaded.Blueprints, value => value.Id != localBlueprint.Id);
        Assert.Equal("Brand Model", loaded.Blueprints.Single(value => value.Id != localBlueprint.Id).Name);
        Assert.Contains(loaded.Blueprints, value => value.Id == localBlueprint.Id && value.Name == "Local Draft");
        Assert.Single(loaded.PrintProviders);
        Assert.Equal("Changed provider", Assert.Single(loaded.PrintProviders).Name);
        Assert.Single(loaded.BlueprintOfferings);
        Assert.Equal("Changed title · Changed provider", Assert.Single(loaded.BlueprintOfferings).Name);
        Assert.Single(loaded.OfferingVariants);
        Assert.Equal("Changed variant", Assert.Single(loaded.OfferingVariants).Name);
        Assert.Single(loaded.OfferingPlaceholders);
        Assert.Equal(4500, Assert.Single(loaded.OfferingPlaceholders).Width);
        Assert.Contains(loaded.StoreProducts, value => value.Id != localBlueprint.Id && value.Name == "Brand Model");
        Assert.Single(loaded.FulfillmentOfferings);
        Assert.Single(loaded.ProductVariants);
        Assert.Equal(4500, Assert.Single(loaded.DesignAreas).Width);
        Assert.Single(loaded.OfferingOptions);
        Assert.Single(loaded.OfferingOptionValues);
    }

    [Fact]
    public async Task ConsolidationRoundTripsArchivedDuplicateAndMigratedMockupReferences()
    {
        using var directory = new TemporaryDirectory();
        var databasePath = directory.GetPath("printify-consolidation.db");
        var workspaceId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var store = new Store(storeId, workspaceId, "Printify Store", null, false, Now, Now, "{\"printifyShopId\":\"42\"}", null, FulfillmentStrategy.ShopifyPrintify);
        var repository = new SqliteWorkspaceRepository(databasePath, useConnectionPooling: false);
        await repository.SaveAsync(new WorkspaceSnapshot([new Workspace(workspaceId, "Workspace", null, false, Now, Now, "{}")], [store], [], [], [], [], [], [], [], []), TestContext.Current.CancellationToken);

        var summary = new StoreSummary(storeId, workspaceId, store.Name, new(PrintifyShopId: 42), false, Now, Now, FulfillmentStrategy.ShopifyPrintify);
        var client = new ClientStub { Catalog = Catalog("Initial title", 100) };
        var service = new PrintifyCatalogImportService(new StoresStub(summary), new CredentialStore(), client, repository);
        var first = await service.LoadSelectedAsync(new(workspaceId, storeId), [68], TestContext.Current.CancellationToken);
        var imported = await repository.LoadAsync(TestContext.Current.CancellationToken);
        var offering = Assert.Single(imported.BlueprintOfferings);
        var area = Assert.Single(imported.OfferingPlaceholders);
        var duplicate = new OfferingPlaceholder(Guid.NewGuid(), offering.Id, "front", null, "front", "dtg", 80, 160,
            area.VariantIds, false, Now, Now, area.MetadataJson, "front");
        var template = new MockupTemplate(Guid.NewGuid(), offering.Id, duplicate.Id, "Front template", null, 1, false, Now, Now);
        var revision = new MockupTemplateRevision(Guid.NewGuid(), template.Id, 1, duplicate.Id, Now);
        var legacyDuplicate = new DesignArea(duplicate.Id, offering.Id, duplicate.Name, duplicate.Description, duplicate.Position, duplicate.DecorationMethod,
            duplicate.Width, duplicate.Height, duplicate.VariantIds, duplicate.CreatedAt, duplicate.UpdatedAt, duplicate.MetadataJson);
        await repository.SaveAsync(imported with
        {
            BlueprintOfferings = [offering with { DefaultPlaceholderId = duplicate.Id, PrimaryArtworkDesignAreaId = duplicate.Id }],
            OfferingPlaceholders = [area, duplicate],
            MockupTemplates = [template],
            MockupTemplateRevisions = [revision],
            DesignAreas = [.. imported.DesignAreas, legacyDuplicate]
        }, TestContext.Current.CancellationToken);

        client.Catalog = Catalog("Updated title", 120);
        var second = await service.LoadSelectedAsync(new(workspaceId, storeId), [68], TestContext.Current.CancellationToken);
        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(first.Succeeded, first.Message);
        Assert.True(second.Succeeded, second.Message);
        var canonical = Assert.Single(loaded.OfferingPlaceholders, value => !value.IsArchived);
        Assert.True(loaded.OfferingPlaceholders.Single(value => value.Id == duplicate.Id).IsArchived);
        Assert.Equal(canonical.Id, loaded.BlueprintOfferings.Single().DefaultPlaceholderId);
        Assert.Equal(canonical.Id, loaded.BlueprintOfferings.Single().PrimaryArtworkDesignAreaId);
        Assert.Equal(canonical.Id, loaded.MockupTemplates.Single().TargetPlaceholderId);
        Assert.Equal(canonical.Id, loaded.MockupTemplateRevisions.Single().TargetPlaceholderId);
        Assert.Single(loaded.DesignAreas);
        Assert.Equal(canonical.Id, loaded.DesignAreas.Single().Id);
        Assert.Equal(120, canonical.Width);
    }

    [Fact]
    public async Task ProviderIdentityConsolidationRoundTripsAliasesArchivedDuplicateAndOfferingReference()
    {
        using var directory = new TemporaryDirectory();
        var databasePath = directory.GetPath("provider-identity.db");
        var workspaceId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var blueprintId = Guid.NewGuid();
        var survivorId = Guid.NewGuid();
        var duplicateId = Guid.NewGuid();
        var offeringId = Guid.NewGuid();
        var store = new Store(storeId, workspaceId, "Printify Store", null, false, Now, Now, "{\"printifyShopId\":\"42\"}", null, FulfillmentStrategy.ShopifyPrintify);
        var blueprint = new Blueprint(blueprintId, storeId, "Tee", null, false, Now, Now);
        var survivor = new PrintProvider(survivorId, storeId, "SwiftPOD", "9", false, Now.AddMinutes(-2), Now.AddMinutes(-2), "{\"custom\":\"preserve\"}");
        var duplicate = new PrintProvider(duplicateId, storeId, " swiftpod ", "23", false, Now.AddMinutes(-1), Now.AddMinutes(-1));
        var offering = new BlueprintOffering(offeringId, blueprintId, storeId, "Tee · SwiftPOD", null, BlueprintOfferingKind.FixedPrintProvider, duplicateId, null, null, "68:23", false, Now, Now);
        var repository = new SqliteWorkspaceRepository(databasePath, useConnectionPooling: false);
        await repository.SaveAsync(new WorkspaceSnapshot([new Workspace(workspaceId, "Workspace", null, false, Now, Now, "{}")], [store], [], [], [], [], [], [], [], [])
        {
            Blueprints = [blueprint], PrintProviders = [survivor, duplicate], BlueprintOfferings = [offering]
        }, TestContext.Current.CancellationToken);

        var summary = new StoreSummary(storeId, workspaceId, store.Name, new(PrintifyShopId: 42), false, Now, Now, FulfillmentStrategy.ShopifyPrintify);
        var service = new PrintifyCatalogImportService(new StoresStub(summary), new CredentialStore(), new ClientStub
        {
            Catalog = [new(new(68, "Tee", null, null, null), [new(23, "SwiftPOD", [new("Color", "color", [new(1, "Black")])], [new(33719, "Black", true, true, [1], [])])])]
        }, repository);

        var result = await service.LoadSelectedAsync(new(workspaceId, storeId), [68], TestContext.Current.CancellationToken);
        var loaded = await new SqliteWorkspaceRepository(databasePath, useConnectionPooling: false).LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Message);
        var canonical = loaded.PrintProviders.Single(value => !value.IsArchived);
        Assert.Equal(survivorId, canonical.Id);
        Assert.True(loaded.PrintProviders.Single(value => value.Id == duplicateId).IsArchived);
        Assert.Equal(survivorId, loaded.BlueprintOfferings.Single().PrintProviderId);
        Assert.Equal("preserve", System.Text.Json.JsonDocument.Parse(canonical.MetadataJson).RootElement.GetProperty("custom").GetString());
        Assert.Equal(["23", "9"], System.Text.Json.JsonDocument.Parse(canonical.MetadataJson).RootElement.GetProperty("externalProviderIds").EnumerateArray().Select(value => value.GetString()).OrderBy(value => value));
    }

    private static IReadOnlyList<PrintifyCatalogBlueprint> Catalog(string title, int width) =>
    [
        new(
            new(68, title, "Description", "Brand", "Model"),
             [new(7, "Changed provider", [new("Color", "color", [new(1, "Black")])], [new(33719, "Changed variant", true, true, [1], [new("front", "dtg", width, 4500)])])])
    ];

    private sealed class StoresStub(StoreSummary store) : IStoreContextReader
    {
        public Task<StoreSummary?> ResolveActiveStoreAsync(Guid workspaceId, Guid storeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<StoreSummary?>(workspaceId == store.WorkspaceId && storeId == store.Id && !store.IsArchived ? store : null);
    }

    private sealed class CredentialStore : IStorePrintifyCredentialStore
    {
        public Task<PrintifyCredentialReadResult> ReadAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PrintifyCredentialReadResult(new(PrintifyConfigurationKind.Available, "available"), "test-key"));
        public Task<PrintifyConfigurationResult> SaveAsync(StoreCredentialScope scope, string key, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class ClientStub : IPrintifyCatalogClient
    {
        public IReadOnlyList<PrintifyCatalogBlueprint> Catalog { get; set; } = [];
        public Task<PrintifyCatalogResult> LoadBlueprintsAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(new PrintifyCatalogResult(PrintifyCatalogResultKind.Empty, "unused", []));
        public Task<PrintifyCatalogResult> LoadSelectedAsync(string key, IReadOnlyCollection<int> blueprintIds, CancellationToken cancellationToken = default) => Task.FromResult(new PrintifyCatalogResult(PrintifyCatalogResultKind.Succeeded, "loaded", SelectedCatalog: Catalog));
    }

    private sealed class SyntheticPrintifyHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            var body = path switch
            {
                "/v1/shops/42/products.json" => """
                    {"last_page":1,"total":3,"data":[
                      {"id":"shop-product-39-a","title":"Charcoal tee","blueprint_id":145,"print_provider_id":39,
                       "options":[{"name":"Colors","type":"color","values":[{"id":5,"title":"Charcoal"},{"id":6,"title":"White"}]},{"name":"Size","type":"size","values":[{"id":1,"title":"S"},{"id":2,"title":"M"}]}],
                       "variants":[{"id":401,"title":"Charcoal / S","options":[5,1]}]},
                      {"id":"shop-product-39-b","title":"White tee","blueprint_id":145,"print_provider_id":39,
                       "options":[{"name":"Size","type":"size","values":[{"id":1,"title":"S"},{"id":2,"title":"M"}]},{"name":"Colors","type":"color","values":[{"id":5,"title":"Charcoal"},{"id":6,"title":"White"}]}],
                       "variants":[]},
                      {"id":"shop-product-99-a","title":"Provider 99 tee","blueprint_id":145,"print_provider_id":99,
                       "options":[{"name":"Colors","type":"color","values":[{"id":5,"title":"Charcoal"},{"id":6,"title":"White"}]},{"name":"Size","type":"size","values":[{"id":1,"title":"S"},{"id":2,"title":"M"}]}],
                       "variants":[{"id":901,"title":"Charcoal / S","options":[5,1]}]}]}
                    """,
                "/v1/catalog/blueprints/145.json" => """
                    {"id":145,"title":"Unisex Garment-Dyed T-Shirt","brand":"Gildan","model":"64000"}
                    """,
                "/v1/catalog/blueprints/145/print_providers.json" => """
                    [{"id":39,"title":"SwiftPOD"},{"id":99,"title":"Printify Choice"}]
                    """,
                "/v1/catalog/blueprints/145/print_providers/39/variants.json" => """
                    {"variants":[
                      {"id":401,"title":"Charcoal / S","options":{"size":"S","color":"Charcoal"},"placeholders":[{"position":"front","decoration_method":"dtg","width":1800,"height":2400}]},
                      {"id":402,"title":"White / M","options":{"color":"White","size":"M"},"placeholders":[{"position":"front","decoration_method":"dtg","width":1800,"height":2400}]}]}
                    """,
                "/v1/catalog/blueprints/145/print_providers/99/variants.json" => """
                    {"variants":[
                      {"id":901,"title":"Charcoal / S","options":{"size":"S","color":"Charcoal"},"placeholders":[{"position":"front","decoration_method":"dtg","width":1200,"height":1600}]}]}
                    """,
                _ => throw new InvalidOperationException($"Unexpected synthetic Printify request: {request.RequestUri}")
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), "FusionCanvas", Guid.NewGuid().ToString("N"));
        public TemporaryDirectory() => Directory.CreateDirectory(_path);
        public string GetPath(string fileName) => Path.Combine(_path, fileName);
        public void Dispose() { if (Directory.Exists(_path)) Directory.Delete(_path, true); }
    }
}
