using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Domain.Tests.Catalog;

public sealed class CatalogModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ProviderNetwork_UsesStableCodeAndNoPrintProvider()
    {
        var storeId = Guid.NewGuid();
        var blueprint = new Blueprint(Guid.NewGuid(), storeId, "T-shirt", null, false, Now, Now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, storeId, "Choice", null, BlueprintOfferingKind.ProviderNetwork, null, "Printify-Choice", null, null, false, Now, Now);

        Assert.Equal("printify-choice", offering.ProviderNetworkCode);
        Assert.Null(offering.PrintProviderId);
        Assert.True(offering.IsProviderNetwork);
    }

    [Fact]
    public void MockupColorBinding_RejectsNonColorValues()
    {
        var offeringId = Guid.NewGuid();
        var template = new MockupTemplate(Guid.NewGuid(), offeringId, Guid.NewGuid(), "Front", null, 1, false, Now, Now);
        var option = new OfferingOption(Guid.NewGuid(), offeringId, OptionKind.Size, "Size", 0);
        var value = new OfferingOptionValue(Guid.NewGuid(), option.Id, offeringId, "Large", 0);

        Assert.Throws<InvalidOperationException>(() => CatalogRelationshipPolicy.ValidateMockupTemplateColor(offeringId, value, option, template));
    }

    [Fact]
    public void MockupColorBinding_IsIndependentOfConcreteSizes()
    {
        var offeringId = Guid.NewGuid();
        var colorOption = new OfferingOption(Guid.NewGuid(), offeringId, OptionKind.Color, "Color", 0);
        var color = new OfferingOptionValue(Guid.NewGuid(), colorOption.Id, offeringId, "Black", 0);
        var template = new MockupTemplate(Guid.NewGuid(), offeringId, Guid.NewGuid(), "Front", null, 1, false, Now, Now);
        var firstSize = new OfferingOption(Guid.NewGuid(), offeringId, OptionKind.Size, "Size", 1);
        var small = new OfferingOptionValue(Guid.NewGuid(), firstSize.Id, offeringId, "Small", 0);
        var large = new OfferingOptionValue(Guid.NewGuid(), firstSize.Id, offeringId, "Large", 1);

        var smallVariant = new OfferingVariant(Guid.NewGuid(), offeringId, "Black / Small", [color.Id, small.Id], false, Now, Now);
        var largeVariant = new OfferingVariant(Guid.NewGuid(), offeringId, "Black / Large", [color.Id, large.Id], false, Now, Now);
        CatalogRelationshipPolicy.ValidateMockupTemplateColor(offeringId, color, colorOption, template);

        Assert.Contains(color.Id, smallVariant.OptionValueIds);
        Assert.Contains(color.Id, largeVariant.OptionValueIds);
    }

    [Fact]
    public void ActiveTemplateColors_AreUniqueByTemplateAndColor()
    {
        var templateId = Guid.NewGuid();
        var colorId = Guid.NewGuid();
        var first = new MockupTemplateColorVariant(Guid.NewGuid(), templateId, colorId, false, Now, Now);
        var second = new MockupTemplateColorVariant(Guid.NewGuid(), templateId, colorId, false, Now, Now);

        Assert.Throws<InvalidOperationException>(() => MockupTemplatePolicy.EnsureUniqueActiveColor([first, second]));
    }

    [Fact]
    public void ReferencedPlaceholderAndColorPreferArchivalHandling()
    {
        var offeringId = Guid.NewGuid();
        var placeholderId = Guid.NewGuid();
        var colorId = Guid.NewGuid();
        var template = new MockupTemplate(Guid.NewGuid(), offeringId, placeholderId, "Front", null, 1, false, Now, Now);
        var binding = new MockupTemplateColorVariant(Guid.NewGuid(), template.Id, colorId, false, Now, Now);

        Assert.Contains("referenced", CatalogLifecyclePolicy.PlaceholderDeletionBlocker(placeholderId, [template]));
        Assert.Contains("referenced", CatalogLifecyclePolicy.ColorValueRetirementBlocker(colorId, [binding]));
    }

    [Fact]
    public void PlaceholderRequiresPositiveDimensionsAndCanCoverSelectedVariants()
    {
        var offeringId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var placeholder = new OfferingPlaceholder(Guid.NewGuid(), offeringId, "Front", null, "front", "DTG", 1200, 1400, [variantId], false, Now, Now);

        Assert.True(CatalogLifecyclePolicy.CoversSelectedVariants(placeholder, [variantId]));
        Assert.False(CatalogLifecyclePolicy.CoversSelectedVariants(placeholder, [variantId, Guid.NewGuid()]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OfferingPlaceholder(Guid.NewGuid(), offeringId, "Front", null, "front", "DTG", 0, 1400, [], false, Now, Now));
    }

    [Fact]
    public void TemplateTargetMustRemainOnTheSameOffering()
    {
        var offeringId = Guid.NewGuid();
        var template = new MockupTemplate(Guid.NewGuid(), offeringId, Guid.NewGuid(), "Front", null, 1, false, Now, Now);
        var matching = new OfferingPlaceholder(template.TargetPlaceholderId!.Value, offeringId, "Front", null, "front", "DTG", 1200, 1400, [], false, Now, Now);
        var different = matching with { OfferingId = Guid.NewGuid() };

        CatalogLifecyclePolicy.EnsureStableTemplateTarget(template, matching);
        Assert.Throws<InvalidOperationException>(() => CatalogLifecyclePolicy.EnsureStableTemplateTarget(template, different));
    }

    [Fact]
    public void RevisionPolicyDetectsOutputAffectingTargetOrColorChanges()
    {
        var placeholder = Guid.NewGuid();
        var color = Guid.NewGuid();

        Assert.False(MockupTemplatePolicy.IsOutputAffectingChange(placeholder, placeholder, new HashSet<Guid> { color }, new HashSet<Guid> { color }));
        Assert.True(MockupTemplatePolicy.IsOutputAffectingChange(placeholder, Guid.NewGuid(), new HashSet<Guid> { color }, new HashSet<Guid> { color }));
        Assert.True(MockupTemplatePolicy.IsOutputAffectingChange(placeholder, placeholder, new HashSet<Guid> { color }, new HashSet<Guid> { Guid.NewGuid() }));
    }

    [Fact]
    public void DesignAreaGuidanceKeepsPixelsAuthoritativeAndDerivesPhysicalSize()
    {
        var guidance = new DesignAreaArtworkGuidance(4500, 5400, 300, "PNG", "Transparent background");
        var placeholder = new OfferingPlaceholder(
            Guid.NewGuid(), Guid.NewGuid(), "Front", null, "front", "DTG", 4500, 5400, [], false, Now, Now,
            providerReference: "print-area-front", artworkGuidance: guidance);

        Assert.Equal(4500, placeholder.Width);
        Assert.Equal("print-area-front", placeholder.ProviderReference);
        Assert.Equal("PNG", placeholder.ArtworkGuidance!.FileFormat);
        Assert.Equal(15, placeholder.MaximumPhysicalSize!.Value.WidthInches, 6);
        Assert.Equal(381, placeholder.MaximumPhysicalSize.Value.WidthMillimetres, 6);
    }

    [Fact]
    public void DesignAreaGuidanceDoesNotInventPhysicalSizeWithoutDpi()
    {
        var guidance = new DesignAreaArtworkGuidance(4500, 5400, fileFormat: "PNG");
        var placeholder = new OfferingPlaceholder(
            Guid.NewGuid(), Guid.NewGuid(), "Front", null, "front", "DTG", 4500, 5400, [], false, Now, Now,
            artworkGuidance: guidance);

        Assert.Null(placeholder.MaximumPhysicalSize);
        Assert.Throws<ArgumentException>(() => new DesignAreaArtworkGuidance(4500, null));
    }

    [Fact]
    public void ImageSpaceMappingRequiresPositiveContainedRectangle()
    {
        var mapping = new MockupImageSpaceMapping(2000, 2000, 620, 480, 700, 910);

        Assert.Equal(620, mapping.X);
        Assert.Equal(910, mapping.Height);
        Assert.Throws<ArgumentOutOfRangeException>(() => new MockupImageSpaceMapping(2000, 2000, 1500, 0, 600, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MockupImageSpaceMapping(2000, 2000, 0, -1, 100, 100));
    }

    [Fact]
    public void RevisionOwnsProviderImageMappingAsSnapshotConfiguration()
    {
        var mapping = new MockupImageSpaceMapping(2000, 2000, 620, 480, 700, 910);
        var revision = new MockupTemplateRevision(Guid.NewGuid(), Guid.NewGuid(), 2, Guid.NewGuid(), Now, "Mapped", "printify-mockup-123", mapping);

        Assert.Equal("printify-mockup-123", revision.ProviderMockupReference);
        Assert.Equal(mapping, revision.ImageMapping);
        Assert.Null(new MockupTemplateRevision(Guid.NewGuid(), Guid.NewGuid(), 2, null, Now, providerMockupReference: "image-only").ImageMapping);
    }

    [Fact]
    public void RevisionPolicyDetectsProviderImageOrMappingChanges()
    {
        var placeholder = Guid.NewGuid();
        var colors = new HashSet<Guid> { Guid.NewGuid() };
        var mapping = new MockupImageSpaceMapping(2000, 2000, 620, 480, 700, 910);
        var moved = new MockupImageSpaceMapping(2000, 2000, 621, 480, 700, 910);

        Assert.False(MockupTemplatePolicy.IsOutputAffectingChange(placeholder, placeholder, colors, colors, "mockup", "mockup", mapping, mapping));
        Assert.True(MockupTemplatePolicy.IsOutputAffectingChange(placeholder, placeholder, colors, colors, "mockup", "other", mapping, mapping));
        Assert.True(MockupTemplatePolicy.IsOutputAffectingChange(placeholder, placeholder, colors, colors, "mockup", "mockup", mapping, moved));
    }

    [Fact]
    public void NameOnlyTemplateAndRevision_AreValidDraftState()
    {
        var offeringId = Guid.NewGuid();
        var template = new MockupTemplate(Guid.NewGuid(), offeringId, null, "  Front draft  ", null, 1, false, Now, Now);
        var revision = new MockupTemplateRevision(Guid.NewGuid(), template.Id, 1, null, Now);

        Assert.Equal("Front draft", template.Name);
        Assert.Null(template.TargetPlaceholderId);
        Assert.Null(revision.TargetPlaceholderId);
        Assert.Null(revision.ProviderMockupReference);
        Assert.Null(revision.ImageMapping);
        Assert.Throws<ArgumentException>(() => new MockupTemplate(Guid.NewGuid(), offeringId, null, " ", null, 1, false, Now, Now));
    }

    [Fact]
    public void ReadinessPolicy_AccumulatesOrderedDraftBlockers()
    {
        var template = new MockupTemplate(Guid.NewGuid(), Guid.NewGuid(), null, "Draft", null, 1, false, Now, Now);
        var revision = new MockupTemplateRevision(Guid.NewGuid(), template.Id, 1, null, Now);

        var result = MockupTemplateReadinessPolicy.Evaluate(new(template, revision, [], [], [], [], []));

        Assert.Equal(MockupTemplateLifecycle.Draft, result.Lifecycle);
        Assert.Equal([
            MockupTemplateReadinessBlocker.MissingTargetDesignArea,
            MockupTemplateReadinessBlocker.MissingColors,
            MockupTemplateReadinessBlocker.MissingImage,
            MockupTemplateReadinessBlocker.MissingMapping], result.Blockers);
    }

    [Fact]
    public void ReadinessPolicy_RecognizesCompleteTemplateAndCatalogRegression()
    {
        var offeringId = Guid.NewGuid();
        var colorOption = new OfferingOption(Guid.NewGuid(), offeringId, OptionKind.Color, "Color", 0);
        var color = new OfferingOptionValue(Guid.NewGuid(), colorOption.Id, offeringId, "Black", 0);
        var variant = new OfferingVariant(Guid.NewGuid(), offeringId, "Black", [color.Id], false, Now, Now);
        var area = new OfferingPlaceholder(Guid.NewGuid(), offeringId, "Front", null, "front", "DTG", 1200, 1200, [variant.Id], false, Now, Now);
        var template = new MockupTemplate(Guid.NewGuid(), offeringId, area.Id, "Ready", null, 1, false, Now, Now);
        var revision = new MockupTemplateRevision(Guid.NewGuid(), template.Id, 1, area.Id, Now,
            providerMockupReference: "front", imageMapping: new MockupImageSpaceMapping(1200, 1200, 100, 100, 800, 800));
        var context = new MockupTemplateReadinessContext(template, revision, [color.Id], [colorOption], [color], [variant], [area], new HashSet<Guid> { color.Id });

        Assert.True(MockupTemplateReadinessPolicy.Evaluate(context).IsReadyForUse);

        var regressed = MockupTemplateReadinessPolicy.Evaluate(context with { DesignAreas = [area with { IsArchived = true }] });
        Assert.Equal([MockupTemplateReadinessBlocker.InvalidTargetDesignArea], regressed.Blockers);
    }

    [Fact]
    public void ReadinessPolicy_ReportsCompatibilityArchiveAndKnownImageBlockers()
    {
        var offeringId = Guid.NewGuid();
        var option = new OfferingOption(Guid.NewGuid(), offeringId, OptionKind.Color, "Color", 0);
        var color = new OfferingOptionValue(Guid.NewGuid(), option.Id, offeringId, "Black", 0);
        var variant = new OfferingVariant(Guid.NewGuid(), offeringId, "Black", [color.Id], false, Now, Now);
        var area = new OfferingPlaceholder(Guid.NewGuid(), offeringId, "Front", null, "front", "DTG", 1000, 1000, [], false, Now, Now);
        var template = new MockupTemplate(Guid.NewGuid(), offeringId, area.Id, "Archived", null, 1, true, Now, Now);
        var revision = new MockupTemplateRevision(Guid.NewGuid(), template.Id, 1, area.Id, Now,
            providerMockupReference: "front", imageMapping: new MockupImageSpaceMapping(1000, 1000, 0, 0, 500, 500));

        var result = MockupTemplateReadinessPolicy.Evaluate(new(template, revision, [color.Id], [option], [color], [variant], [area], new HashSet<Guid>()));

        Assert.Equal([
            MockupTemplateReadinessBlocker.Archived,
            MockupTemplateReadinessBlocker.IncompatibleVariants,
            MockupTemplateReadinessBlocker.KnownImageColorIncompatibility], result.Blockers);
    }
}
