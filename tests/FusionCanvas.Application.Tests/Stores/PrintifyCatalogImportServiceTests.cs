using System.Text.Json;
using FusionCanvas.Application.Stores;
using FusionCanvas.Application.Stores.Printify;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests.Stores;

public sealed class PrintifyCatalogImportServiceTests
{
    [Fact]
    public async Task RefusesManualStoreWithoutReadingCredential()
    {
        var store = new StoreSummary(Guid.NewGuid(), Guid.NewGuid(), "Store", new(PrintifyShopId: 42), false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, FulfillmentStrategy.Manual);
        var stores = new StoresStub(store);
        var credentials = new CredentialsStub();
        var client = new ClientStub();
        var service = new PrintifyCatalogImportService(stores, credentials, client);

        var result = await service.LoadBlueprintsAsync(new(store.WorkspaceId, store.Id), TestContext.Current.CancellationToken);

        Assert.Equal(PrintifyCatalogResultKind.InvalidRequest, result.Kind);
        Assert.Equal(0, credentials.Reads);
        Assert.Equal(0, client.Calls);
    }

    [Theory]
    [InlineData(FulfillmentStrategy.ShopifyPrintify)]
    [InlineData(FulfillmentStrategy.Printify)]
    public async Task LoadsCatalogOnlyAfterAllStoreGuardsPass(FulfillmentStrategy strategy)
    {
        var store = new StoreSummary(Guid.NewGuid(), Guid.NewGuid(), "Store", new(PrintifyShopId: 42), false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, strategy);
        var credentials = new CredentialsStub { Result = new(new(PrintifyConfigurationKind.Available, "available"), "synthetic-key") };
        var client = new ClientStub { Result = new(PrintifyCatalogResultKind.Succeeded, "loaded", []) };
        var service = new PrintifyCatalogImportService(new StoresStub(store), credentials, client);

        var result = await service.LoadBlueprintsAsync(new(store.WorkspaceId, store.Id), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(1, credentials.Reads);
        Assert.Equal("synthetic-key", client.LastKey);
    }

    [Fact]
    public async Task LoadsProductsFromTheSelectedPrintifyShop()
    {
        var store = new StoreSummary(Guid.NewGuid(), Guid.NewGuid(), "Store", new(PrintifyShopId: 42), false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, FulfillmentStrategy.Printify);
        var credentials = new CredentialsStub { Result = new(new(PrintifyConfigurationKind.Available, "available"), "synthetic-key") };
        var client = new ShopProductsClientStub();
        var service = new PrintifyCatalogImportService(new StoresStub(store), credentials, client);

        var result = await service.LoadBlueprintsAsync(new(store.WorkspaceId, store.Id), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(42, client.ShopId);
        Assert.Equal("synthetic-key", client.LastKey);
        Assert.Equal(["product-a"], result.Products!.Select(value => value.ProductId));
    }

    [Fact]
    public async Task ImportsSelectedCatalogAndUpdatesExistingPrintifyRecords()
    {
        var store = new StoreSummary(Guid.NewGuid(), Guid.NewGuid(), "Store", new(PrintifyShopId: 42), false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, FulfillmentStrategy.Printify);
        var repository = new RepositoryStub(WorkspaceSnapshot.Empty with { Stores = [new Store(store.Id, store.WorkspaceId, store.Name, null, false, store.CreatedAt, store.UpdatedAt, "{}", null, store.FulfillmentStrategy)] });
        var client = new ClientStub
        {
            SelectedResult = new(PrintifyCatalogResultKind.Succeeded, "loaded", SelectedCatalog: [new(
                new(68, "Updated Tee", null, "Gildan", "5000"),
                 [new(9, "Provider", [new("Color", "color", [new(1, "Black")])], [new(33719, "Black", true, true, [1], [new("front", "dtg", 100, 200)])])])])
        };
        var credentials = new CredentialsStub { Result = new(new(PrintifyConfigurationKind.Available, "available"), "synthetic-key") };
        var service = new PrintifyCatalogImportService(new StoresStub(store), credentials, client, repository);

        var first = await service.LoadSelectedAsync(new(store.WorkspaceId, store.Id), [68], TestContext.Current.CancellationToken);
        var second = await service.LoadSelectedAsync(new(store.WorkspaceId, store.Id), [68], TestContext.Current.CancellationToken);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Single(repository.Snapshot.Blueprints);
        Assert.Equal("Gildan 5000", Assert.Single(repository.Snapshot.Blueprints).Name);
        Assert.Equal("Gildan 5000", Assert.Single(repository.Snapshot.StoreProducts).Name);
        Assert.Single(repository.Snapshot.PrintProviders);
        Assert.Single(repository.Snapshot.BlueprintOfferings);
        var option = Assert.Single(repository.Snapshot.OfferingOptions);
        Assert.Equal(OptionKind.Color, option.OptionKind);
        Assert.Contains("\"kind\":\"option\"", option.MetadataJson);
        var optionValue = Assert.Single(repository.Snapshot.OfferingOptionValues);
        Assert.Contains("\"kind\":\"option-value\"", optionValue.MetadataJson);
        Assert.Single(repository.Snapshot.OfferingVariants);
        var variant = Assert.Single(repository.Snapshot.OfferingVariants);
        var placeholder = Assert.Single(repository.Snapshot.OfferingPlaceholders);
        Assert.Equal([variant.Id], placeholder.VariantIds);
    }

    [Fact]
    public async Task ImportsPrintifyOptionDefinitionsAndAllDeclaredValues()
    {
        var store = new StoreSummary(Guid.NewGuid(), Guid.NewGuid(), "Store", new(PrintifyShopId: 42), false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, FulfillmentStrategy.Printify);
        var repository = new RepositoryStub(WorkspaceSnapshot.Empty with
        {
            Stores = [new Store(store.Id, store.WorkspaceId, store.Name, null, false, store.CreatedAt, store.UpdatedAt, "{}", null, store.FulfillmentStrategy)]
        });
        var option = new PrintifyCatalogOption("Color", "color", [
            new(101, "Black"),
            new(102, "White")
        ]);
        var client = new ClientStub
        {
            SelectedResult = new(PrintifyCatalogResultKind.Succeeded, "loaded", SelectedCatalog: [new(
                new(68, "Tee", null, "Brand", "Model"),
                [new(9, "Provider", [option], [new(33719, "Black", true, true, [101], [])])])])
        };
        var service = new PrintifyCatalogImportService(
            new StoresStub(store),
            new CredentialsStub { Result = new(new(PrintifyConfigurationKind.Available, "available"), "synthetic-key") },
            client,
            repository);

        var result = await service.LoadSelectedAsync(new(store.WorkspaceId, store.Id), [68], TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Message);
        var importedOption = Assert.Single(repository.Snapshot.OfferingOptions);
        Assert.Equal("Color", importedOption.Name);
        Assert.Equal(OptionKind.Color, importedOption.OptionKind);
        Assert.Equal(["Black", "White"], repository.Snapshot.OfferingOptionValues.OrderBy(value => value.SortOrder).Select(value => value.Value));
        Assert.Contains(
            repository.Snapshot.OfferingOptionValues.Single(value => value.Value == "Black").Id,
            Assert.Single(repository.Snapshot.OfferingVariants).OptionValueIds);
    }

    [Fact]
    public async Task ShopProductImportPreservesLegacyOfferingIdentityAndMockupTemplates()
    {
        var store = new StoreSummary(Guid.NewGuid(), Guid.NewGuid(), "Store", new(PrintifyShopId: 42), false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, FulfillmentStrategy.Printify);
        var blueprintId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var offeringId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var blueprint = new Blueprint(blueprintId, store.Id, "Old Tee", null, false, now, now, "{\"source\":\"printify\",\"kind\":\"blueprint\",\"ids\":[68]}");
        var provider = new PrintProvider(providerId, store.Id, "Old Provider", "9", false, now, now, "{}");
        var offering = new BlueprintOffering(offeringId, blueprintId, store.Id, "Old Tee · Old Provider", null, BlueprintOfferingKind.FixedPrintProvider, providerId, null, null, "68:9", false, now, now);
        var template = new MockupTemplate(templateId, offeringId, null, "Existing mockup", null, 1, false, now, now);
        var repository = new RepositoryStub(new WorkspaceSnapshot([], [
            new Store(store.Id, store.WorkspaceId, store.Name, null, false, now, now, "{}", null, store.FulfillmentStrategy)
        ], [], [], [], [], [], [], [], []) with
        {
            Blueprints = [blueprint],
            PrintProviders = [provider],
            BlueprintOfferings = [offering],
            MockupTemplates = [template]
        });
        var client = new ClientStub
        {
            SelectedResult = new(PrintifyCatalogResultKind.Succeeded, "loaded", SelectedCatalog: [new PrintifyCatalogBlueprint(
                new(68, "Updated Tee", null, null, null),
                 [new(9, "Updated Provider", [new("Color", "color", [new(1, "Black")])], [new(33719, "Black", true, true, [1], [new("front", "dtg", 100, 200)])])])
                { ProductId = "shop-product-1" }])
        };
        var service = new PrintifyCatalogImportService(new StoresStub(store), new CredentialsStub { Result = new(new(PrintifyConfigurationKind.Available, "available"), "key") }, client, repository);

        var result = await service.LoadSelectedAsync(new(store.WorkspaceId, store.Id), [68], TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Message);
        Assert.Single(repository.Snapshot.Blueprints);
        Assert.Equal("Updated Tee", Assert.Single(repository.Snapshot.Blueprints).Name);
        Assert.Single(repository.Snapshot.BlueprintOfferings);
        Assert.Equal(offeringId, repository.Snapshot.BlueprintOfferings[0].Id);
        Assert.Equal(offeringId, repository.Snapshot.MockupTemplates[0].BlueprintOfferingId);
        Assert.Equal("Updated Tee · Updated Provider", repository.Snapshot.BlueprintOfferings[0].Name);
    }

    [Fact]
    public async Task ImportsProviderIdentityWithinTargetStoreOnly()
    {
        var workspaceId = Guid.NewGuid();
        var targetStore = new StoreSummary(Guid.NewGuid(), workspaceId, "Target", new(PrintifyShopId: 42), false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, FulfillmentStrategy.ShopifyPrintify);
        var otherStore = new StoreSummary(Guid.NewGuid(), workspaceId, "Other", new(PrintifyShopId: 84), false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, FulfillmentStrategy.ShopifyPrintify);
        var otherProvider = new PrintProvider(Guid.NewGuid(), otherStore.Id, "Other provider", "7", false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "{}");
        var repository = new RepositoryStub(new WorkspaceSnapshot([], [
            new Store(targetStore.Id, workspaceId, targetStore.Name, null, false, targetStore.CreatedAt, targetStore.UpdatedAt, "{}", null, targetStore.FulfillmentStrategy),
            new Store(otherStore.Id, workspaceId, otherStore.Name, null, false, otherStore.CreatedAt, otherStore.UpdatedAt, "{}", null, otherStore.FulfillmentStrategy)
        ], [], [], [], [], [], [], [], []) { PrintProviders = [otherProvider] });
        var client = new ClientStub
        {
            SelectedResult = new(PrintifyCatalogResultKind.Succeeded, "loaded", SelectedCatalog: [new(
                new(68, "Updated Tee", null, "Gildan", "5000"),
                 [new(7, "Target provider", [new("Color", "color", [new(1, "Black")])], [new(33719, "Black", true, true, [1], [new("front", "dtg", 100, 200)])])])])
        };
        var credentials = new CredentialsStub { Result = new(new(PrintifyConfigurationKind.Available, "available"), "synthetic-key") };
        var service = new PrintifyCatalogImportService(new StoresStub(targetStore), credentials, client, repository);

        var result = await service.LoadSelectedAsync(new(workspaceId, targetStore.Id), [68], TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(2, repository.Snapshot.PrintProviders.Count);
        Assert.Contains(repository.Snapshot.PrintProviders, value => value.StoreId == otherStore.Id && value.ExternalProviderId == "7");
        Assert.Contains(repository.Snapshot.PrintProviders, value => value.StoreId == targetStore.Id && value.ExternalProviderId == "7");
    }

    [Fact]
    public async Task ReusesLegacyProviderWithSameNameWhenExternalIdentityIsMissing()
    {
        var store = new StoreSummary(Guid.NewGuid(), Guid.NewGuid(), "Store", new(PrintifyShopId: 42), false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, FulfillmentStrategy.Printify);
        var legacyProvider = new PrintProvider(Guid.NewGuid(), store.Id, "SwiftPOD", null, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var repository = new RepositoryStub(WorkspaceSnapshot.Empty with
        {
            Stores = [new Store(store.Id, store.WorkspaceId, store.Name, null, false, store.CreatedAt, store.UpdatedAt, "{}", null, store.FulfillmentStrategy)],
            PrintProviders = [legacyProvider]
        });
        var client = new ClientStub
        {
            SelectedResult = new(PrintifyCatalogResultKind.Succeeded, "loaded", SelectedCatalog: [new(
                new(68, "Tee", null, null, null),
                 [new(9, "SwiftPOD", [new("Color", "color", [new(1, "Black")])], [new(33719, "Black", true, true, [1], [])])]) { ProductId = "product-a" }])
        };
        var service = new PrintifyCatalogImportService(new StoresStub(store), new CredentialsStub { Result = new(new(PrintifyConfigurationKind.Available, "available"), "key") }, client, repository);

        var result = await service.LoadSelectedAsync(new(store.WorkspaceId, store.Id), ["product-a"], TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Message);
        Assert.Single(repository.Snapshot.PrintProviders);
        Assert.Equal(legacyProvider.Id, repository.Snapshot.BlueprintOfferings.Single().PrintProviderId);
        Assert.Equal("9", repository.Snapshot.PrintProviders.Single().ExternalProviderId);
    }

    [Fact]
    public async Task ConsolidatesSameNameProvidersWithDifferentExternalIdsAndIsIdempotent()
    {
        var store = TestStore();
        var repository = TestRepository(store);
        var catalog = new PrintifyCatalogBlueprint(
            new(68, "Tee", null, null, null),
            [
                new(9, "SwiftPOD", [new("Color", "color", [new(1, "Black")])], [new(33719, "Black", true, true, [1], [])]),
                new(23, " swiftpod ", [new("Color", "color", [new(2, "White")])], [new(33720, "White", true, true, [2], [])])
            ]);
        var service = TestService(store, repository, new ClientStub
        {
            SelectedResult = new(PrintifyCatalogResultKind.Succeeded, "loaded", SelectedCatalog: [catalog])
        });

        var first = await service.LoadSelectedAsync(new(store.WorkspaceId, store.Id), [68], TestContext.Current.CancellationToken);
        var second = await service.LoadSelectedAsync(new(store.WorkspaceId, store.Id), [68], TestContext.Current.CancellationToken);

        Assert.True(first.Succeeded, first.Message);
        Assert.True(second.Succeeded, second.Message);
        Assert.Single(repository.Snapshot.PrintProviders, value => !value.IsArchived);
        var activeProvider = repository.Snapshot.PrintProviders.Single(value => !value.IsArchived);
        Assert.Equal("9", activeProvider.ExternalProviderId);
        var aliases = System.Text.Json.JsonDocument.Parse(activeProvider.MetadataJson).RootElement.GetProperty("externalProviderIds").EnumerateArray().Select(value => value.GetString()).OrderBy(value => value);
        Assert.Equal(["23", "9"], aliases);
        Assert.Equal(2, repository.Snapshot.BlueprintOfferings.Count);
        Assert.All(repository.Snapshot.BlueprintOfferings, offering => Assert.Equal(activeProvider.Id, offering.PrintProviderId));
    }

    [Fact]
    public async Task RejectsDuplicateProviderPayloadWithoutSaving()
    {
        var store = new StoreSummary(Guid.NewGuid(), Guid.NewGuid(), "Store", new(PrintifyShopId: 42), false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, FulfillmentStrategy.ShopifyPrintify);
        var repository = new RepositoryStub(WorkspaceSnapshot.Empty with { Stores = [new Store(store.Id, store.WorkspaceId, store.Name, null, false, store.CreatedAt, store.UpdatedAt, "{}", null, store.FulfillmentStrategy)] });
        var duplicateProvider = new PrintifyCatalogProvider(7, "Provider", [], [new(33719, "Black", true, true, [1], [new("front", "dtg", 100, 200)])]);
        var client = new ClientStub
        {
            SelectedResult = new(PrintifyCatalogResultKind.Succeeded, "loaded", SelectedCatalog: [new(new(68, "Tee", null, "Brand", "Model"), [duplicateProvider, duplicateProvider])])
        };
        var service = new PrintifyCatalogImportService(new StoresStub(store), new CredentialsStub { Result = new(new(PrintifyConfigurationKind.Available, "available"), "key") }, client, repository);

        var result = await service.LoadSelectedAsync(new(store.WorkspaceId, store.Id), [68], TestContext.Current.CancellationToken);

        Assert.Equal(PrintifyCatalogResultKind.UnexpectedResponse, result.Kind);
        Assert.Contains("duplicate provider identities", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(repository.Snapshot.Blueprints);
        Assert.Empty(repository.Snapshot.PrintProviders);
    }

    [Fact]
    public async Task KeepsDistinctShopProductsThatShareBlueprintIdSeparate()
    {
        var store = TestStore();
        var repository = TestRepository(store);
        var client = new ClientStub { SelectedResult = new(PrintifyCatalogResultKind.Succeeded, "loaded", SelectedCatalog: [
            Product("product-a", "A"), Product("product-b", "B")]) };
        var service = TestService(store, repository, client);

        var result = await service.LoadSelectedAsync(new(store.WorkspaceId, store.Id), ["product-a", "product-b"], TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(2, repository.Snapshot.Blueprints.Count);
        Assert.Equal(["product-a", "product-b"], repository.Snapshot.Blueprints.Select(value => JsonDocument.Parse(value.MetadataJson).RootElement.GetProperty("productId").GetString()).OrderBy(value => value));
    }

    [Fact]
    public async Task UsesBrandAndModelWhenDescriptionIsPresent()
    {
        var store = TestStore();
        var repository = TestRepository(store);
        var client = new ClientStub { SelectedResult = new(PrintifyCatalogResultKind.Succeeded, "loaded", SelectedCatalog: [new(
            new(68, "Catalog title", "Description", "Gildan", "5000"), []) { ProductId = "product-a" }]) };
        var result = await TestService(store, repository, client).LoadSelectedAsync(new(store.WorkspaceId, store.Id), ["product-a"], TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal("Gildan 5000", Assert.Single(repository.Snapshot.Blueprints).Name);
    }

    [Fact]
    public async Task RejectsVariantReferencesToUndeclaredOptionValuesAtomically()
    {
        var store = TestStore();
        var repository = TestRepository(store);
        var invalid = new PrintifyCatalogBlueprint(new(68, "Tee", null, "Brand", "Model"), [
            new(9, "Provider", [new("Color", "color", [new(1, "Black")])], [new(33719, "Blue", true, true, [999], [])])
        ]) { ProductId = "product-a" };
        var result = await TestService(store, repository, new ClientStub { SelectedResult = new(PrintifyCatalogResultKind.Succeeded, "loaded", SelectedCatalog: [invalid]) })
            .LoadSelectedAsync(new(store.WorkspaceId, store.Id), ["product-a"], TestContext.Current.CancellationToken);

        Assert.Equal(PrintifyCatalogResultKind.UnexpectedResponse, result.Kind);
        Assert.Empty(repository.Snapshot.Blueprints);
    }

    [Fact]
    public async Task ConsolidatesVariantSpecificPrintAreasAndRebuildsMembershipWhenGeometryChanges()
    {
        var store = TestStore();
        var repository = TestRepository(store);
        var first = new PrintifyCatalogBlueprint(
            new(68, "Tee", null, "Brand", "Model"),
            [new(9, "Provider", [new("Color", "color", [new(1, "Black"), new(2, "White")])], [
                new(33, "Black", true, true, [1], [new("front", "dtg", 100, 200)]),
                new(337, "White", true, true, [2], [new("front", "dtg", 100, 200)])
            ])]) { ProductId = "product-a" };
        var firstResult = await TestService(store, repository, new ClientStub
        {
            SelectedResult = new(PrintifyCatalogResultKind.Succeeded, "loaded", SelectedCatalog: [first])
        }).LoadSelectedAsync(new(store.WorkspaceId, store.Id), ["product-a"], TestContext.Current.CancellationToken);

        Assert.True(firstResult.Succeeded, firstResult.Message);
        var firstArea = Assert.Single(repository.Snapshot.OfferingPlaceholders);
        var firstVariantIds = repository.Snapshot.OfferingVariants.ToDictionary(value => JsonDocument.Parse(value.MetadataJson).RootElement.GetProperty("ids")[0].GetInt32(), value => value.Id);
        Assert.Equal(firstVariantIds.Values.OrderBy(value => value), firstArea.VariantIds.OrderBy(value => value));

        var repeatResult = await TestService(store, repository, new ClientStub
        {
            SelectedResult = new(PrintifyCatalogResultKind.Succeeded, "loaded", SelectedCatalog: [first])
        }).LoadSelectedAsync(new(store.WorkspaceId, store.Id), ["product-a"], TestContext.Current.CancellationToken);

        Assert.True(repeatResult.Succeeded, repeatResult.Message);
        Assert.Equal(firstArea.Id, Assert.Single(repository.Snapshot.OfferingPlaceholders).Id);
        Assert.Equal(firstArea.VariantIds.OrderBy(value => value), repository.Snapshot.OfferingPlaceholders.Single().VariantIds.OrderBy(value => value));

        var changed = first with
        {
            Providers = [new(9, "Provider", [new("Color", "color", [new(1, "Black"), new(2, "White")])], [
                new(33, "Black", true, true, [1], [new("front", "dtg", 100, 200)]),
                new(337, "White", true, true, [2], [new("front", "dtg", 120, 200)])
            ])]
        };
        var changedResult = await TestService(store, repository, new ClientStub
        {
            SelectedResult = new(PrintifyCatalogResultKind.Succeeded, "loaded", SelectedCatalog: [changed])
        }).LoadSelectedAsync(new(store.WorkspaceId, store.Id), ["product-a"], TestContext.Current.CancellationToken);

        Assert.True(changedResult.Succeeded, changedResult.Message);
        var area = Assert.Single(repository.Snapshot.OfferingPlaceholders);
        Assert.Equal(120, area.Width);
        Assert.Equal(200, area.Height);
        Assert.Equal(firstVariantIds.Values.OrderBy(value => value), area.VariantIds.OrderBy(value => value));
    }

    [Fact]
    public async Task ConsolidationArchivesOlderSplitAreasAndMigratesReferences()
    {
        var store = TestStore();
        var repository = TestRepository(store);
        var first = new PrintifyCatalogBlueprint(
            new(68, "Tee", null, "Brand", "Model"),
            [new(9, "Provider", [new("Color", "color", [new(1, "Black"), new(2, "White")])], [
                new(33, "Black", true, true, [1], [new("front", "dtg", 100, 200)]),
                new(337, "White", true, true, [2], [new("front", "dtg", 100, 200)])
            ])]) { ProductId = "product-a" };
        await TestService(store, repository, new ClientStub
        {
            SelectedResult = new(PrintifyCatalogResultKind.Succeeded, "loaded", SelectedCatalog: [first])
        }).LoadSelectedAsync(new(store.WorkspaceId, store.Id), ["product-a"], TestContext.Current.CancellationToken);

        var offering = Assert.Single(repository.Snapshot.BlueprintOfferings);
        var area = Assert.Single(repository.Snapshot.OfferingPlaceholders);
        var duplicate = new OfferingPlaceholder(Guid.NewGuid(), offering.Id, "front", null, "front", "dtg", 80, 160,
            area.VariantIds, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, area.MetadataJson, "front");
        var template = new MockupTemplate(Guid.NewGuid(), offering.Id, duplicate.Id, "Front template", null, 1, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var revision = new MockupTemplateRevision(Guid.NewGuid(), template.Id, 1, duplicate.Id, DateTimeOffset.UtcNow);
        var assignment = new DesignSlotAssignment(Guid.NewGuid(), duplicate.Id, Guid.NewGuid());
        repository.SetSnapshot(repository.Snapshot with
        {
            BlueprintOfferings = [offering with { DefaultPlaceholderId = duplicate.Id, PrimaryArtworkDesignAreaId = duplicate.Id }],
            OfferingPlaceholders = [area, duplicate],
            MockupTemplates = [template],
            MockupTemplateRevisions = [revision],
            DesignSlotAssignments = [assignment]
        });

        var result = await TestService(store, repository, new ClientStub
        {
            SelectedResult = new(PrintifyCatalogResultKind.Succeeded, "loaded", SelectedCatalog: [first])
        }).LoadSelectedAsync(new(store.WorkspaceId, store.Id), ["product-a"], TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Message);
        var canonical = Assert.Single(repository.Snapshot.OfferingPlaceholders, value => !value.IsArchived);
        Assert.True(repository.Snapshot.OfferingPlaceholders.Single(value => value.Id == duplicate.Id).IsArchived);
        Assert.Equal(canonical.Id, repository.Snapshot.BlueprintOfferings.Single().DefaultPlaceholderId);
        Assert.Equal(canonical.Id, repository.Snapshot.BlueprintOfferings.Single().PrimaryArtworkDesignAreaId);
        Assert.Equal(canonical.Id, repository.Snapshot.MockupTemplates.Single().TargetPlaceholderId);
        Assert.Equal(canonical.Id, repository.Snapshot.MockupTemplateRevisions.Single().TargetPlaceholderId);
        Assert.Equal(canonical.Id, repository.Snapshot.DesignSlotAssignments.Single().DesignAreaId);
    }

    private static StoreSummary TestStore() => new(Guid.NewGuid(), Guid.NewGuid(), "Store", new(PrintifyShopId: 42), false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, FulfillmentStrategy.Printify);
    private static RepositoryStub TestRepository(StoreSummary store) => new(WorkspaceSnapshot.Empty with
    {
        Stores = [new Store(store.Id, store.WorkspaceId, store.Name, null, false, store.CreatedAt, store.UpdatedAt, "{}", null, store.FulfillmentStrategy)]
    });
    private static PrintifyCatalogImportService TestService(StoreSummary store, RepositoryStub repository, ClientStub client) => new(
        new StoresStub(store), new CredentialsStub { Result = new(new(PrintifyConfigurationKind.Available, "available"), "key") }, client, repository);
    private static PrintifyCatalogBlueprint Product(string productId, string title) => new(
        new(68, title, null, "Brand", "Model"), [new(9, "Provider", [new("Color", "color", [new(1, "Black")])], [new(33719, title, true, true, [1], [])])]) { ProductId = productId };

    private sealed class StoresStub(StoreSummary store) : IStoreManagementService
    {
        public Guid? ActiveWorkspaceId => store.WorkspaceId;
        public Guid? ActiveStoreId => store.Id;
        public void SetActiveWorkspace(Guid? workspaceId) { }
        public Task<StoreManagementState> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(new StoreManagementState(store.WorkspaceId, [store], [], store.Id, store, false));
        public Task<StoreManagementResult> CreateStoreAsync(StoreManagementCreateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StoreManagementResult> UpdateStoreAsync(StoreManagementUpdateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StoreManagementResult> ArchiveStoreAsync(Guid storeId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StoreManagementResult> RestoreStoreAsync(Guid storeId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StoreManagementResult> DeleteStoreAsync(StoreManagementDeleteRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StoreManagementResult> SelectStoreAsync(Guid storeId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class CredentialsStub : IStorePrintifyCredentialStore
    {
        public int Reads { get; private set; }
        public PrintifyCredentialReadResult Result { get; init; } = new(new(PrintifyConfigurationKind.Missing, "missing"));
        public Task<PrintifyCredentialReadResult> ReadAsync(StoreCredentialScope scope, CancellationToken cancellationToken = default) { Reads++; return Task.FromResult(Result); }
        public Task<PrintifyConfigurationResult> SaveAsync(StoreCredentialScope scope, string key, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class ClientStub : IPrintifyCatalogClient
    {
        public int Calls { get; private set; }
        public string? LastKey { get; private set; }
        public PrintifyCatalogResult Result { get; init; } = new(PrintifyCatalogResultKind.Empty, "empty", []);
        public PrintifyCatalogResult SelectedResult { get; init; } = new(PrintifyCatalogResultKind.Empty, "empty", []);
        public Task<PrintifyCatalogResult> LoadBlueprintsAsync(string key, CancellationToken cancellationToken = default) { Calls++; LastKey = key; return Task.FromResult(Result); }
        public Task<PrintifyCatalogResult> LoadSelectedAsync(string key, IReadOnlyCollection<int> blueprintIds, CancellationToken cancellationToken = default) => Task.FromResult(SelectedResult);
    }

    private sealed class ShopProductsClientStub : IPrintifyCatalogClient
    {
        public int ShopId { get; private set; }
        public string? LastKey { get; private set; }

        public Task<PrintifyCatalogResult> LoadBlueprintsAsync(string key, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PrintifyCatalogResult> LoadSelectedAsync(string key, IReadOnlyCollection<int> blueprintIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<PrintifyCatalogResult> LoadShopProductsAsync(string key, int shopId, CancellationToken cancellationToken = default)
        {
            LastKey = key;
            ShopId = shopId;
            return Task.FromResult(new PrintifyCatalogResult(PrintifyCatalogResultKind.Succeeded, "loaded", Products: [new("product-a", "Test product", null, 68, 9)]));
        }
    }

    private sealed class RepositoryStub(WorkspaceSnapshot initial) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = initial;
        public void SetSnapshot(WorkspaceSnapshot snapshot) => Snapshot = snapshot;
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default) { Snapshot = snapshot; return Task.CompletedTask; }
    }
}
