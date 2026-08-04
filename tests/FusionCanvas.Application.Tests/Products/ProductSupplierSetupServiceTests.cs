using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Products;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.Products;

namespace FusionCanvas.Application.Tests;

public class ProductSupplierSetupServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly Guid OtherStoreId = Guid.NewGuid();
    private static readonly Guid BlockStoreId = Guid.NewGuid();

    [Fact]
    public async Task CreateProductAndOffering_PersistsAcrossReload()
    {
        var repository = new InMemoryWorkspaceRepository(SeedSnap());
        var service = New(repository);

        var productResult = await service.CreateProductAsync(new CreateProductRequest(StoreId, "Gildan 64000"), TestContext.Current.CancellationToken);
        Assert.True(productResult.Succeeded);
        var productId = Assert.Single(productResult.State.Products).Id;

        var offeringResult = await service.CreateOfferingAsync(new CreateOfferingRequest(productId, "Printful", FulfillmentKind.FixedProvider, "Printful"), TestContext.Current.CancellationToken);
        Assert.True(offeringResult.Succeeded);

        var reloaded = await service.LoadForStoreAsync(StoreId, TestContext.Current.CancellationToken);
        var product = Assert.Single(reloaded.Products);
        Assert.Equal(productId, product.Id);
        Assert.Single(product.Offerings);
        Assert.Equal("Printful", product.Offerings[0].ProviderName);
        Assert.Equal(FulfillmentKind.FixedProvider, product.Offerings[0].Kind);
    }

    [Fact]
    public async Task Catalog_IsIsolatedByStore()
    {
        var repository = new InMemoryWorkspaceRepository(SeedSnap(storeId: StoreId));
        var service = New(repository);
        await service.CreateProductAsync(new CreateProductRequest(StoreId, "Gildan 64000"), TestContext.Current.CancellationToken);

        var state = await service.LoadForStoreAsync(OtherStoreId, TestContext.Current.CancellationToken);

        Assert.Empty(state.Products);
        var ownState = await service.LoadForStoreAsync(StoreId, TestContext.Current.CancellationToken);
        Assert.Single(ownState.Products);
    }

    [Fact]
    public async Task VariantSpecificArea_PersistsApplicableVariantsFromSameOffering()
    {
        var repository = new InMemoryWorkspaceRepository(SeedSnap());
        var service = New(repository);
        var (productId, offeringId) = await CreateProductOfferingAsync(service);
        var variantResult = await service.CreateVariantAsync(new CreateVariantRequest(offeringId, [new VariantOptionDraft("Color", "Black")]), TestContext.Current.CancellationToken);
        Assert.True(variantResult.Succeeded);
        var variantId = Assert.Single(variantResult.State.Products[0].Offerings[0].Variants).Id;

        var areaResult = await service.CreateDesignAreaAsync(new CreateDesignAreaRequest(offeringId, "Front", "front", "DTG", 3000, 4500, [variantId]), TestContext.Current.CancellationToken);
        Assert.True(areaResult.Succeeded);
        var area = Assert.Single(areaResult.State.Products[0].Offerings[0].DesignAreas);
        Assert.Equal([variantId], area.VariantIds);
        Assert.Equal(3000, area.Width);
        Assert.Equal(4500, area.Height);
    }

    [Fact]
    public async Task InvalidDimensions_AreRejectedAndDataUnchanged()
    {
        var repository = new InMemoryWorkspaceRepository(SeedSnap());
        var service = New(repository);
        var (_, offeringId) = await CreateProductOfferingAsync(service);

        var result = await service.CreateDesignAreaAsync(new CreateDesignAreaRequest(offeringId, "Front", "front", "DTG", 0, 4500, null), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("width", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.State.Products[0].Offerings[0].DesignAreas);
    }

    [Fact]
    public async Task CrossOfferingApplicableVariant_IsRejectedAndDataUnchanged()
    {
        var repository = new InMemoryWorkspaceRepository(SeedSnap());
        var service = New(repository);
        var (_, offeringId) = await CreateProductOfferingAsync(service);
        var foreignVariantId = Guid.NewGuid();
        repository.Snapshot = repository.Snapshot with
        {
            ProductVariants = [.. repository.Snapshot.ProductVariants, new ProductVariant(foreignVariantId, Guid.NewGuid(), [new VariantOption("Color", "Black")], Now, Now)]
        };

        var result = await service.CreateDesignAreaAsync(new CreateDesignAreaRequest(offeringId, "Front", "front", "DTG", 3000, 4500, [foreignVariantId]), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("own offering", result.Error);
        Assert.Empty(result.State.Products[0].Offerings[0].DesignAreas);
    }

    [Fact]
    public async Task ChoiceOffering_IdentifiedAsNetworkWithoutProvider()
    {
        var repository = new InMemoryWorkspaceRepository(SeedSnap());
        var service = New(repository);
        var (productId, _) = await CreateProductOfferingAsync(service);

        var result = await service.CreateOfferingAsync(new CreateOfferingRequest(productId, "Choice", FulfillmentKind.PrintifyChoiceNetwork), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var offering = result.State.Products[0].Offerings.Single(o => o.Kind == FulfillmentKind.PrintifyChoiceNetwork);
        Assert.Equal(FulfillmentKind.PrintifyChoiceNetwork, offering.Kind);
        Assert.Null(offering.ProviderName);
    }

    [Fact]
    public async Task ChoiceOffering_RejectsProviderName()
    {
        var repository = new InMemoryWorkspaceRepository(SeedSnap());
        var service = New(repository);
        var (productId, _) = await CreateProductOfferingAsync(service);

        var result = await service.CreateOfferingAsync(new CreateOfferingRequest(productId, "Bad Choice", FulfillmentKind.PrintifyChoiceNetwork, "Printful"), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("must not specify a fixed provider", result.Error);
    }

    [Fact]
    public async Task RemoveUnreferencedArea_PreservesOtherCatalogAndItems()
    {
        var repository = new InMemoryWorkspaceRepository(SeedSnap());
        var service = New(repository);
        var (productId, offeringId) = await CreateProductOfferingAsync(service);
        var keep = await service.CreateDesignAreaAsync(new CreateDesignAreaRequest(offeringId, "Front", "front", "DTG", 3000, 4500, null), TestContext.Current.CancellationToken);
        var keepId = Assert.Single(keep.State.Products[0].Offerings[0].DesignAreas).Id;
        var removeArea = await service.CreateDesignAreaAsync(new CreateDesignAreaRequest(offeringId, "Back", "back", "DTG", 3000, 4500, null), TestContext.Current.CancellationToken);
        var removeId = removeArea.State.Products[0].Offerings[0].DesignAreas.Single(a => a.Id != keepId).Id;

        var result = await service.DeleteDesignAreaAsync(new DeleteDesignAreaRequest(removeId, Confirm: true), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var reloaded = await service.LoadForStoreAsync(StoreId, TestContext.Current.CancellationToken);
        var areas = Assert.Single(Assert.Single(reloaded.Products).Offerings).DesignAreas;
        Assert.Single(areas);
        Assert.Equal(keepId, areas[0].Id);
    }

    [Fact]
    public async Task RemoveReferencedArea_IsBlocked()
    {
        var repository = new InMemoryWorkspaceRepository(SeedSnap());
        var service = New(repository);
        var (_, offeringId) = await CreateProductOfferingAsync(service);
        var areaResult = await service.CreateDesignAreaAsync(new CreateDesignAreaRequest(offeringId, "Front", "front", "DTG", 3000, 4500, null), TestContext.Current.CancellationToken);
        var areaId = Assert.Single(areaResult.State.Products[0].Offerings[0].DesignAreas).Id;

        var itemId = Guid.NewGuid();
        var item = new Item(itemId, StoreId, null, null, "Tee", null, ItemStatus.Draft, WorkflowStage.Design, false, Now, Now, "{}");
        repository.Snapshot = repository.Snapshot with
        {
            Items = [.. repository.Snapshot.Items, item],
            ItemDesignAreaTargets = [.. repository.Snapshot.ItemDesignAreaTargets, new ItemDesignAreaTarget(itemId, areaId)]
        };

        var result = await service.DeleteDesignAreaAsync(new DeleteDesignAreaRequest(areaId, Confirm: true), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("selected", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Single(await service.LoadForStoreAsync(StoreId, TestContext.Current.CancellationToken).ContinueWith(t => t.Result.Products[0].Offerings[0].DesignAreas));
    }

    [Fact]
    public async Task RemoveReferencedProduct_IsBlocked()
    {
        var repository = new InMemoryWorkspaceRepository(SeedSnap());
        var service = New(repository);
        var (productId, offeringId) = await CreateProductOfferingAsync(service);
        var areaResult = await service.CreateDesignAreaAsync(new CreateDesignAreaRequest(offeringId, "Front", "front", "DTG", 3000, 4500, null), TestContext.Current.CancellationToken);
        var areaId = Assert.Single(areaResult.State.Products[0].Offerings[0].DesignAreas).Id;
        var itemId = Guid.NewGuid();
        repository.Snapshot = repository.Snapshot with
        {
            Items = [.. repository.Snapshot.Items, new Item(itemId, StoreId, null, null, "Tee", null, ItemStatus.Draft, WorkflowStage.Design, false, Now, Now, "{}")],
            ItemDesignAreaTargets = [new ItemDesignAreaTarget(itemId, areaId)]
        };

        var result = await service.DeleteProductAsync(new DeleteProductRequest(productId, Confirm: true), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Single(await service.LoadForStoreAsync(StoreId, TestContext.Current.CancellationToken).ContinueWith(t => t.Result.Products));
    }

    [Fact]
    public async Task Requirement_deletionRequiresConfirmation()
    {
        var repository = new InMemoryWorkspaceRepository(SeedSnap());
        var service = New(repository);
        var (productId, _) = await CreateProductOfferingAsync(service);
        var second = await service.CreateProductAsync(new CreateProductRequest(StoreId, "Second"), TestContext.Current.CancellationToken);
        var secondId = second.State.Products.Single(p => p.Id != productId).Id;

        var result = await service.DeleteProductAsync(new DeleteProductRequest(secondId, Confirm: false), TestContext.Current.CancellationToken);
        Assert.False(result.Succeeded);
        Assert.Contains("confirmation", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ArchivedStore_IsReadOnlyForCatalogMutation()
    {
        var repository = new InMemoryWorkspaceRepository(SeedSnap(storeArchived: true));
        var service = New(repository);

        var result = await service.CreateProductAsync(new CreateProductRequest(StoreId, "Gildan"), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("read-only", result.Error, StringComparison.OrdinalIgnoreCase);
        var state = await service.LoadForStoreAsync(StoreId, TestContext.Current.CancellationToken);
        Assert.True(state.IsReadOnly);
        Assert.Empty(state.Products);
    }

    [Fact]
    public async Task EmptyStore_ReportsNeedsFirstProduct()
    {
        var repository = new InMemoryWorkspaceRepository(SeedSnap(storeId: StoreId));
        var service = New(repository);

        var state = await service.LoadForStoreAsync(StoreId, TestContext.Current.CancellationToken);

        Assert.True(state.NeedsFirstProduct);
        Assert.Empty(state.Products);
    }

    [Fact]
    public async Task MultipleCompatibleAreas_ArePersistedAtomicallyAndShownAfterReload()
    {
        var repository = new InMemoryWorkspaceRepository(SeedSnap());
        var service = New(repository);
        var (_, offeringId) = await CreateProductOfferingAsync(service);
        var a = (await service.CreateDesignAreaAsync(new CreateDesignAreaRequest(offeringId, "Front", "front", "DTG", 3000, 4500, null), TestContext.Current.CancellationToken)).State.Products[0].Offerings[0].DesignAreas[0];
        var b = (await service.CreateDesignAreaAsync(new CreateDesignAreaRequest(offeringId, "Back", "back", "DTG", 3000, 4500, null), TestContext.Current.CancellationToken)).State.Products[0].Offerings[0].DesignAreas[0];
        var itemId = Guid.NewGuid();
        repository.Snapshot = repository.Snapshot with
        {
            Items = [.. repository.Snapshot.Items, new Item(itemId, StoreId, null, null, "Tee", null, ItemStatus.Draft, WorkflowStage.Design, false, Now, Now, "{}")]
        };

        var result = await service.ReplaceDesignTargetsAsync(new ReplaceDesignTargetsRequest(itemId, [a.Id, b.Id]), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.All(result.State.Options.Where(o => o.IsSelected), selected => Assert.Contains(selected.DesignAreaId, new[] { a.Id, b.Id }));
        Assert.Equal(2, result.State.Options.Count(o => o.IsSelected));

        var reloaded = await service.LoadDesignTargetsAsync(itemId, TestContext.Current.CancellationToken);
        Assert.Equal(2, reloaded.Options.Count(o => o.IsSelected));
        Assert.All(reloaded.Options.Where(o => o.IsSelected), selected => Assert.Contains(selected.DesignAreaId, new[] { a.Id, b.Id }));
    }

    [Fact]
    public async Task CrossStoreTargetSelection_IsRejectedAndPreservesPriorTargets()
    {
        var repository = new InMemoryWorkspaceRepository(SeedSnap());
        var service = New(repository);
        repository.Snapshot = repository.Snapshot with
        {
            Stores = [.. repository.Snapshot.Stores, new Store(OtherStoreId, name: "Other Studio", null, false, Now, Now, "{}")]
        };
        var (_, offeringId) = await CreateProductOfferingAsync(service);
        var area = (await service.CreateDesignAreaAsync(new CreateDesignAreaRequest(offeringId, "Front", "front", "DTG", 3000, 4500, null), TestContext.Current.CancellationToken)).State.Products[0].Offerings[0].DesignAreas[0];

        var other = await service.CreateProductAsync(new CreateProductRequest(OtherStoreId, "Other"), TestContext.Current.CancellationToken);
        Assert.True(other.Succeeded, other.Error);
        var otherOffering = await service.CreateOfferingAsync(new CreateOfferingRequest(other.State.Products[0].Id, "O", FulfillmentKind.FixedProvider, "P"), TestContext.Current.CancellationToken);
        var otherArea = (await service.CreateDesignAreaAsync(new CreateDesignAreaRequest(otherOffering.State.Products[0].Offerings[0].Id, "X", "x", "DTG", 1000, 1000, null), TestContext.Current.CancellationToken)).State.Products[0].Offerings[0].DesignAreas[0];

        var itemId = Guid.NewGuid();
        repository.Snapshot = repository.Snapshot with
        {
            Items = [.. repository.Snapshot.Items, new Item(itemId, StoreId, null, null, "Tee", null, ItemStatus.Draft, WorkflowStage.Design, false, Now, Now, "{}")],
            ItemDesignAreaTargets = [new ItemDesignAreaTarget(itemId, area.Id)]
        };

        var result = await service.ReplaceDesignTargetsAsync(new ReplaceDesignTargetsRequest(itemId, [area.Id, otherArea.Id]), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("another Store", result.Error);
        var reloaded = await service.LoadDesignTargetsAsync(itemId, TestContext.Current.CancellationToken);
        Assert.Single(reloaded.Options.Where(o => o.IsSelected));
        Assert.Equal(area.Id, reloaded.Options.Single(o => o.IsSelected).DesignAreaId);
    }

    [Fact]
    public async Task ProtectedItem_RejectsTargetMutation()
    {
        var repository = new InMemoryWorkspaceRepository(SeedSnap());
        var service = New(repository);
        var (_, offeringId) = await CreateProductOfferingAsync(service);
        var area = (await service.CreateDesignAreaAsync(new CreateDesignAreaRequest(offeringId, "Front", "front", "DTG", 3000, 4500, null), TestContext.Current.CancellationToken)).State.Products[0].Offerings[0].DesignAreas[0];
        var itemId = Guid.NewGuid();
        repository.Snapshot = repository.Snapshot with
        {
            Items = [.. repository.Snapshot.Items, new Item(itemId, StoreId, null, null, "Tee", null, ItemStatus.Published, WorkflowStage.Design, false, Now, Now, "{}")]
        };

        var result = await service.ReplaceDesignTargetsAsync(new ReplaceDesignTargetsRequest(itemId, [area.Id]), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.False((await service.LoadDesignTargetsAsync(itemId, TestContext.Current.CancellationToken)).Options.Any(o => o.IsSelected));
    }

    [Fact]
    public async Task UpdateProductAsync_PersistsChangesAndPreservesId()
    {
        var repository = new InMemoryWorkspaceRepository(SeedSnap());
        var service = New(repository);
        var (productId, _) = await CreateProductOfferingAsync(service);

        var result = await service.UpdateProductAsync(new UpdateProductRequest(productId, "Bella Canvas 3001", "Updated", "ext-new"), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var product = Assert.Single(result.State.Products);
        Assert.Equal(productId, product.Id);
        Assert.Equal("Bella Canvas 3001", product.Name);
        Assert.Equal("Updated", product.Description);
        Assert.Equal("ext-new", product.ExternalProductId);
    }

    [Fact]
    public async Task UpdateOfferingAsync_PersistsChangesAndPreservesId()
    {
        var repository = new InMemoryWorkspaceRepository(SeedSnap());
        var service = New(repository);
        var (_, offeringId) = await CreateProductOfferingAsync(service);

        var result = await service.UpdateOfferingAsync(new UpdateOfferingRequest(offeringId, "Printify", FulfillmentKind.FixedProvider, "Printify"), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var offering = result.State.Products[0].Offerings.Single(o => o.Id == offeringId);
        Assert.Equal(offeringId, offering.Id);
        Assert.Equal("Printify", offering.Name);
        Assert.Equal("Printify", offering.ProviderName);
    }

    [Fact]
    public async Task UpdateVariantAsync_PersistsChangedOptions()
    {
        var repository = new InMemoryWorkspaceRepository(SeedSnap());
        var service = New(repository);
        var (_, offeringId) = await CreateProductOfferingAsync(service);
        var variantResult = await service.CreateVariantAsync(new CreateVariantRequest(offeringId, [new VariantOptionDraft("Color", "Black")]), TestContext.Current.CancellationToken);
        var variantId = Assert.Single(variantResult.State.Products[0].Offerings[0].Variants).Id;

        var result = await service.UpdateVariantAsync(new UpdateVariantRequest(variantId, [new VariantOptionDraft("Color", "White"), new VariantOptionDraft("Size", "L")]), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var variant = Assert.Single(result.State.Products[0].Offerings[0].Variants);
        Assert.Equal([new VariantOption("Color", "White"), new VariantOption("Size", "L")], variant.Options);
    }

    private static ProductSupplierSetupService New(InMemoryWorkspaceRepository repository) =>
        new(repository, () => Now, Guid.NewGuid);

    private static async Task<(Guid productId, Guid offeringId)> CreateProductOfferingAsync(ProductSupplierSetupService service)
    {
        var productResult = await service.CreateProductAsync(new CreateProductRequest(StoreId, "Gildan 64000"), TestContext.Current.CancellationToken);
        var productId = productResult.State.Products[0].Id;
        var offeringResult = await service.CreateOfferingAsync(new CreateOfferingRequest(productId, "Printful", FulfillmentKind.FixedProvider, "Printful"), TestContext.Current.CancellationToken);
        return (productId, offeringResult.State.Products[0].Offerings[0].Id);
    }

    private static WorkspaceSnapshot SeedSnap(Guid? storeId = null, bool storeArchived = false)
    {
        var id = storeId ?? StoreId;
        return new WorkspaceSnapshot(
            [WorkspaceSnapshot.DefaultWorkspace(Now)],
            [new Store(id, name: "North Star Studio", null, storeArchived, Now, Now, "{}")],
            [], [], [], [], [], [], [], [])
        {
            IdeationRejections = []
        };
    }

    private sealed class InMemoryWorkspaceRepository(WorkspaceSnapshot? snapshot = null) : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = snapshot ?? WorkspaceSnapshot.Empty;

        public WorkspaceSnapshot Snapshot
        {
            get => _snapshot;
            set => _snapshot = value;
        }

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            _snapshot = snapshot;
            return Task.CompletedTask;
        }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_snapshot);
    }
}
