using FusionCanvas.Application.Listings;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Workspace;

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
        Assert.Null(repository.Snapshot.ExternalListingMappings.Single().IntegrationValuesJson);
        Assert.Equal(1, products.UpdateCalls);
    }

    [Fact]
    public async Task Standalone_printify_cannot_publish_or_unpublish()
    {
        var service = new ListingLifecycleService(new MemoryRepository(), new FakeConnection(), new FakeProducts(), new FakePublication());
        var request = new ListingConnectionRequest(Guid.NewGuid(), "shop-1", false);

        var publish = await service.PublishAsync(request, Guid.NewGuid(), TestContext.Current.CancellationToken);
        var unpublish = await service.UnpublishAsync(request, Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(ListingLifecycleResultKind.NotAllowed, publish.Kind);
        Assert.Equal(ListingLifecycleResultKind.NotAllowed, unpublish.Kind);
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
        public ListingMutationResult CreateResult { get; init; } = new("product", true);
        public ListingMutationResult UpdateResult { get; init; } = new("product", true);
        public ListingRemoteProduct? RemoteProduct { get; init; }
        public Exception? DeleteException { get; init; }
        public ListingProductProjection? LastProjection { get; private set; }

        public Task<ListingRemoteProduct?> GetAsync(string shopId, string productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(RemoteProduct);

        public Task<ListingMutationResult> CreateAsync(string shopId, ListingProductProjection projection, IReadOnlyDictionary<Guid, ListingImageReference> images, CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            return Task.FromResult(CreateResult);
        }

        public Task<ListingMutationResult> UpdateAsync(string shopId, string productId, ListingProductProjection projection, IReadOnlyDictionary<Guid, ListingImageReference> images, CancellationToken cancellationToken = default)
        {
            UpdateCalls++;
            LastProjection = projection;
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
