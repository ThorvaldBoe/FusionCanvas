using Avalonia.Headless.XUnit;
using FusionCanvas.App.StageTools;
using FusionCanvas.Application.Mockups;
using FusionCanvas.Domain.Mockups;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.Listings;
using FusionCanvas.Application.Stores;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.App.Stores;
using FusionCanvas.App.Tests.TestSupport;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;

namespace FusionCanvas.App.Tests;

public class StageToolViewModelsTests
{
    [Fact]
    public async Task PrintifyTool_FailsClosedWhenItemStoreIsUnavailable()
    {
        var vm = new PrintifyListingStageToolViewModel(
            serviceFactory: null,
            projectionSource: null,
            snapshot: () => WorkspaceSnapshot.Empty,
            storeResolver: _ => null,
            storeContextMapper: new UnavailableStoreContextMapper());

        await vm.LoadAsync(Guid.NewGuid(), canEdit: true, TestContext.Current.CancellationToken);

        Assert.True(vm.HasError);
        Assert.Contains("Store is unavailable", vm.ErrorMessage);
        Assert.Equal("Unavailable", vm.ConnectionStatus);
        Assert.False(vm.CanCreateOrUpdate);
        Assert.False(vm.CanPublish);
    }

    [Fact]
    public async Task PrintifyTool_OffersVariantSetupForLinkedItemBeforeLifecycleProjectionIsReady()
    {
        var now = DateTimeOffset.UtcNow;
        var storeId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var store = new Store(storeId, "Store", null, false, now, now, "{}", null, FulfillmentStrategy.Printify);
        var item = new Item(itemId, storeId, null, null, "Imported listing", "Description", ItemStatus.Published, WorkflowStage.Listing, false, now, now, "{}");
        var mapping = new ExternalListingMapping(storeId, itemId, "printify", "42", "product-1", null, null,
            ExternalListingSyncState.Synchronized, ExternalListingPublicationState.Published, ExternalListingOperationState.Succeeded, null, null, now, now, now);
        var snapshot = WorkspaceSnapshot.Empty with { Stores = [store], Items = [item], ExternalListingMappings = [mapping] };
        var variantSetupService = new FusionCanvas.Application.Stores.Printify.PrintifyListingImportService(null!, null!, null!, null!, null!);
        var vm = new PrintifyListingStageToolViewModel(null, null, () => snapshot, id => id == storeId ? store : null,
            new ContextMapper(), variantSetupService);

        await vm.LoadAsync(itemId, canEdit: true, TestContext.Current.CancellationToken);

        Assert.True(vm.HasError);
        Assert.True(vm.CanDownloadVariantSetup);
        Assert.True(vm.DownloadVariantSetupCommand.CanExecute(null));
    }

    [AvaloniaFact]
    public async Task PrintifyTool_RequiresConfirmationBeforeRemoteDeletionAndSupportsCancellation()
    {
        var now = DateTimeOffset.UtcNow;
        var storeId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var store = new Store(storeId, "The Groan Zone", null, false, now, now, "{}", null, FulfillmentStrategy.Printify);
        var item = new Item(itemId, storeId, null, null, "Dad joke", null, ItemStatus.Draft, WorkflowStage.Listing, false, now, now, "{}");
        var mapping = new ExternalListingMapping(
            storeId,
            itemId,
            "printify",
            "42",
            "product-1",
            null,
            null,
            ExternalListingSyncState.Synchronized,
            ExternalListingPublicationState.NotApplicable,
            ExternalListingOperationState.Succeeded,
            "{}",
            null,
            now,
            now,
            now);
        var snapshot = WorkspaceSnapshot.Empty with
        {
            Stores = [store],
            Items = [item],
            ExternalListingMappings = [mapping]
        };
        var repository = new MemoryRepository(snapshot);
        var products = new Products
        {
            RemoteProduct = new ListingRemoteProduct("product-1", false, ListingPublicationState.Unpublished, ListingSnapshot.Empty)
        };
        var service = new ListingLifecycleService(repository, new Connection(), products, new Publication());
        var vm = new PrintifyListingStageToolViewModel(
            new Factory(service),
            new ProjectionSource(),
            () => repository.Snapshot,
            id => id == storeId ? store : null,
            new ContextMapper());

        await vm.LoadAsync(itemId, canEdit: true, TestContext.Current.CancellationToken);
        Assert.True(vm.CanDelete);

        vm.DeleteCommand.Execute(null);
        Assert.True(vm.IsDeleteConfirmationVisible);
        Assert.Equal(0, products.DeleteCalls);
        vm.CancelDeleteCommand.Execute(null);
        Assert.False(vm.IsDeleteConfirmationVisible);

        vm.DeleteCommand.Execute(null);
        vm.ConfirmDeleteCommand.Execute(null);
        await HeadlessUiWait.UntilAsync(() => !vm.IsBusy, "Printify deletion confirmation");

        Assert.Equal(1, products.DeleteCalls);
        Assert.False(vm.IsDeleteConfirmationVisible);
    }

    [AvaloniaFact]
    public async Task PrintifyTool_HeadlessJourneyCreatesRefreshesReviewsConflictAndKeepsLocal()
    {
        var now = DateTimeOffset.UtcNow;
        var storeId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var store = new Store(storeId, "The Groan Zone", null, false, now, now, "{}", null, FulfillmentStrategy.Printify);
        var item = new Item(itemId, storeId, null, null, "Dad joke", null, ItemStatus.Draft, WorkflowStage.Listing, false, now, now, "{}");
        var snapshot = WorkspaceSnapshot.Empty with { Stores = [store], Items = [item] };
        var repository = new MemoryRepository(snapshot);
        var products = new Products();
        var service = new ListingLifecycleService(repository, new Connection(), products, new Publication());
        var vm = new PrintifyListingStageToolViewModel(
            new Factory(service),
            new ProjectionSource(),
            () => repository.Snapshot,
            id => id == storeId ? store : null,
            new ContextMapper());

        await vm.LoadAsync(itemId, canEdit: true, TestContext.Current.CancellationToken);
        Assert.True(vm.CanCreateOrUpdate);
        vm.CreateOrUpdateCommand.Execute(null);
        await HeadlessUiWait.UntilAsync(() => !vm.IsBusy, "Printify create");
        Assert.Equal(1, products.CreateCalls);
        Assert.True(vm.CanRefresh);

        products.RemoteProduct = new ListingRemoteProduct(
            "product-1",
            false,
            ListingPublicationState.Unpublished,
            new ListingSnapshot(new Dictionary<string, string?> { ["title"] = "Remote title" }));
        vm.RefreshCommand.Execute(null);
        await HeadlessUiWait.UntilAsync(() => !vm.IsBusy, "Printify refresh conflict");
        Assert.True(vm.HasConflict);

        vm.KeepLocalCommand.Execute(null);
        await HeadlessUiWait.UntilAsync(() => !vm.IsBusy, "Printify keep-local update");
        Assert.False(vm.HasConflict);
        Assert.Equal(1, products.UpdateCalls);
    }

    [AvaloniaFact]
    public async Task PrintifyTool_HeadlessShopifyJourneyPublishesUnpublishesAndReopensDeletionGate()
    {
        var now = DateTimeOffset.UtcNow;
        var storeId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var store = new Store(storeId, "The Groan Zone", null, false, now, now, "{}", null, FulfillmentStrategy.ShopifyPrintify);
        var item = new Item(itemId, storeId, null, null, "Dad joke", null, ItemStatus.Draft, WorkflowStage.Listing, false, now, now, "{}");
        var mapping = new ExternalListingMapping(
            storeId,
            itemId,
            "printify",
            "42",
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
            now);
        var repository = new MemoryRepository(WorkspaceSnapshot.Empty with
        {
            Stores = [store],
            Items = [item],
            ExternalListingMappings = [mapping]
        });
        var products = new Products
        {
            RemoteProduct = new ListingRemoteProduct("product-1", false, ListingPublicationState.Unpublished, ListingSnapshot.Empty)
        };
        var publication = new Publication();
        var service = new ListingLifecycleService(repository, new Connection(), products, publication);
        var vm = new PrintifyListingStageToolViewModel(
            new Factory(service),
            new ProjectionSource(),
            () => repository.Snapshot,
            id => id == storeId ? store : null,
            new ContextMapper());

        await vm.LoadAsync(itemId, canEdit: true, TestContext.Current.CancellationToken);
        Assert.True(vm.CanPublish);
        products.RemoteProduct = new ListingRemoteProduct("product-1", false, ListingPublicationState.Published, ListingSnapshot.Empty);
        vm.PublishCommand.Execute(null);
        await HeadlessUiWait.UntilAsync(() => !vm.IsBusy, "Printify publish");
        Assert.Equal(1, publication.PublishCalls);
        Assert.True(vm.CanUnpublish);

        products.RemoteProduct = new ListingRemoteProduct("product-1", false, ListingPublicationState.Unpublished, ListingSnapshot.Empty);
        vm.UnpublishCommand.Execute(null);
        await HeadlessUiWait.UntilAsync(() => !vm.IsBusy, "Printify unpublish");
        Assert.Equal(1, publication.UnpublishCalls);
        Assert.True(vm.CanPublish);
        Assert.True(vm.CanDelete);
    }

    [Fact]
    public void IdeaTool_LoadsIdeaAndAppliesReadOnlyReason()
    {
        var vm = new IdeaStageToolViewModel();

        vm.LoadFromMetadata(new ItemInspectorCreativeFields(
            Idea: "original idea",
            Audience: null,
            ConceptIdea: null,
            Phrase: null,
            GraphicDirection: null), canEdit: false);

        Assert.Equal("original idea", vm.Idea);
        Assert.True(vm.IsReadOnly);
        Assert.NotEmpty(vm.ReadOnlyReason);
    }

    [Fact]
    public void IdeaTool_ToStagePayload_CarriesIdeaOnly()
    {
        var vm = new IdeaStageToolViewModel { Idea = "new idea" };

        var payload = vm.ToStagePayload();

        Assert.Equal(WorkflowStage.Idea, payload.Stage);
        Assert.Equal("new idea", payload.Idea);
        Assert.Null(payload.ConceptIdea);
        Assert.Null(payload.Phrase);
        Assert.Null(payload.GraphicDirection);
    }

    [Fact]
    public void ConceptTool_LoadsPhraseAndGraphicDirection()
    {
        var vm = new ConceptStageToolViewModel();

        vm.LoadFromMetadata(new ItemInspectorCreativeFields(
            Idea: null,
            Audience: null,
            ConceptIdea: "concept",
            Phrase: "trimmed phrase",
            GraphicDirection: "direction"), canEdit: true);

        Assert.Equal("trimmed phrase", vm.Phrase);
        Assert.Equal("concept", vm.ConceptIdea);
        Assert.Equal("direction", vm.GraphicDirection);
        Assert.False(vm.IsReadOnly);
    }

    [Fact]
    public void ConceptTool_ToStagePayload_CarriesConceptFieldsOnly()
    {
        var vm = new ConceptStageToolViewModel
        {
            ConceptIdea = "concept idea",
            Phrase = "phrase",
            GraphicDirection = "direction"
        };

        var payload = vm.ToStagePayload();

        Assert.Equal(WorkflowStage.Concept, payload.Stage);
        Assert.Equal("concept idea", payload.ConceptIdea);
        Assert.Equal("phrase", payload.Phrase);
        Assert.Equal("direction", payload.GraphicDirection);
        Assert.Null(payload.Idea);
    }

    [Fact]
    public void ListingTool_ReportsStatusSummaryAndHonorsEditability()
    {
        var vm = new ListingStageToolViewModel();

        vm.Load(ItemStatus.Published, canEdit: false);

        Assert.Contains("Published", vm.StatusSummary);
        Assert.True(vm.IsReadOnly);
    }

    [Fact]
    public async Task ListingTool_ShowsTemplateBlockersWhenNoReadyTemplateExists()
    {
        var vm = new ListingStageToolViewModel(new StubMockupGenerationService(new MockupGenerationState(
            Guid.NewGuid(), Guid.NewGuid(), false, string.Empty, [], null, [], ["Black"],
            "No ready Mockup Templates are available. Complete the requirements shown below in Store settings.",
            null,
            [new MockupTemplateEligibilityDiagnostic(Guid.NewGuid(), "Front image", [
                MockupTemplateReadinessBlocker.MissingImage,
                MockupTemplateReadinessBlocker.MissingMapping,
                MockupTemplateReadinessBlocker.MissingSourceApplicability,
                MockupTemplateReadinessBlocker.InvalidSourceApplicability,
                MockupTemplateReadinessBlocker.MissingVariantSourceImage,
                MockupTemplateReadinessBlocker.AmbiguousVariantSourceImages])])));

        await vm.LoadAsync(Guid.NewGuid(), ItemStatus.Draft, canEdit: true, TestContext.Current.CancellationToken);

        var diagnostic = Assert.Single(vm.TemplateDiagnostics);
        Assert.Equal("Front image", diagnostic.TemplateName);
        Assert.Equal([
            MockupTemplateReadinessBlocker.MissingImage,
            MockupTemplateReadinessBlocker.MissingMapping,
            MockupTemplateReadinessBlocker.MissingSourceApplicability,
            MockupTemplateReadinessBlocker.InvalidSourceApplicability,
            MockupTemplateReadinessBlocker.MissingVariantSourceImage,
            MockupTemplateReadinessBlocker.AmbiguousVariantSourceImages
        ], diagnostic.Blockers);
        Assert.Contains("Choose a mockup image.", diagnostic.Guidance);
        Assert.Contains("Add a valid design-area placement mapping.", diagnostic.Guidance);
        Assert.Contains("Choose applicability options for each source image.", diagnostic.Guidance);
        Assert.Contains("Remove unavailable applicability options", diagnostic.Guidance);
        Assert.Contains("Configure a matching source image for every compatible Variant.", diagnostic.Guidance);
        Assert.Contains("each compatible Variant matches exactly one image", diagnostic.Guidance);
        Assert.Contains(Environment.NewLine, diagnostic.Guidance);
        Assert.True(vm.HasBlockedReason);
        Assert.False(vm.CanApply);
    }

    [Fact]
    public async Task ListingTool_ShowsCoverageSummaryAlongsideReadinessBlockers()
    {
        var coverage = new MockupTemplateCoveragePlan(
            Guid.NewGuid(), Guid.NewGuid(), MockupTemplateCoverageGroupingStrategy.ColorFirst, "context",
            [new("black", MockupTemplateCoverageStatus.Missing, [Guid.NewGuid()], ["Black / M"], [], [], "Assign a source image.")], [], true);
        var vm = new ListingStageToolViewModel(new StubMockupGenerationService(new MockupGenerationState(
            Guid.NewGuid(), Guid.NewGuid(), false, string.Empty, [], null, [], ["Black"],
            "No ready Mockup Templates are available.", null,
            [new MockupTemplateEligibilityDiagnostic(Guid.NewGuid(), "Front image", [MockupTemplateReadinessBlocker.MissingVariantSourceImage], coverage)])));

        await vm.LoadAsync(Guid.NewGuid(), ItemStatus.Draft, canEdit: true, TestContext.Current.CancellationToken);

        var diagnostic = Assert.Single(vm.TemplateDiagnostics);
        Assert.Equal("0/1 Variants covered; 1 missing, 0 ambiguous, 0 incomplete.", diagnostic.CoverageSummary);
        Assert.True(diagnostic.HasCoverageSummary);
        Assert.Equal("Black / M: Assign a source image.", diagnostic.CoverageAffectedVariantGuidance);
        Assert.True(diagnostic.HasCoverageAffectedVariantGuidance);
        Assert.True(vm.HasBlockedReason);
        Assert.False(vm.CanApply);
    }

    [Fact]
    public async Task ListingTool_DistinguishesOfferingWithNoTemplates()
    {
        var vm = new ListingStageToolViewModel(new StubMockupGenerationService(new MockupGenerationState(
            Guid.NewGuid(), Guid.NewGuid(), false, string.Empty, [], null, [], ["Black"],
            "No Mockup Templates are configured for this Offering. Add one in Store settings.", null, [])));

        await vm.LoadAsync(Guid.NewGuid(), ItemStatus.Draft, canEdit: true, TestContext.Current.CancellationToken);

        Assert.Contains("No Mockup Templates are configured", vm.BlockedReason);
        Assert.Empty(vm.TemplateDiagnostics);
    }

    [Fact]
    public async Task ListingTool_WhenReadinessLoadFails_ShowsRecoveryErrorWithoutClaimingNoTemplates()
    {
        var vm = new ListingStageToolViewModel(new ThrowingMockupGenerationService(
            new InvalidOperationException("Workspace context is unavailable.")));

        await vm.LoadAsync(Guid.NewGuid(), ItemStatus.Draft, canEdit: true, TestContext.Current.CancellationToken);

        Assert.False(vm.HasBlockedReason);
        Assert.Empty(vm.Templates);
        Assert.Empty(vm.TemplateDiagnostics);
        Assert.Contains("Listing readiness could not be loaded", vm.ErrorMessage);
        Assert.Contains("Workspace context is unavailable", vm.ErrorMessage);
        Assert.Contains("reloading Listing", vm.ErrorMessage);
    }

    [Fact]
    public async Task ListingTool_SelectingTemplateEnablesApplyAndUsesNameAsDisplayValue()
    {
        var templateId = Guid.NewGuid();
        var vm = new ListingStageToolViewModel(new StubMockupGenerationService(new MockupGenerationState(
            Guid.NewGuid(), Guid.NewGuid(), false, string.Empty,
            [new MockupTemplate(templateId, Guid.NewGuid(), null, "Flatlay no 1", null, 1, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)],
            templateId, [], [], null, null, [])));

        await vm.LoadAsync(Guid.NewGuid(), ItemStatus.Draft, canEdit: true, TestContext.Current.CancellationToken);

        var template = Assert.Single(vm.Templates);
        Assert.Equal("Flatlay no 1", template.Name);
        Assert.Equal(templateId, vm.SelectedTemplateId);
        Assert.True(vm.CanApply);
        Assert.Empty(vm.TemplateDiagnostics);
        Assert.False(vm.HasTemplateDiagnostics);

        vm.SelectedTemplate = null;
        Assert.False(vm.CanApply);
        vm.SelectedTemplate = template;
        Assert.True(vm.CanApply);
    }

    [Fact]
    public async Task ListingTool_ReapplyingTemplateRetainsPriorOutputsForReview()
    {
        var itemId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var service = new ApplyingMockupGenerationService(itemId, templateId);
        var vm = new ListingStageToolViewModel(service);

        await vm.LoadAsync(itemId, ItemStatus.Draft, canEdit: true, TestContext.Current.CancellationToken);
        await vm.ApplyAsync();
        var first = Assert.Single(vm.Outputs);

        await vm.ApplyAsync();

        Assert.Equal(2, vm.Outputs.Count);
        Assert.NotEqual(first.AssetId, vm.Outputs[1].AssetId);
        Assert.Equal(2, service.ApplyCalls);
    }

    [Fact]
    public async Task ListingTool_CancelsPriorLoadAndIgnoresItsLateResult()
    {
        var firstItemId = Guid.NewGuid();
        var secondItemId = Guid.NewGuid();
        var firstTemplateId = Guid.NewGuid();
        var secondTemplateId = Guid.NewGuid();
        var service = new DeferredMockupGenerationService();
        service.Add(firstItemId, CreateState(firstItemId, firstTemplateId, "First item template"));
        service.Add(secondItemId, CreateState(secondItemId, secondTemplateId, "Second item template"));
        var vm = new ListingStageToolViewModel(service);

        var firstLoad = vm.LoadAsync(firstItemId, ItemStatus.Draft, canEdit: true, TestContext.Current.CancellationToken);
        var secondLoad = vm.LoadAsync(secondItemId, ItemStatus.Draft, canEdit: true, TestContext.Current.CancellationToken);

        Assert.Contains(firstItemId, service.CancelledItemIds);

        service.Complete(firstItemId);
        service.Complete(secondItemId);
        await Task.WhenAll(firstLoad, secondLoad);

        var template = Assert.Single(vm.Templates);
        Assert.Equal(secondTemplateId, template.Id);
        Assert.Equal("Second item template", template.Name);
    }

    [Fact]
    public async Task ListingTool_ClearsPriorReadinessWhileNextLoadIsPending()
    {
        var firstItemId = Guid.NewGuid();
        var secondItemId = Guid.NewGuid();
        var service = new DeferredMockupGenerationService
        {
            PendingItemId = secondItemId
        };
        service.Add(firstItemId, CreateState(firstItemId, Guid.NewGuid(), "Old template") with
        {
            BlockedReason = "Old blocker",
            Error = "Old error"
        });
        service.Add(secondItemId, CreateState(secondItemId, Guid.NewGuid(), "New template"));
        var vm = new ListingStageToolViewModel(service);

        var firstLoad = vm.LoadAsync(firstItemId, ItemStatus.Draft, canEdit: true, TestContext.Current.CancellationToken);
        service.Complete(firstItemId);
        await firstLoad;
        Assert.Single(vm.Templates);
        Assert.Equal("Old blocker", vm.BlockedReason);

        var nextLoad = vm.LoadAsync(secondItemId, ItemStatus.Draft, canEdit: true, TestContext.Current.CancellationToken);
        await service.PendingLoadStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        Assert.Empty(vm.Templates);
        Assert.Null(vm.SelectedTemplate);
        Assert.Empty(vm.TemplateDiagnostics);
        Assert.False(vm.HasTemplateDiagnostics);
        Assert.Empty(vm.Outputs);
        Assert.Null(vm.BlockedReason);
        Assert.Null(vm.ErrorMessage);

        service.Complete(secondItemId);
        await nextLoad;
        Assert.Equal("New template", Assert.Single(vm.Templates).Name);
    }

    private static MockupGenerationState CreateState(Guid itemId, Guid templateId, string templateName) =>
        new(itemId, Guid.NewGuid(), false, string.Empty,
            [new MockupTemplate(templateId, Guid.NewGuid(), null, templateName, null, 1, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)],
            templateId, [], [], null, null, []);

    private sealed class StubMockupGenerationService(MockupGenerationState state) : IMockupGenerationService
    {
        public Task<MockupGenerationState> LoadAsync(Guid itemId, bool isReadOnly, string readOnlyReason, CancellationToken cancellationToken = default) => Task.FromResult(state);

        public Task<MockupGenerationResult> ApplyAsync(MockupGenerationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MockupGenerationResult.Failure("Not used in this test."));
    }

    private sealed class ThrowingMockupGenerationService(Exception exception) : IMockupGenerationService
    {
        public Task<MockupGenerationState> LoadAsync(Guid itemId, bool isReadOnly, string readOnlyReason, CancellationToken cancellationToken = default) =>
            Task.FromException<MockupGenerationState>(exception);

        public Task<MockupGenerationResult> ApplyAsync(MockupGenerationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MockupGenerationResult.Failure("Not used in this test."));
    }

    private sealed class DeferredMockupGenerationService : IMockupGenerationService
    {
        private readonly Dictionary<Guid, (TaskCompletionSource<MockupGenerationState> Completion, MockupGenerationState State)> _loads = [];

        public List<Guid> CancelledItemIds { get; } = [];
        public Guid? PendingItemId { get; init; }
        public TaskCompletionSource PendingLoadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Add(Guid itemId, MockupGenerationState state) =>
            _loads[itemId] = (new(TaskCreationOptions.RunContinuationsAsynchronously), state);

        public void Complete(Guid itemId) => _loads[itemId].Completion.TrySetResult(_loads[itemId].State);

        public Task<MockupGenerationState> LoadAsync(Guid itemId, bool isReadOnly, string readOnlyReason, CancellationToken cancellationToken = default)
        {
            if (itemId == PendingItemId) PendingLoadStarted.TrySetResult();
            cancellationToken.Register(() => CancelledItemIds.Add(itemId));
            return _loads[itemId].Completion.Task;
        }

        public Task<MockupGenerationResult> ApplyAsync(MockupGenerationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MockupGenerationResult.Failure("Not used in this test."));
    }

    private sealed class ApplyingMockupGenerationService(Guid itemId, Guid templateId) : IMockupGenerationService
    {
        public int ApplyCalls { get; private set; }

        public Task<MockupGenerationState> LoadAsync(Guid requestedItemId, bool isReadOnly, string readOnlyReason, CancellationToken cancellationToken = default)
        {
            Assert.Equal(itemId, requestedItemId);
            return Task.FromResult(new MockupGenerationState(
                itemId, Guid.NewGuid(), false, string.Empty,
                [new MockupTemplate(templateId, Guid.NewGuid(), null, "Flatlay", null, 1, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)],
                templateId, [], [], null, null, []));
        }

        public Task<MockupGenerationResult> ApplyAsync(MockupGenerationRequest request, CancellationToken cancellationToken = default)
        {
            ApplyCalls++;
            return Task.FromResult(new MockupGenerationResult(
                true,
                null,
                [new MockupGenerationOutput(Guid.NewGuid(), $"mockup-{ApplyCalls}", $"mockup-{ApplyCalls}.png", "Black", request.TemplateId, 1, Guid.NewGuid())],
                []));
        }
    }

    private sealed class Factory(ListingLifecycleService service) : IListingLifecycleServiceFactory
    {
        public ListingLifecycleService Create(Store store) => service;
    }

    private sealed class ProjectionSource : IListingProjectionSource
    {
        public Task<ListingProjectionResult> BuildAsync(WorkspaceSnapshot snapshot, Guid itemId, ListingPricingInput pricing, string? shippingProfile, string? outOfStockPolicy, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ListingProjectionResult(new ListingProductProjection(
                itemId,
                snapshot.Items.Single(item => item.Id == itemId).StoreId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Dad joke – T-shirt",
                "A joke shirt.",
                shippingProfile,
                outOfStockPolicy,
                pricing,
                [],
                []), []));
    }

    private sealed class Connection : IListingConnectionPort
    {
        public Task<ListingReadiness> CheckAsync(ListingConnectionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ListingReadiness(ListingConnectionState.Ready, true, request.RequirePublication, []));
    }

    private sealed class Products : IListingProductPort
    {
        public int CreateCalls { get; private set; }
        public int UpdateCalls { get; private set; }
        public int DeleteCalls { get; private set; }
        public ListingRemoteProduct? RemoteProduct { get; set; }

        public Task<ListingRemoteProduct?> GetAsync(string shopId, string productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(RemoteProduct);

        public Task<ListingMutationResult> CreateAsync(string shopId, ListingProductProjection projection, IReadOnlyDictionary<Guid, ListingImageReference> images, CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateResult());

        public Task<ListingMutationResult> UpdateAsync(string shopId, string productId, ListingProductProjection projection, IReadOnlyDictionary<Guid, ListingImageReference> images, CancellationToken cancellationToken = default) =>
            Task.FromResult(UpdateResult());

        public Task<ListingMutationResult> DeleteAsync(string shopId, string productId, CancellationToken cancellationToken = default)
        {
            DeleteCalls++;
            return Task.FromResult(new ListingMutationResult(productId, true));
        }

        private ListingMutationResult CreateResult()
        {
            CreateCalls++;
            return new ListingMutationResult("product-1", true);
        }

        private ListingMutationResult UpdateResult()
        {
            UpdateCalls++;
            return new ListingMutationResult("product-1", true);
        }
    }

    private sealed class Publication : IListingPublicationPort
    {
        public int PublishCalls { get; private set; }
        public int UnpublishCalls { get; private set; }

        public Task<ListingMutationResult> PublishAsync(string shopId, string productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(PublishResult(productId));

        public Task<ListingMutationResult> UnpublishAsync(string shopId, string productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(UnpublishResult(productId));

        private ListingMutationResult PublishResult(string productId)
        {
            PublishCalls++;
            return new ListingMutationResult(productId, true);
        }

        private ListingMutationResult UnpublishResult(string productId)
        {
            UnpublishCalls++;
            return new ListingMutationResult(productId, true);
        }
    }

    private sealed class ContextMapper : IStoreContextMapper
    {
        public StoreContext Read(Store store) => new(PrintifyShopId: 42, PrintifyShopTitle: "The Groan Zone");

        public Store Apply(Store store, StoreContext context) => store;
    }

    private sealed class MemoryRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = snapshot;

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            Snapshot = snapshot;
            return Task.CompletedTask;
        }
    }
}
