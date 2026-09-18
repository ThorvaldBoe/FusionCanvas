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

public sealed class MockupGenerationApplicabilityRegressionTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Apply_reports_ambiguity_when_each_size_has_a_different_source_for_one_color()
    {
        var fixture = CreateFixture(true); var compositor = new CountingCompositor();
        var result = await fixture.Service(compositor).ApplyAsync(new(fixture.Item.Id, fixture.Template.Id));
        Assert.False(result.Succeeded); Assert.Empty(result.Outputs); var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("Black", diagnostic.ColorValue); Assert.Contains("Multiple template source images", diagnostic.Message); Assert.Equal(0, compositor.Calls);
    }

    [Fact]
    public async Task Apply_generates_one_output_when_one_source_covers_all_sizes_without_color_condition()
    {
        var fixture = CreateFixture(false); var compositor = new CountingCompositor();
        var result = await fixture.Service(compositor).ApplyAsync(new(fixture.Item.Id, fixture.Template.Id));
        Assert.True(result.Succeeded); Assert.Empty(result.Diagnostics); Assert.Single(result.Outputs); Assert.Equal("Black", result.Outputs[0].ColorValue);
        Assert.Equal(1, compositor.Calls); Assert.Equal(1, fixture.Files.SaveCount);
    }

    private static Fixture CreateFixture(bool twoSizeSpecificSources)
    {
        var storeId = Guid.NewGuid(); var offeringId = Guid.NewGuid();
        var blueprint = new Blueprint(Guid.NewGuid(), storeId, "T-shirt", null, false, Now, Now);
        var offering = new BlueprintOffering(offeringId, blueprint.Id, storeId, "Tee", null, BlueprintOfferingKind.ProviderNetwork, null, "manual", null, null, false, Now, Now);
        var colorOption = new OfferingOption(Guid.NewGuid(), offeringId, OptionKind.Color, "Color", 0); var sizeOption = new OfferingOption(Guid.NewGuid(), offeringId, OptionKind.Size, "Size", 1);
        var color = new OfferingOptionValue(Guid.NewGuid(), colorOption.Id, offeringId, "Black", 0); var medium = new OfferingOptionValue(Guid.NewGuid(), sizeOption.Id, offeringId, "M", 0); var large = new OfferingOptionValue(Guid.NewGuid(), sizeOption.Id, offeringId, "L", 1);
        var variants = new[] { new OfferingVariant(Guid.NewGuid(), offeringId, "Black M", [color.Id, medium.Id], false, Now, Now), new OfferingVariant(Guid.NewGuid(), offeringId, "Black L", [color.Id, large.Id], false, Now, Now) };
        var area = new OfferingPlaceholder(Guid.NewGuid(), offeringId, "Front", null, "front", "DTG", 3000, 4500, variants.Select(v => v.Id).ToArray(), false, Now, Now);
        var template = new MockupTemplate(Guid.NewGuid(), offeringId, area.Id, "Flatlay", null, 1, false, Now, Now); var revision = new MockupTemplateRevision(Guid.NewGuid(), template.Id, 1, area.Id, Now);
        var sourceImages = (twoSizeSpecificSources
            ? new[] { Guid.NewGuid(), Guid.NewGuid() }.Select(id => new MockupTemplateSourceImage(Guid.NewGuid(), template.Id, id, new(2000, 2000, 10, 10, 100, 100), false, Now, Now)).ToArray()
            : new[] { new MockupTemplateSourceImage(Guid.NewGuid(), template.Id, Guid.NewGuid(), new(2000, 2000, 10, 10, 100, 100), false, Now, Now) });
        var conditions = twoSizeSpecificSources
            ? new[] { new MockupTemplateSourceImageOptionValue(sourceImages[0].Id, medium.Id), new MockupTemplateSourceImageOptionValue(sourceImages[1].Id, large.Id) }
            : new[] { new MockupTemplateSourceImageOptionValue(sourceImages[0].Id, medium.Id), new MockupTemplateSourceImageOptionValue(sourceImages[0].Id, large.Id) };
        var item = new Item(Guid.NewGuid(), storeId, null, null, "Test listing", null, ItemStatus.Draft, WorkflowStage.Listing, false, Now, Now, "{}"); var design = new Asset(Guid.NewGuid(), storeId, "design.png", null, AssetKind.ExportedImage, "assets/design.png", null, false, false, Now, Now, "{}"); var row = new DesignVariantRow(Guid.NewGuid(), item.Id, true, 0);
        var snapshot = new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)], [new Store(storeId, "Store", null, false, Now, Now, "{}")], [], [], [], [], [], [], [], [])
        {
            Blueprints = [blueprint], BlueprintOfferings = [offering], OfferingOptions = [colorOption, sizeOption], OfferingOptionValues = [color, medium, large], OfferingVariants = variants, OfferingPlaceholders = [area], MockupTemplates = [template], MockupTemplateRevisions = [revision], MockupTemplateSourceImages = sourceImages, MockupTemplateSourceImageOptionValues = conditions,
            MockupTemplateRevisionSourceImages = sourceImages.Select(x => new MockupTemplateRevisionSourceImage(x.Id, revision.Id, x.SourceAssetId, x.ImageMapping)).ToArray(), MockupTemplateRevisionSourceImageOptionValues = conditions.Select(x => new MockupTemplateRevisionSourceImageOptionValue(x.SourceImageId, x.OptionValueId)).ToArray(), Items = [item], ItemListingConfigurations = [new(item.Id, offeringId)], DesignSelectedColors = [new(item.Id, "Black")], DesignVariantRows = [row], DesignVariantRowColors = [new(row.Id, "Black")], DesignSlotAssignments = [new(row.Id, area.Id, design.Id)], Assets = [design, .. sourceImages.Select(x => new Asset(x.SourceAssetId, storeId, "source.png", null, AssetKind.MockupImage, $"assets/{x.Id}.png", null, false, false, Now, Now, "{}"))]
        };
        var files = new MemoryFiles(); return new(snapshot, item, template, files);
    }

    private sealed record Fixture(WorkspaceSnapshot Snapshot, Item Item, MockupTemplate Template, MemoryFiles Files)
    { public MockupGenerationService Service(CountingCompositor compositor) { var repo = new MemoryRepository(Snapshot); return new(repo, Files, new MockupTemplateSetupService(repo), compositor); } }
    private sealed class MemoryRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository { public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(snapshot); public Task SaveAsync(WorkspaceSnapshot value, CancellationToken cancellationToken = default) { snapshot = value; return Task.CompletedTask; } }
    private sealed class CountingCompositor : IMockupRasterCompositor { public int Calls { get; private set; } public Task<Stream> ComposeAsync(Stream template, Stream design, MockupImageSpaceMapping mapping, CancellationToken cancellationToken = default) { Calls++; return Task.FromResult<Stream>(new MemoryStream([1])); } }
    private sealed class MemoryFiles : IWorkspaceFileStore { public string WorkspaceRoot => "unused"; public int SaveCount { get; private set; } public bool Exists(string workspaceRelativePath) => true; public bool TryDelete(string workspaceRelativePath) => true; public Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream([1])); public Task<ManagedWorkspaceFile> SaveAsync(string fileName, AssetKind kind, Stream content, CancellationToken cancellationToken = default) { SaveCount++; return Task.FromResult(new ManagedWorkspaceFile(fileName, kind, $"assets/output-{SaveCount}.png", "unused", "unused")); } public Task<ManagedWorkspaceFile> ImportAsync(string sourcePath, AssetKind kind, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken cancellationToken = default) => throw new NotSupportedException(); }
}
