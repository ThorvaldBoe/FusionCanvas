using FusionCanvas.Application.Catalog;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests.Catalog;

public sealed class OfferingManagementServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task BlueprintListIsIdentityScopedAndNamesTheActualFulfillmentProvider()
    {
        var fixture = Fixture.Create();
        var otherBlueprint = new Blueprint(Guid.NewGuid(), fixture.Store.Id, "Other", null, false, Now, Now);
        var otherOffering = new BlueprintOffering(Guid.NewGuid(), otherBlueprint.Id, fixture.Store.Id, "Other offering", null, BlueprintOfferingKind.ProviderNetwork, null, "printify-choice", null, null, false, Now, Now);
        fixture.Snapshot = fixture.Snapshot with { Blueprints = [.. fixture.Snapshot.Blueprints, otherBlueprint], BlueprintOfferings = [.. fixture.Snapshot.BlueprintOfferings, otherOffering] };
        var service = new OfferingManagementService(new MemoryRepository(fixture.Snapshot));

        var result = await service.LoadForBlueprintAsync(fixture.Store.Id, fixture.Blueprint.Id, TestContext.Current.CancellationToken);

        var summary = Assert.Single(result);
        Assert.Equal(fixture.Offering.Id, summary.Context.OfferingId);
        Assert.Equal("SwiftPOD", summary.Fulfillment.DisplayName);
        Assert.False(summary.Fulfillment.IsVariableProviderNetwork);
        Assert.Equal("Printify", summary.Fulfillment.CatalogSource);
        Assert.Equal(new OfferingSetupCounts(1, 1, 1), summary.Counts);
    }

    [Fact]
    public async Task ProviderNetworkDoesNotFabricateFixedProviderAndArchivedStoreIsReadOnly()
    {
        var fixture = Fixture.Create(providerNetwork: true, archivedStore: true);
        var service = new OfferingManagementService(new MemoryRepository(fixture.Snapshot));

        var state = await service.LoadOfferingAsync(new OfferingContext(fixture.Store.Id, fixture.Blueprint.Id, fixture.Offering.Id), TestContext.Current.CancellationToken);

        Assert.True(state.IsReadOnly);
        Assert.True(state.Summary.Fulfillment.IsVariableProviderNetwork);
        Assert.Equal("Printify Choice Provider Network", state.Summary.Fulfillment.DisplayName);
        Assert.DoesNotContain("SwiftPOD", state.Summary.Fulfillment.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DesignAreaProjectionKeepsPixelsAuthoritativeAndDerivesPhysicalSizeOnlyWithDpi()
    {
        var fixture = Fixture.Create();
        var original = fixture.Snapshot.OfferingPlaceholders.Single();
        var area = new OfferingPlaceholder(original.Id, original.OfferingId, original.Name, original.Description, original.Position, original.DecorationMethod, original.Width, original.Height, original.VariantIds, original.IsArchived, original.CreatedAt, original.UpdatedAt, providerReference: "front-area", artworkGuidance: new DesignAreaArtworkGuidance(4500, 5400, 300, "PNG", "Transparent"));
        fixture.Snapshot = fixture.Snapshot with { OfferingPlaceholders = [area] };

        var state = await new OfferingManagementService(new MemoryRepository(fixture.Snapshot)).LoadOfferingAsync(
            new OfferingContext(fixture.Store.Id, fixture.Blueprint.Id, fixture.Offering.Id),
            TestContext.Current.CancellationToken);

        var summary = Assert.Single(state.DesignAreaSummaries);
        Assert.Equal(3000, summary.MaximumWidthPixels);
        Assert.Equal(10, summary.SecondaryPhysicalSize!.Value.WidthInches);
        Assert.Equal("front-area", summary.ProviderReference);
        Assert.True(summary.AppliesToAllActiveVariants);
    }

    [Fact]
    public async Task StableContextRejectsCrossBlueprintFallback()
    {
        var fixture = Fixture.Create();
        var service = new OfferingManagementService(new MemoryRepository(fixture.Snapshot));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoadOfferingAsync(
            new OfferingContext(fixture.Store.Id, Guid.NewGuid(), fixture.Offering.Id),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UnavailableCatalogBoundaryIsDeterministicAndReadOnly()
    {
        var context = new OfferingContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var source = new UnavailableProviderCatalogCandidateSource();

        var first = await source.LoadAsync(context, TestContext.Current.CancellationToken);
        var second = await source.LoadAsync(context, TestContext.Current.CancellationToken);

        Assert.Equal(first.Context, second.Context);
        Assert.Equal(first.UnavailableReason, second.UnavailableReason);
        Assert.False(first.IsAvailable);
        Assert.Empty(first.ValidColorSizeCombinations);
    }

    [Fact]
    public async Task BulkColorWorkflowPreviewsExclusionsAndAtomicallyCreatesOnlyNewValidSizes()
    {
        var fixture = Fixture.Create();
        var sizeOption = new OfferingOption(Guid.NewGuid(), fixture.Offering.Id, OptionKind.Size, "Size", 1);
        var small = new OfferingOptionValue(Guid.NewGuid(), sizeOption.Id, fixture.Offering.Id, "S", 0);
        var medium = new OfferingOptionValue(Guid.NewGuid(), sizeOption.Id, fixture.Offering.Id, "M", 1);
        var large = new OfferingOptionValue(Guid.NewGuid(), sizeOption.Id, fixture.Offering.Id, "L", 2);
        var color = fixture.Snapshot.OfferingOptionValues.Single();
        var existing = new OfferingVariant(Guid.NewGuid(), fixture.Offering.Id, "Black / S", [color.Id, small.Id], false, Now, Now);
        fixture.Snapshot = fixture.Snapshot with
        {
            OfferingOptions = [.. fixture.Snapshot.OfferingOptions, sizeOption],
            OfferingOptionValues = [.. fixture.Snapshot.OfferingOptionValues, small, medium, large],
            OfferingVariants = [existing]
        };
        var context = new OfferingContext(fixture.Store.Id, fixture.Blueprint.Id, fixture.Offering.Id);
        var descriptor = new ProviderCatalogCandidateDescriptor(context, true, null, new HashSet<ProviderCatalogCombination>
        {
            new(color.Id, small.Id), new(color.Id, medium.Id)
        });
        var repository = new MutableMemoryRepository(fixture.Snapshot);
        var service = new OfferingManagementService(repository, new FixedCatalogSource(descriptor), () => Now, Guid.NewGuid);
        var request = new BulkVariantRequest(context, color.Id, [small.Id, medium.Id, large.Id]);

        var preview = await service.PreviewBulkVariantsAsync(request, TestContext.Current.CancellationToken);
        var result = await service.ConfirmBulkVariantsAsync(request, TestContext.Current.CancellationToken);

        Assert.True(preview.CanConfirm);
        Assert.Contains(preview.Candidates, value => value.SizeOptionValueId == small.Id && !value.WillCreate && value.ExclusionReason!.Contains("already exists", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(preview.Candidates, value => value.SizeOptionValueId == large.Id && !value.WillCreate && value.ExclusionReason!.Contains("does not allow", StringComparison.OrdinalIgnoreCase));
        Assert.True(result.Succeeded);
        Assert.Equal(medium.Id, Assert.Single(result.CreatedVariants).OptionValueIds.Single(value => value != color.Id));
        Assert.Equal(2, repository.Snapshot.OfferingVariants.Count);
    }

    [Fact]
    public async Task BulkColorWorkflowNoOpsWhenCatalogUnavailableOrNothingNewRemains()
    {
        var fixture = Fixture.Create();
        var context = new OfferingContext(fixture.Store.Id, fixture.Blueprint.Id, fixture.Offering.Id);
        var repository = new MutableMemoryRepository(fixture.Snapshot);
        var service = new OfferingManagementService(repository);
        var request = new BulkVariantRequest(context, fixture.Snapshot.OfferingOptionValues.Single().Id, []);

        var result = await service.ConfirmBulkVariantsAsync(request, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Empty(result.CreatedVariants);
        Assert.DoesNotContain(repository.Snapshot.OfferingVariants, value => value.Id != fixture.Snapshot.OfferingVariants.Single().Id);
    }

    [Fact]
    public async Task BulkColorWorkflowRejectsCrossOfferingValuesAndCancellationCannotPartiallySave()
    {
        var fixture = Fixture.Create();
        var context = new OfferingContext(fixture.Store.Id, fixture.Blueprint.Id, fixture.Offering.Id);
        var otherColor = new OfferingOptionValue(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Other", 0);
        var repository = new MutableMemoryRepository(fixture.Snapshot);
        var descriptor = new ProviderCatalogCandidateDescriptor(context, true, null, new HashSet<ProviderCatalogCombination>());
        var service = new OfferingManagementService(repository, new FixedCatalogSource(descriptor), () => Now, Guid.NewGuid);

        var crossOffering = await service.PreviewBulkVariantsAsync(new BulkVariantRequest(context, otherColor.Id, []), TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ConfirmBulkVariantsAsync(
            new BulkVariantRequest(context, fixture.Snapshot.OfferingOptionValues.Single().Id, []),
            cancellation.Token));

        Assert.False(crossOffering.CanConfirm);
        Assert.Contains("Color", crossOffering.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Same(fixture.Snapshot, repository.Snapshot);
    }

    [Fact]
    public async Task FocusedCommandsUseStableContextAndKeepInvalidDesignAreaDraftOutOfPersistence()
    {
        var fixture = Fixture.Create();
        var repository = new MutableMemoryRepository(fixture.Snapshot);
        var service = new OfferingManagementService(repository, clock: () => Now, newId: Guid.NewGuid);
        var context = new OfferingContext(fixture.Store.Id, fixture.Blueprint.Id, fixture.Offering.Id);
        var color = fixture.Snapshot.OfferingOptionValues.Single();

        var duplicate = await service.CreateVariantAsync(new CreateFocusedVariantRequest(context, "Duplicate", [color.Id]), TestContext.Current.CancellationToken);
        var invalidArea = await service.CreateDesignAreaAsync(new CreateFocusedDesignAreaRequest(context, "Invalid", "front", "DTG", 0, 4500, [], true), TestContext.Current.CancellationToken);

        Assert.False(duplicate.Succeeded);
        Assert.Contains("already", duplicate.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(invalidArea.Succeeded);
        Assert.DoesNotContain(repository.Snapshot.OfferingPlaceholders, value => value.Name == "Invalid");
    }

    [Fact]
    public async Task ProviderMockupSelectionCreatesRevisionAndDerivesConcreteVariantsFromColor()
    {
        var fixture = Fixture.Create();
        var context = new OfferingContext(fixture.Store.Id, fixture.Blueprint.Id, fixture.Offering.Id);
        var color = fixture.Snapshot.OfferingOptionValues.Single();
        var descriptor = new ProviderCatalogCandidateDescriptor(context, true, null, new HashSet<ProviderCatalogCombination>(),
        [new ProviderMockupCandidateDescriptor("front-black", "Front — Black", 1200, 1200, new HashSet<Guid> { color.Id })]);
        var repository = new MutableMemoryRepository(fixture.Snapshot);
        var service = new OfferingManagementService(repository, new FixedCatalogSource(descriptor), () => Now, Guid.NewGuid);
        var area = fixture.Snapshot.OfferingPlaceholders.Single();
        var mapping = new MockupImageSpaceMapping(1200, 1200, 300, 200, 500, 650);

        var result = await service.CreateMockupTemplateAsync(new CreateFocusedMockupTemplateRequest(context, "Front black", "front-black", area.Id, [color.Id], mapping), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Single(repository.Snapshot.MockupTemplateColorVariants);
        var summary = result.State.MockupTemplateSummaries.Single(value => value.Name == "Front black");
        Assert.Equal("front-black", summary.ProviderMockupReference);
        Assert.Equal(1, summary.CurrentRevision);
        Assert.Equal(color.Id, Assert.Single(summary.ColorOptionValueIds));
        Assert.Equal(fixture.Snapshot.OfferingVariants.Single().Id, Assert.Single(summary.CompatibleVariantIds));
        Assert.Equal(mapping, repository.Snapshot.MockupTemplateRevisions.Single().ImageMapping);
    }

    [Fact]
    public async Task ProviderMockupSelectionReportsEveryIncompatibleConcreteVariantWithoutSaving()
    {
        var fixture = Fixture.Create();
        var originalArea = fixture.Snapshot.OfferingPlaceholders.Single();
        var incompatibleArea = new OfferingPlaceholder(originalArea.Id, originalArea.OfferingId, originalArea.Name, originalArea.Description, originalArea.Position, originalArea.DecorationMethod, originalArea.Width, originalArea.Height, [], false, originalArea.CreatedAt, originalArea.UpdatedAt);
        fixture.Snapshot = fixture.Snapshot with { OfferingPlaceholders = [incompatibleArea], MockupTemplates = [] };
        var context = new OfferingContext(fixture.Store.Id, fixture.Blueprint.Id, fixture.Offering.Id);
        var color = fixture.Snapshot.OfferingOptionValues.Single();
        var descriptor = new ProviderCatalogCandidateDescriptor(context, true, null, new HashSet<ProviderCatalogCombination>(),
        [new ProviderMockupCandidateDescriptor("front-black", "Front — Black", 1200, 1200, new HashSet<Guid> { color.Id })]);
        var repository = new MutableMemoryRepository(fixture.Snapshot);
        var service = new OfferingManagementService(repository, new FixedCatalogSource(descriptor), () => Now, Guid.NewGuid);

        var result = await service.CreateMockupTemplateAsync(new CreateFocusedMockupTemplateRequest(context, "Front black", "front-black", incompatibleArea.Id, [color.Id], new MockupImageSpaceMapping(1200, 1200, 300, 200, 500, 650)), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains(fixture.Snapshot.OfferingVariants.Single().Name, result.Details!);
        Assert.Empty(repository.Snapshot.MockupTemplates);
    }

    private sealed class Fixture
    {
        public required Store Store { get; init; }
        public required Blueprint Blueprint { get; init; }
        public required BlueprintOffering Offering { get; init; }
        public required WorkspaceSnapshot Snapshot { get; set; }

        public static Fixture Create(bool providerNetwork = false, bool archivedStore = false)
        {
            var store = new Store(Guid.NewGuid(), "Store", null, archivedStore, Now, Now, "{}");
            var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, Now, Now);
            var provider = new PrintProvider(Guid.NewGuid(), store.Id, "SwiftPOD", "provider-42", false, Now, Now);
            var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Tee", null, providerNetwork ? BlueprintOfferingKind.ProviderNetwork : BlueprintOfferingKind.FixedPrintProvider, providerNetwork ? null : provider.Id, providerNetwork ? "printify-choice" : null, null, null, false, Now, Now);
            var colorOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Color, "Color", 0);
            var black = new OfferingOptionValue(Guid.NewGuid(), colorOption.Id, offering.Id, "Black", 0);
            var variant = new OfferingVariant(Guid.NewGuid(), offering.Id, "Black", [black.Id], false, Now, Now);
            var area = new OfferingPlaceholder(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 3000, 4500, [variant.Id], false, Now, Now);
            var template = new MockupTemplate(Guid.NewGuid(), offering.Id, area.Id, "Front mockup", null, 1, false, Now, Now);
            return new Fixture
            {
                Store = store,
                Blueprint = blueprint,
                Offering = offering,
                Snapshot = new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)], [store], [], [], [], [], [], [], [], [])
                {
                    Blueprints = [blueprint], PrintProviders = [provider], BlueprintOfferings = [offering],
                    OfferingOptions = [colorOption], OfferingOptionValues = [black], OfferingVariants = [variant],
                    OfferingPlaceholders = [area], MockupTemplates = [template]
                }
            };
        }
    }

    private sealed class MemoryRepository(WorkspaceSnapshot initial) : IWorkspaceRepository
    {
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(initial);
        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class MutableMemoryRepository(WorkspaceSnapshot initial) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = initial;
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default) { Snapshot = snapshot; return Task.CompletedTask; }
    }

    private sealed class FixedCatalogSource(ProviderCatalogCandidateDescriptor descriptor) : IProviderCatalogCandidateSource
    {
        public Task<ProviderCatalogCandidateDescriptor> LoadAsync(OfferingContext context, CancellationToken cancellationToken = default) => Task.FromResult(descriptor);
    }
}
