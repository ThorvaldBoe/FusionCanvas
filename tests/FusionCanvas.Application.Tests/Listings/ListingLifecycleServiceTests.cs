using FusionCanvas.Application.Listings;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.Application.Tests.Listings;

public sealed class ListingLifecycleServiceTests
{
    [Fact]
    public async Task Create_saves_identity_after_definitive_response()
    {
        var storeId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var repository = new MemoryRepository();
        var products = new FakeProducts
        {
            CreateResult = new ListingMutationResult(
                "product-1",
                true,
                new ListingRemoteProduct("product-1", false, ListingPublicationState.Unpublished, ListingSnapshot.Empty))
        };
        var service = new ListingLifecycleService(
            repository,
            new FakeConnection(),
            products,
            new FakePublication(),
            () => new DateTimeOffset(2026, 10, 7, 0, 0, 0, TimeSpan.Zero));

        var result = await service.CreateOrUpdateAsync(
            new ListingConnectionRequest(storeId, "shop-1", false),
            Projection(storeId, itemId),
            TestContext.Current.CancellationToken);

        Assert.Equal(ListingLifecycleResultKind.Succeeded, result.Kind);
        Assert.Equal("product-1", result.Mapping!.Identity!.ProductId);
        Assert.Single(repository.Snapshot.ExternalListingMappings);
        Assert.Equal(ExternalListingSyncState.Synchronized, repository.Snapshot.ExternalListingMappings[0].SynchronizationState);
    }

    [Fact]
    public async Task Definitive_mutation_error_is_not_recorded_as_success()
    {
        var repository = new MemoryRepository();
        var products = new FakeProducts
        {
            CreateResult = new ListingMutationResult("product-1", true, ErrorCode: "UnexpectedResponse", ErrorMessage: "Printify rejected the payload.")
        };
        var service = new ListingLifecycleService(repository, new FakeConnection(), products, new FakePublication());

        var result = await service.CreateOrUpdateAsync(
            new ListingConnectionRequest(Guid.NewGuid(), "shop-1", false),
            Projection(Guid.NewGuid(), Guid.NewGuid()),
            TestContext.Current.CancellationToken);

        Assert.Equal(ListingLifecycleResultKind.Failed, result.Kind);
        Assert.Equal(ListingSynchronizationState.Failed, result.Mapping!.SynchronizationState);
        Assert.Equal("Printify rejected the payload.", result.Message);
    }

    [Fact]
    public async Task Ambiguous_create_is_persisted_and_blocks_blind_retry()
    {
        var storeId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var repository = new MemoryRepository();
        var products = new FakeProducts
        {
            CreateResult = new ListingMutationResult(null, false, ErrorCode: "timeout", ErrorMessage: "Response was lost.")
        };
        var service = new ListingLifecycleService(repository, new FakeConnection(), products, new FakePublication());
        var request = new ListingConnectionRequest(storeId, "shop-1", false);
        var projection = Projection(storeId, itemId);

        var first = await service.CreateOrUpdateAsync(request, projection, TestContext.Current.CancellationToken);
        var second = await service.CreateOrUpdateAsync(request, projection, TestContext.Current.CancellationToken);

        Assert.Equal(ListingLifecycleResultKind.Uncertain, first.Kind);
        Assert.Equal(ListingLifecycleResultKind.Uncertain, second.Kind);
        Assert.Equal(1, products.CreateCalls);
        Assert.Equal(ExternalListingSyncState.Uncertain, repository.Snapshot.ExternalListingMappings[0].SynchronizationState);
    }

    [Fact]
    public async Task Uncertain_create_lists_shop_and_requires_two_no_match_checks_before_retry()
    {
        var storeId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var projection = Projection(storeId, itemId) with { ExternalBlueprintId = 68, ExternalProviderId = 23 };
        var repository = new MemoryRepository
        {
            Snapshot = WorkspaceSnapshot.Empty with
            {
                ExternalListingMappings = [UncertainCreateMapping(storeId, itemId, projection.Title, 68, 23)]
            }
        };
        var products = new FakeProducts();
        var service = new ListingLifecycleService(repository, new FakeConnection(), products, new FakePublication());
        var request = new ListingConnectionRequest(storeId, "shop-1", false);

        var reconciliation = await service.ReconcileUncertainCreateAsync(request, itemId, projection, TestContext.Current.CancellationToken);
        Assert.Equal(ListingLifecycleResultKind.NoRemoteMatch, reconciliation.Kind);
        Assert.Equal(0, products.CreateCalls);

        var retry = await service.RetryUncertainCreateAsync(request, itemId, projection, TestContext.Current.CancellationToken);

        Assert.Equal(ListingLifecycleResultKind.Succeeded, retry.Kind);
        Assert.Equal(2, products.ListShopProductsCalls);
        Assert.Equal(1, products.CreateCalls);
        Assert.Equal("product", retry.Mapping!.Identity!.ProductId);
    }

    [Fact]
    public async Task Uncertain_create_with_shop_match_does_not_create_and_returns_candidate_identity()
    {
        var storeId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var projection = Projection(storeId, itemId) with { ExternalBlueprintId = 68, ExternalProviderId = 23 };
        var repository = new MemoryRepository
        {
            Snapshot = WorkspaceSnapshot.Empty with
            {
                ExternalListingMappings = [UncertainCreateMapping(storeId, itemId, projection.Title, 68, 23)]
            }
        };
        var products = new FakeProducts
        {
            ShopProducts = [new ListingRemoteProductSummary("product-existing", projection.Title, 68, 23)]
        };
        var service = new ListingLifecycleService(repository, new FakeConnection(), products, new FakePublication());

        var result = await service.ReconcileUncertainCreateAsync(new ListingConnectionRequest(storeId, "shop-1", false), itemId, projection, TestContext.Current.CancellationToken);

        Assert.Equal(ListingLifecycleResultKind.Uncertain, result.Kind);
        Assert.Contains("product-existing", result.Message);
        Assert.Equal(0, products.CreateCalls);
    }

    [Fact]
    public async Task Verified_shop_product_can_be_explicitly_linked_to_uncertain_create()
    {
        var storeId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var store = new Store(storeId, "Printify Store", null, false, now, now, "{}", null, FulfillmentStrategy.Printify);
        var item = new Item(itemId, storeId, null, null, "Dad Joke Loading", null, ItemStatus.Draft, WorkflowStage.Listing, false, now, now, "{}");
        var repository = new MemoryRepository
        {
            Snapshot = WorkspaceSnapshot.Empty with
            {
                Stores = [store],
                Items = [item],
                ExternalListingMappings = [UncertainCreateMapping(storeId, itemId, "Dad Joke Loading", 68, 23)]
            }
        };
        var products = new FakeProducts
        {
            RemoteProduct = new ListingRemoteProduct("product-existing", false, ListingPublicationState.Unpublished,
                ListingSnapshot.Empty, ShopId: "shop-1", HasVerifiedIdentity: true)
        };
        var service = new ListingLifecycleService(repository, new FakeConnection(), products, new FakePublication());

        var result = await service.LinkExistingProductAsync(
            new ListingConnectionRequest(storeId, "shop-1", false),
            itemId,
            new ListingProductLinkCandidate("product-existing", "shop-1", "Dad Joke Loading"),
            TestContext.Current.CancellationToken);

        Assert.Equal(ListingLifecycleResultKind.Succeeded, result.Kind);
        Assert.Equal("product-existing", result.Mapping!.Identity!.ProductId);
        Assert.Equal(0, products.CreateCalls);
    }

    [Fact]
    public async Task Unavailable_connection_does_not_mutate_repository_or_remote()
    {
        var repository = new MemoryRepository();
        var products = new FakeProducts();
        var service = new ListingLifecycleService(
            repository,
            new FakeConnection
            {
                Readiness = ListingReadiness.Unavailable("offline", "Connection unavailable.")
            },
            products,
            new FakePublication());

        var result = await service.CreateOrUpdateAsync(
            new ListingConnectionRequest(Guid.NewGuid(), "shop-1", false),
            Projection(Guid.NewGuid(), Guid.NewGuid()),
            TestContext.Current.CancellationToken);

        Assert.Equal(ListingLifecycleResultKind.Unavailable, result.Kind);
        Assert.Empty(repository.Snapshot.ExternalListingMappings);
        Assert.Equal(0, products.CreateCalls);
    }

    [Fact]
    public async Task Uncertain_mapping_cannot_be_deleted_without_reconciliation()
    {
        var storeId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var repository = new MemoryRepository
        {
            Snapshot = WorkspaceSnapshot.Empty with
            {
                ExternalListingMappings =
                [
                    new ExternalListingMapping(
                        storeId,
                        itemId,
                        "printify",
                        "shop-1",
                        "product-1",
                        null,
                        null,
                        ExternalListingSyncState.Uncertain,
                        ExternalListingPublicationState.Unpublished,
                        ExternalListingOperationState.NeedsReconciliation,
                        null,
                        "timeout",
                        null,
                        DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow)
                ]
            }
        };
        var products = new FakeProducts();
        var service = new ListingLifecycleService(repository, new FakeConnection(), products, new FakePublication());

        var result = await service.DeleteRemoteAsync(
            new ListingConnectionRequest(storeId, "shop-1", false),
            itemId,
            TestContext.Current.CancellationToken);

        Assert.Equal(ListingLifecycleResultKind.Uncertain, result.Kind);
        Assert.Equal(0, products.DeleteCalls);
    }

    [Fact]
    public async Task Publish_requires_verified_remote_publication_state()
    {
        var storeId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var repository = new MemoryRepository
        {
            Snapshot = WorkspaceSnapshot.Empty with
            {
                ExternalListingMappings =
                [
                    new ExternalListingMapping(
                        storeId,
                        itemId,
                        "printify",
                        "shop-1",
                        "product-1",
                        null,
                        null,
                        ExternalListingSyncState.Synchronized,
                        ExternalListingPublicationState.Unpublished,
                        ExternalListingOperationState.Succeeded,
                        "{}",
                        null,
                        now,
                        now,
                        now)
                ]
            }
        };
        var products = new FakeProducts
        {
            RemoteProduct = new ListingRemoteProduct("product-1", false, ListingPublicationState.Published, ListingSnapshot.Empty)
        };
        var service = new ListingLifecycleService(repository, new FakeConnection(), products, new FakePublication());

        var result = await service.PublishAsync(
            new ListingConnectionRequest(storeId, "shop-1", true),
            itemId,
            TestContext.Current.CancellationToken);

        Assert.Equal(ListingLifecycleResultKind.Succeeded, result.Kind);
        Assert.Equal(ExternalListingPublicationState.Published, repository.Snapshot.ExternalListingMappings.Single().PublicationState);
        Assert.Equal(ExternalListingSyncState.Synchronized, repository.Snapshot.ExternalListingMappings.Single().SynchronizationState);
    }

    [Fact]
    public async Task Refresh_marks_remote_changes_as_conflict_and_accept_remote_persists_integration_values()
    {
        var storeId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var repository = new MemoryRepository
        {
            Snapshot = WorkspaceSnapshot.Empty with
            {
                ExternalListingMappings = [Mapping(storeId, itemId, "{\"title\":\"Old title\",\"description\":\"Old description\",\"shippingProfile\":\"Default\",\"variants\":\"101:30.00\"}")]
            }
        };
        var products = new FakeProducts
        {
            RemoteProduct = new ListingRemoteProduct(
                "product-1",
                false,
                ListingPublicationState.Unpublished,
                new ListingSnapshot(new Dictionary<string, string?>
                {
                    ["title"] = "Remote title",
                    ["description"] = "Remote description",
                    ["shippingProfile"] = "Priority",
                    ["variants"] = "101:35.00"
                }))
        };
        var service = new ListingLifecycleService(repository, new FakeConnection(), products, new FakePublication());
        var request = new ListingConnectionRequest(storeId, "shop-1", false);

        var refresh = await service.RefreshAsync(request, itemId, TestContext.Current.CancellationToken);

        Assert.Equal(ListingLifecycleResultKind.Conflict, refresh.Kind);
        var reconcile = await service.ReconcileAsync(request, Projection(storeId, itemId), ListingConflictResolution.AcceptRemote, TestContext.Current.CancellationToken);

        Assert.Equal(ListingLifecycleResultKind.Succeeded, reconcile.Kind);
        var mapping = repository.Snapshot.ExternalListingMappings.Single();
        Assert.Contains("Remote title", mapping.IntegrationValuesJson);
        Assert.Contains("35", mapping.IntegrationValuesJson);
        Assert.Equal(ExternalListingSyncState.Synchronized, mapping.SynchronizationState);
    }

    [Fact]
    public async Task Keep_local_reconciliation_updates_remote_without_changing_design_owned_projection()
    {
        var storeId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var repository = new MemoryRepository
        {
            Snapshot = WorkspaceSnapshot.Empty with
            {
                ExternalListingMappings = [Mapping(storeId, itemId, "{\"title\":\"Old title\"}")]
            }
        };
        var products = new FakeProducts
        {
            RemoteProduct = new ListingRemoteProduct("product-1", false, ListingPublicationState.Unpublished, Snapshot("Remote title")),
            UpdateResult = new ListingMutationResult("product-1", true)
        };
        var service = new ListingLifecycleService(repository, new FakeConnection(), products, new FakePublication());

        var result = await service.ReconcileAsync(
            new ListingConnectionRequest(storeId, "shop-1", false),
            Projection(storeId, itemId),
            ListingConflictResolution.KeepLocal,
            TestContext.Current.CancellationToken);

        Assert.Equal(ListingLifecycleResultKind.Succeeded, result.Kind);
        Assert.Equal("Dad Joke Loading… – T-shirt", products.LastProjection!.Title);
        Assert.Contains("title", products.LastChangedFields!);
        Assert.Contains("variants", products.LastChangedFields!);
        Assert.Null(repository.Snapshot.ExternalListingMappings.Single().IntegrationValuesJson);
        Assert.Equal(1, products.UpdateCalls);
    }

    [Fact]
    public async Task Publication_readiness_blocks_publish_and_unpublish_for_disconnected_shop()
    {
        var connection = new FakeConnection
        {
            Readiness = new ListingReadiness(ListingConnectionState.Ready, true, false,
                [new ListingReadinessIssue("printify-sales-channel-unavailable", "Connect a supported channel.")])
        };
        var service = new ListingLifecycleService(new MemoryRepository(), connection, new FakeProducts(), new FakePublication());
        var request = new ListingConnectionRequest(Guid.NewGuid(), "shop-1", true);

        var publish = await service.PublishAsync(request, Guid.NewGuid(), TestContext.Current.CancellationToken);
        var unpublish = await service.UnpublishAsync(request, Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(ListingLifecycleResultKind.Unavailable, publish.Kind);
        Assert.Equal(ListingLifecycleResultKind.Unavailable, unpublish.Kind);
        Assert.Contains("channel", publish.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Delete_exception_becomes_uncertain_and_does_not_clear_identity()
    {
        var storeId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var repository = new MemoryRepository
        {
            Snapshot = WorkspaceSnapshot.Empty with
            {
                ExternalListingMappings = [Mapping(storeId, itemId, "{}")]
            }
        };
        var products = new FakeProducts
        {
            RemoteProduct = new ListingRemoteProduct("product-1", false, ListingPublicationState.Unpublished, ListingSnapshot.Empty),
            DeleteException = new TimeoutException("request timed out")
        };
        var service = new ListingLifecycleService(repository, new FakeConnection(), products, new FakePublication());

        var result = await service.DeleteRemoteAsync(
            new ListingConnectionRequest(storeId, "shop-1", false),
            itemId,
            TestContext.Current.CancellationToken);

        Assert.Equal(ListingLifecycleResultKind.Uncertain, result.Kind);
        Assert.Equal("product-1", repository.Snapshot.ExternalListingMappings.Single().ProductId);
        Assert.Equal(ExternalListingSyncState.Uncertain, repository.Snapshot.ExternalListingMappings.Single().SynchronizationState);
    }

    [Fact]
    public async Task Archive_is_local_only_and_preserves_remote_identity()
    {
        var storeId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var repository = new MemoryRepository
        {
            Snapshot = WorkspaceSnapshot.Empty with
            {
                ExternalListingMappings = [Mapping(storeId, itemId, "{}")]
            }
        };
        var products = new FakeProducts();
        var service = new ListingLifecycleService(repository, new FakeConnection(), products, new FakePublication());

        var result = await service.ArchiveLocallyAsync(storeId, itemId, TestContext.Current.CancellationToken);

        Assert.Equal(ListingLifecycleResultKind.Succeeded, result.Kind);
        Assert.Equal(ExternalListingSyncState.Archived, repository.Snapshot.ExternalListingMappings.Single().SynchronizationState);
        Assert.Equal("product-1", repository.Snapshot.ExternalListingMappings.Single().ProductId);
        Assert.Equal(0, products.DeleteCalls);
    }

    [Fact]
    public async Task Verify_and_confirm_link_persist_only_verified_product()
    {
        var storeId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var store = new Store(storeId, "Store", null, false, now, now, "{}", fulfillmentStrategy: FulfillmentStrategy.Printify);
        var item = new Item(itemId, storeId, null, null, "Item", null, ItemStatus.Draft, WorkflowStage.Listing, false, now, now, "{}");
        var repository = new MemoryRepository
        {
            Snapshot = WorkspaceSnapshot.Empty with { Stores = [store], Items = [item] }
        };
        var products = new FakeProducts
        {
            RemoteProduct = new ListingRemoteProduct("product-9", false, ListingPublicationState.Unpublished, Snapshot("External title"), ShopId: "shop-1", HasVerifiedIdentity: true)
        };
        var service = new ListingLifecycleService(repository, new FakeConnection(), products, new FakePublication(), () => now);
        var request = new ListingConnectionRequest(storeId, "shop-1", false);

        var verified = await service.VerifyProductLinkAsync(request, itemId, " product-9 ", TestContext.Current.CancellationToken);

        Assert.True(verified.Succeeded);
        Assert.Equal("product-9", verified.Candidate!.ProductId);
        Assert.Equal("External title", verified.Candidate.Title);
        Assert.Empty(repository.Snapshot.ExternalListingMappings);

        var result = await service.LinkExistingProductAsync(request, itemId, verified.Candidate, TestContext.Current.CancellationToken);

        Assert.Equal(ListingLifecycleResultKind.Succeeded, result.Kind);
        var mapping = Assert.Single(repository.Snapshot.ExternalListingMappings);
        Assert.Equal("product-9", mapping.ProductId);
        Assert.Equal("shop-1", mapping.ShopId);
        Assert.Contains("External title", mapping.SnapshotJson);
        Assert.Equal(ExternalListingSyncState.Synchronized, mapping.SynchronizationState);
        Assert.Equal(ExternalListingOperationState.Succeeded, mapping.OperationState);
        Assert.Equal(3, products.GetCalls);
    }

    [Fact]
    public async Task Product_identity_mismatch_duplicate_and_uncertain_mapping_are_rejected_without_writes()
    {
        var storeId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var otherItemId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var store = new Store(storeId, "Store", null, false, now, now, "{}", fulfillmentStrategy: FulfillmentStrategy.Printify);
        var item = new Item(itemId, storeId, null, null, "Item", null, ItemStatus.Draft, WorkflowStage.Listing, false, now, now, "{}");
        var otherItem = item with { Id = otherItemId, Name = "Other Item" };
        var request = new ListingConnectionRequest(storeId, "shop-1", false);
        var repository = new MemoryRepository
        {
            Snapshot = WorkspaceSnapshot.Empty with { Stores = [store], Items = [item, otherItem] }
        };
        var products = new FakeProducts
        {
            RemoteProduct = new ListingRemoteProduct("product-9", false, ListingPublicationState.Unpublished, Snapshot("External title"), ShopId: "shop-2", HasVerifiedIdentity: true)
        };
        var service = new ListingLifecycleService(repository, new FakeConnection(), products, new FakePublication());

        var mismatch = await service.VerifyProductLinkAsync(request, itemId, "product-9", TestContext.Current.CancellationToken);
        Assert.False(mismatch.Succeeded);
        Assert.Empty(repository.Snapshot.ExternalListingMappings);

        products = new FakeProducts
        {
            RemoteProduct = new ListingRemoteProduct("product-9", false, ListingPublicationState.Unpublished, Snapshot("External title"), ShopId: "shop-1", HasVerifiedIdentity: true)
        };
        repository.Snapshot = repository.Snapshot with { ExternalListingMappings = [Mapping(storeId, otherItemId, "{}")] };
        service = new ListingLifecycleService(repository, new FakeConnection(), products, new FakePublication());
        var duplicate = await service.VerifyProductLinkAsync(request, itemId, "product-1", TestContext.Current.CancellationToken);
        Assert.False(duplicate.Succeeded);
        Assert.Single(repository.Snapshot.ExternalListingMappings);

        repository.Snapshot = repository.Snapshot with
        {
            ExternalListingMappings =
            [
                Mapping(storeId, itemId, "{}") with { SynchronizationState = ExternalListingSyncState.Uncertain, OperationState = ExternalListingOperationState.NeedsReconciliation },
                Mapping(storeId, otherItemId, "{}")
            ]
        };
        service = new ListingLifecycleService(repository, new FakeConnection(), products, new FakePublication());
        var uncertain = await service.VerifyProductLinkAsync(request, itemId, "product-9", TestContext.Current.CancellationToken);
        Assert.False(uncertain.Succeeded);
        Assert.Equal("product-1", repository.Snapshot.ExternalListingMappings.Single(value => value.ItemId == itemId).ProductId);
    }

    private static ExternalListingMapping Mapping(Guid storeId, Guid itemId, string snapshotJson) =>
        new(
            storeId,
            itemId,
            "printify",
            "shop-1",
            "product-1",
            null,
            null,
            ExternalListingSyncState.Synchronized,
            ExternalListingPublicationState.Unpublished,
            ExternalListingOperationState.Succeeded,
            snapshotJson,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

    private static ExternalListingMapping UncertainCreateMapping(Guid storeId, Guid itemId, string title, int blueprintId, int providerId) =>
        new(
            storeId,
            itemId,
            "printify",
            "shop-1",
            null,
            null,
            null,
            ExternalListingSyncState.Uncertain,
            ExternalListingPublicationState.NotApplicable,
            ExternalListingOperationState.NeedsReconciliation,
            System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string?>
            {
                ["createTitle"] = title,
                ["createBlueprintId"] = blueprintId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["createProviderId"] = providerId.ToString(System.Globalization.CultureInfo.InvariantCulture)
            }),
            "{\"kind\":\"Create\"}",
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

    private static ListingSnapshot Snapshot(string title) =>
        new(new Dictionary<string, string?> { ["title"] = title });

    private static ListingProductProjection Projection(Guid storeId, Guid itemId) =>
        new(
            itemId,
            storeId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Dad Joke Loading… – T-shirt",
            "A joke shirt.",
            "Default",
            "Hide out of stock",
            new ListingPricingInput(ListingPricingPolicy.FixedRetailPrice, 30m),
            [],
            []);

    private sealed class FakeConnection : IListingConnectionPort
    {
        public ListingReadiness Readiness { get; init; } = new(ListingConnectionState.Ready, true, true, []);

        public Task<ListingReadiness> CheckAsync(ListingConnectionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(Readiness);
    }

    private sealed class FakeProducts : IListingProductPort
    {
        public int CreateCalls { get; private set; }
        public int DeleteCalls { get; private set; }
        public int UpdateCalls { get; private set; }
        public int GetCalls { get; private set; }
        public ListingMutationResult CreateResult { get; init; } = new("product", true);
        public ListingMutationResult UpdateResult { get; init; } = new("product", true);
        public ListingRemoteProduct? RemoteProduct { get; init; }
        public Exception? DeleteException { get; init; }
        public ListingProductProjection? LastProjection { get; private set; }
        public IReadOnlySet<string>? LastChangedFields { get; private set; }
        public IReadOnlyList<ListingRemoteProductSummary> ShopProducts { get; set; } = [];
        public int ListShopProductsCalls { get; private set; }

        public Task<IReadOnlyList<ListingRemoteProductSummary>> ListShopProductsAsync(string shopId, CancellationToken cancellationToken = default)
        {
            ListShopProductsCalls++;
            return Task.FromResult(ShopProducts);
        }

        public Task<ListingRemoteProduct?> GetAsync(string shopId, string productId, CancellationToken cancellationToken = default)
        {
            GetCalls++;
            return Task.FromResult(RemoteProduct);
        }

        public Task<ListingMutationResult> CreateAsync(string shopId, ListingProductProjection projection, IReadOnlyDictionary<Guid, ListingImageReference> images, CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            return Task.FromResult(CreateResult);
        }

        public Task<ListingMutationResult> UpdateAsync(string shopId, string productId, ListingProductProjection projection, IReadOnlyDictionary<Guid, ListingImageReference> images, IReadOnlySet<string>? changedFields = null, CancellationToken cancellationToken = default)
        {
            UpdateCalls++;
            LastProjection = projection;
            LastChangedFields = changedFields;
            return Task.FromResult(UpdateResult);
        }

        public Task<ListingMutationResult> DeleteAsync(string shopId, string productId, CancellationToken cancellationToken = default)
        {
            DeleteCalls++;
            if (DeleteException is not null) return Task.FromException<ListingMutationResult>(DeleteException);
            return Task.FromResult(CreateResult);
        }
    }

    private sealed class FakePublication : IListingPublicationPort
    {
        public Task<ListingMutationResult> PublishAsync(string shopId, string productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ListingMutationResult(productId, true));

        public Task<ListingMutationResult> UnpublishAsync(string shopId, string productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ListingMutationResult(productId, true));
    }

    private sealed class MemoryRepository : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; set; } = WorkspaceSnapshot.Empty;

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            Snapshot = snapshot;
            return Task.CompletedTask;
        }
    }
}

