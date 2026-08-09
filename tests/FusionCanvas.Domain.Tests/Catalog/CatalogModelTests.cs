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
        var matching = new OfferingPlaceholder(template.TargetPlaceholderId, offeringId, "Front", null, "front", "DTG", 1200, 1400, [], false, Now, Now);
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
}
