using FusionCanvas.Application.Catalog;
using FusionCanvas.Application.Mockups;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests.Mockups;

public sealed class LocalMockupTemplateReadinessTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CompleteLocalImagesAreEligibleWithoutLegacyTemplateFields()
    {
        var snapshot = CreateSnapshot();
        var repository = new MemoryRepository(snapshot);
        var template = snapshot.MockupTemplates.Single();
        var offering = snapshot.BlueprintOfferings.Single();
        var service = new MockupTemplateSetupService(repository);

        var result = await service.GetEligibleTemplatesAsync(offering.StoreId, offering.Id, template.Id, TestContext.Current.CancellationToken);
        var catalog = await new OfferingManagementService(repository).LoadOfferingAsync(
            new(offering.StoreId, offering.BlueprintId, offering.Id), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(template.Id, Assert.Single(result.Templates).Id);
        Assert.Empty(Assert.Single(result.CandidateDiagnostics).Blockers);
        Assert.Equal(MockupTemplateLifecycle.ReadyForUse, Assert.Single(catalog.MockupTemplateSummaries).Lifecycle);
        Assert.Empty(snapshot.MockupTemplateColorVariants);
        Assert.Null(snapshot.MockupTemplateRevisions.Single().ProviderMockupReference);
        Assert.Null(snapshot.MockupTemplateRevisions.Single().ImageMapping);
    }

    private static WorkspaceSnapshot CreateSnapshot()
    {
        var storeId = Guid.NewGuid();
        var blueprint = new Blueprint(Guid.NewGuid(), storeId, "T-shirt", null, false, Now, Now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, storeId, "Tee", null, BlueprintOfferingKind.ProviderNetwork, null, "manual", null, null, false, Now, Now);
        var color = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Color, "Color", 0);
        var size = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Size, "Size", 1);
        var colors = new[] { "Black", "Navy" }.Select((name, index) => new OfferingOptionValue(Guid.NewGuid(), color.Id, offering.Id, name, index)).ToArray();
        var sizes = new[] { "S", "M", "L" }.Select((name, index) => new OfferingOptionValue(Guid.NewGuid(), size.Id, offering.Id, name, index)).ToArray();
        var variants = colors.SelectMany(c => sizes.Select(s => new OfferingVariant(Guid.NewGuid(), offering.Id, $"{c.Value} {s.Value}", [c.Id, s.Id], false, Now, Now))).ToArray();
        var area = new OfferingPlaceholder(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 3000, 4500, variants.Select(v => v.Id).ToArray(), false, Now, Now);
        var template = new MockupTemplate(Guid.NewGuid(), offering.Id, area.Id, "Flatlay no 1", null, 1, false, Now, Now);
        var images = colors.Select(_ => new MockupTemplateSourceImage(Guid.NewGuid(), template.Id, Guid.NewGuid(), new(2000, 2000, 869, 479, 897, 1166), false, Now, Now)).ToArray();
        var conditions = images.SelectMany((image, index) => new[] { colors[index].Id }.Concat(sizes.Select(s => s.Id)).Select(id => new MockupTemplateSourceImageOptionValue(image.Id, id))).ToArray();
        return new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)], [new Store(storeId, "Store", null, false, Now, Now, "{}")], [], [], [], [], [], [], [], [])
        {
            Blueprints = [blueprint], BlueprintOfferings = [offering], OfferingOptions = [color, size],
            OfferingOptionValues = [.. colors, .. sizes], OfferingVariants = variants, OfferingPlaceholders = [area],
            MockupTemplates = [template], MockupTemplateRevisions = [new(Guid.NewGuid(), template.Id, 1, area.Id, Now)],
            MockupTemplateSourceImages = images, MockupTemplateSourceImageOptionValues = conditions
        };
    }

    [Fact]
    public async Task ListingLoadsAndGeneratesBothColorsFromLocalRevisionImages()
    {
        var snapshot = CreateSnapshot();
        var offering = snapshot.BlueprintOfferings.Single();
        var template = snapshot.MockupTemplates.Single();
        var revision = snapshot.MockupTemplateRevisions.Single();
        var item = new Item(Guid.NewGuid(), offering.StoreId, null, null, "Test listing", null, ItemStatus.Draft, WorkflowStage.Listing, false, Now, Now, "{}");
        var design = new Asset(Guid.NewGuid(), offering.StoreId, "design.png", null, AssetKind.ExportedImage, "assets/design.png", null, false, false, Now, Now, "{}");
        var row = new DesignVariantRow(Guid.NewGuid(), item.Id, true, 0);
        snapshot = snapshot with
        {
            Items = [item], ItemListingConfigurations = [new(item.Id, offering.Id)],
            DesignSelectedColors = [new(item.Id, "Black"), new(item.Id, "Navy")],
            DesignVariantRows = [row], DesignVariantRowColors = [new(row.Id, "Black"), new(row.Id, "Navy")],
            DesignSlotAssignments = [new(row.Id, template.TargetPlaceholderId!.Value, design.Id)],
            Assets = [design, .. snapshot.MockupTemplateSourceImages.Select(image => new Asset(image.SourceAssetId, offering.StoreId,
                "source.png", null, AssetKind.MockupImage, $"assets/{image.Id}.png", null, false, false, Now, Now, "{}"))],
            MockupTemplateRevisionSourceImages = snapshot.MockupTemplateSourceImages.Select(image =>
                new MockupTemplateRevisionSourceImage(image.Id, revision.Id, image.SourceAssetId, image.ImageMapping)).ToArray(),
            MockupTemplateRevisionSourceImageOptionValues = snapshot.MockupTemplateSourceImageOptionValues.Select(value =>
                new MockupTemplateRevisionSourceImageOptionValue(value.SourceImageId, value.OptionValueId)).ToArray()
        };
        var repository = new MemoryRepository(snapshot);
        var files = new MemoryFileStore();
        var service = new MockupGenerationService(repository, files, new MockupTemplateSetupService(repository), new StubCompositor());

        var state = await service.LoadAsync(item.Id, false, string.Empty, TestContext.Current.CancellationToken);
        var result = await service.ApplyAsync(new(item.Id, template.Id), TestContext.Current.CancellationToken);

        Assert.Null(state.BlockedReason);
        Assert.Equal(template.Id, Assert.Single(state.Templates).Id);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(new[] { "Black", "Navy" }, result.Outputs.Select(value => value.ColorValue));
        Assert.All(result.Outputs, output => Assert.Equal(revision.RevisionNumber, output.TemplateRevision));
        Assert.Equal(2, files.SaveCount);
    }

    [Fact]
    public async Task Apply_SanitizesWindowsInvalidCharactersFromGeneratedFileNames()
    {
        var snapshot = CreateSnapshot();
        var offering = snapshot.BlueprintOfferings.Single();
        var template = snapshot.MockupTemplates.Single();
        var revision = snapshot.MockupTemplateRevisions.Single();
        var item = new Item(Guid.NewGuid(), offering.StoreId, null, null, "A \"Dad Joke\" listing", null, ItemStatus.Draft, WorkflowStage.Listing, false, Now, Now, "{}");
        var design = new Asset(Guid.NewGuid(), offering.StoreId, "design.png", null, AssetKind.ExportedImage, "assets/design.png", null, false, false, Now, Now, "{}");
        var row = new DesignVariantRow(Guid.NewGuid(), item.Id, true, 0);
        snapshot = snapshot with
        {
            Items = [item], ItemListingConfigurations = [new(item.Id, offering.Id)],
            OfferingOptionValues = snapshot.OfferingOptionValues.Select(value => value.Value == "Black" ? value with { Value = "Black/White" } : value).ToArray(),
            DesignSelectedColors = [new(item.Id, "Black/White")],
            DesignVariantRows = [row], DesignVariantRowColors = [new(row.Id, "Black/White")],
            DesignSlotAssignments = [new(row.Id, template.TargetPlaceholderId!.Value, design.Id)],
            Assets = [design, .. snapshot.MockupTemplateSourceImages.Select(image => new Asset(image.SourceAssetId, offering.StoreId,
                "source.png", null, AssetKind.MockupImage, $"assets/{image.Id}.png", null, false, false, Now, Now, "{}"))],
            MockupTemplateRevisionSourceImages = snapshot.MockupTemplateSourceImages.Select(image =>
                new MockupTemplateRevisionSourceImage(image.Id, revision.Id, image.SourceAssetId, image.ImageMapping)).ToArray(),
            MockupTemplateRevisionSourceImageOptionValues = snapshot.MockupTemplateSourceImageOptionValues.Select(value =>
                new MockupTemplateRevisionSourceImageOptionValue(value.SourceImageId, value.OptionValueId)).ToArray()
        };
        var repository = new MemoryRepository(snapshot);
        var files = new MemoryFileStore();
        var service = new MockupGenerationService(repository, files, new MockupTemplateSetupService(repository), new StubCompositor());

        var result = await service.ApplyAsync(new(item.Id, template.Id), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("A -Dad Joke- listing-Black-White-mockup.png", Assert.Single(files.SavedFileNames));
    }

    [Theory]
    [InlineData("missing-image", MockupTemplateReadinessBlocker.MissingVariantSourceImage)]
    [InlineData("ambiguous", MockupTemplateReadinessBlocker.AmbiguousVariantSourceImages)]
    [InlineData("mapping", MockupTemplateReadinessBlocker.MissingMapping)]
    [InlineData("applicability", MockupTemplateReadinessBlocker.MissingSourceApplicability)]
    [InlineData("archived-option", MockupTemplateReadinessBlocker.InvalidSourceApplicability)]
    [InlineData("archived-value", MockupTemplateReadinessBlocker.InvalidSourceApplicability)]
    [InlineData("archived-images", MockupTemplateReadinessBlocker.MissingImage)]
    [InlineData("archived-template", MockupTemplateReadinessBlocker.Archived)]
    [InlineData("archived-area", MockupTemplateReadinessBlocker.InvalidTargetDesignArea)]
    [InlineData("missing-area", MockupTemplateReadinessBlocker.MissingTargetDesignArea)]
    [InlineData("no-variants", MockupTemplateReadinessBlocker.MissingCompatibleVariants)]
    public async Task IncompleteLocalConfigurationIsRejectedWithRelevantBlockers(string change, MockupTemplateReadinessBlocker expected)
    {
        var snapshot = CreateSnapshot();
        var first = snapshot.MockupTemplateSourceImages[0];
        var duplicate = first with { Id = Guid.NewGuid() };
        snapshot = change switch
        {
            "missing-image" => snapshot with { MockupTemplateSourceImages = [first] },
            "ambiguous" => snapshot with
            {
                MockupTemplateSourceImages = [.. snapshot.MockupTemplateSourceImages, duplicate],
                MockupTemplateSourceImageOptionValues = [.. snapshot.MockupTemplateSourceImageOptionValues,
                    .. snapshot.MockupTemplateSourceImageOptionValues.Where(value => value.SourceImageId == first.Id)
                        .Select(value => new MockupTemplateSourceImageOptionValue(duplicate.Id, value.OptionValueId))]
            },
            "mapping" => snapshot with { MockupTemplateSourceImages = [first with { ImageMapping = null }, snapshot.MockupTemplateSourceImages[1]] },
            "applicability" => snapshot with { MockupTemplateSourceImageOptionValues = snapshot.MockupTemplateSourceImageOptionValues.Where(value => value.SourceImageId != first.Id).ToArray() },
            "archived-option" => snapshot with { OfferingOptions = snapshot.OfferingOptions.Select(value => value with { IsArchived = true }).ToArray() },
            "archived-value" => snapshot with { OfferingOptionValues = snapshot.OfferingOptionValues.Select(value => value with { IsArchived = true }).ToArray() },
            "archived-images" => snapshot with { MockupTemplateSourceImages = snapshot.MockupTemplateSourceImages.Select(value => value with { IsArchived = true }).ToArray() },
            "archived-template" => snapshot with { MockupTemplates = [snapshot.MockupTemplates.Single() with { IsArchived = true }] },
            "archived-area" => snapshot with { OfferingPlaceholders = [snapshot.OfferingPlaceholders.Single() with { IsArchived = true }] },
            "missing-area" => snapshot with { MockupTemplates = [snapshot.MockupTemplates.Single() with { TargetPlaceholderId = null }] },
            "no-variants" => snapshot with { OfferingVariants = [] },
            _ => throw new ArgumentOutOfRangeException(nameof(change))
        };
        var offering = snapshot.BlueprintOfferings.Single();
        var result = await new MockupTemplateSetupService(new MemoryRepository(snapshot)).GetEligibleTemplatesAsync(
            offering.StoreId, offering.Id, snapshot.MockupTemplates.Single().Id, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Empty(result.Templates);
        Assert.Contains(expected, result.Blockers);
        Assert.DoesNotContain(MockupTemplateReadinessBlocker.MissingColors, result.Blockers);
    }

    [Fact]
    public async Task ColorOnlyApplicabilityCoversAllSizesAndIgnoresArchivedImages()
    {
        var snapshot = CreateSnapshot();
        var colorIds = snapshot.OfferingOptionValues.Where(value => snapshot.OfferingOptions.Any(option => option.Id == value.OptionId && option.OptionKind == OptionKind.Color))
            .Select(value => value.Id).ToHashSet();
        snapshot = snapshot with
        {
            MockupTemplateSourceImages = [.. snapshot.MockupTemplateSourceImages, snapshot.MockupTemplateSourceImages[0] with { Id = Guid.NewGuid(), IsArchived = true, ImageMapping = null }],
            MockupTemplateSourceImageOptionValues = snapshot.MockupTemplateSourceImageOptionValues.Where(value => colorIds.Contains(value.OptionValueId)).ToArray()
        };
        var offering = snapshot.BlueprintOfferings.Single();
        var result = await new MockupTemplateSetupService(new MemoryRepository(snapshot)).GetEligibleTemplatesAsync(
            offering.StoreId, offering.Id, snapshot.MockupTemplates.Single().Id, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Single(result.Templates);
    }

    private sealed class MemoryRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(snapshot);
        public Task SaveAsync(WorkspaceSnapshot value, CancellationToken cancellationToken = default) { snapshot = value; return Task.CompletedTask; }
    }

    private sealed class StubCompositor : IMockupRasterCompositor
    {
        public Task<Stream> ComposeAsync(Stream template, Stream design, MockupImageSpaceMapping mapping, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream([1, 2, 3]));
    }

    private sealed class MemoryFileStore : IWorkspaceFileStore
    {
        public string WorkspaceRoot => "unused";
        public int SaveCount { get; private set; }
        public List<string> SavedFileNames { get; } = [];
        public bool Exists(string workspaceRelativePath) => true;
        public bool TryDelete(string workspaceRelativePath) => true;
        public Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream([1]));
        public Task<ManagedWorkspaceFile> SaveAsync(string fileName, AssetKind kind, Stream content, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            SavedFileNames.Add(fileName);
            return Task.FromResult(new ManagedWorkspaceFile(fileName, kind, $"assets/output-{SaveCount}.png", "unused", "unused"));
        }
        public Task<ManagedWorkspaceFile> ImportAsync(string sourcePath, AssetKind kind, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
