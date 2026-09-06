using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Domain.Tests.Mockups;

public sealed class MockupTemplateLocalReadinessTests
{
    [Fact]
    public void LocalReadinessUsesOnlyVariantsCompatibleWithTheTargetArea()
    {
        var context = CreateContext();
        var unrelated = new OfferingVariant(Guid.NewGuid(), context.Template.BlueprintOfferingId, "Other", [Guid.NewGuid()],
            false, context.Template.CreatedAt, context.Template.UpdatedAt);

        var result = MockupTemplateReadinessPolicy.Evaluate(context with { Variants = [.. context.Variants, unrelated] });

        Assert.True(result.IsReadyForUse);
        Assert.Empty(result.Blockers);
    }

    [Fact]
    public void UnconfiguredActiveImageKeepsTemplateDraftEvenWhenVariantsAreCovered()
    {
        var context = CreateContext();
        var incomplete = context.SourceImages![0] with { Id = Guid.NewGuid(), ImageMapping = null };

        var result = MockupTemplateReadinessPolicy.Evaluate(context with { SourceImages = [.. context.SourceImages, incomplete] });

        Assert.False(result.IsReadyForUse);
        Assert.Contains(MockupTemplateReadinessBlocker.MissingMapping, result.Blockers);
        Assert.Contains(MockupTemplateReadinessBlocker.MissingSourceApplicability, result.Blockers);
        Assert.DoesNotContain(MockupTemplateReadinessBlocker.MissingVariantSourceImage, result.Blockers);
    }

    [Fact]
    public void SourcesFromAnotherTemplateCannotMakeADraftReady()
    {
        var context = CreateContext();
        var result = MockupTemplateReadinessPolicy.Evaluate(context with
        {
            SourceImages = [context.SourceImages![0] with { MockupTemplateId = Guid.NewGuid() }]
        });

        Assert.False(result.IsReadyForUse);
        Assert.Contains(MockupTemplateReadinessBlocker.MissingImage, result.Blockers);
    }

    private static MockupTemplateReadinessContext CreateContext()
    {
        var now = new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero);
        var offeringId = Guid.NewGuid();
        var option = new OfferingOption(Guid.NewGuid(), offeringId, OptionKind.Color, "Color", 0);
        var color = new OfferingOptionValue(Guid.NewGuid(), option.Id, offeringId, "Black", 0);
        var variant = new OfferingVariant(Guid.NewGuid(), offeringId, "Black", [color.Id], false, now, now);
        var area = new OfferingPlaceholder(Guid.NewGuid(), offeringId, "Front", null, "front", "DTG", 100, 100, [variant.Id], false, now, now);
        var template = new MockupTemplate(Guid.NewGuid(), offeringId, area.Id, "Local", null, 1, false, now, now);
        var image = new MockupTemplateSourceImage(Guid.NewGuid(), template.Id, Guid.NewGuid(), new(100, 100, 0, 0, 100, 100), false, now, now);
        return new(template, new(Guid.NewGuid(), template.Id, 1, area.Id, now), [], [option], [color], [variant], [area],
            SourceImages: [image], SourceImageOptionValues: [new(image.Id, color.Id)]);
    }
}
