using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Domain.Tests.Mockups;

public sealed class MockupTemplateCoveragePlannerTests
{
    [Fact]
    public void ColorFirstGroupsMissingVariantsAndLeavesSecondaryOptionsUnrestricted()
    {
        var fixture = CreateFixture();

        var plan = MockupTemplateCoveragePlanner.Plan(
            fixture.TemplateId,
            fixture.Area.Id,
            fixture.Variants,
            [],
            [],
            fixture.Options,
            fixture.OptionValues);

        Assert.Equal(4, plan.VariantCount);
        Assert.Equal(4, plan.MissingCount);
        Assert.Equal(2, plan.Requirements.Count);
        Assert.All(plan.Requirements, requirement =>
        {
            Assert.Equal(MockupTemplateCoverageStatus.Missing, requirement.Status);
            Assert.Contains(requirement.Applicability, value => value.Kind == OptionKind.Color);
            Assert.DoesNotContain(requirement.Applicability, value => value.Kind == OptionKind.Size);
        });
    }

    [Fact]
    public void ColorAndSizeStrategyCreatesFinerRequirements()
    {
        var fixture = CreateFixture();

        var plan = MockupTemplateCoveragePlanner.Plan(
            fixture.TemplateId,
            fixture.Area.Id,
            fixture.Variants,
            [],
            [],
            fixture.Options,
            fixture.OptionValues,
            MockupTemplateCoverageGroupingStrategy.ColorAndSize);

        Assert.Equal(4, plan.Requirements.Count);
        Assert.All(plan.Requirements, requirement => Assert.Contains(requirement.Applicability, value => value.Kind == OptionKind.Size));
    }

    [Fact]
    public void IncompleteApplicableSourceIsReportedSeparatelyFromMissingCoverage()
    {
        var fixture = CreateFixture();
        var incomplete = new MockupTemplateSourceImage(
            Guid.NewGuid(), fixture.TemplateId, Guid.NewGuid(), null, false, fixture.Now, fixture.Now, 100, 100);

        var plan = MockupTemplateCoveragePlanner.Plan(
            fixture.TemplateId,
            fixture.Area.Id,
            fixture.Variants,
            [incomplete],
            [new MockupTemplateSourceImageOptionValue(incomplete.Id, fixture.Black.Id)],
            fixture.Options,
            fixture.OptionValues);

        var blackRequirements = plan.Requirements.Where(value => value.Applicability.Any(option => option.Id == fixture.Black.Id)).ToArray();
        Assert.Single(blackRequirements);
        Assert.All(blackRequirements, requirement => Assert.Equal(MockupTemplateCoverageStatus.Incomplete, requirement.Status));
        Assert.Contains(incomplete.Id, plan.IncompleteSourceImageIds);
        Assert.False(plan.IsComplete);
    }

    [Fact]
    public void NoTargetDesignAreaDoesNotCreatePlaceholderRequirements()
    {
        var fixture = CreateFixture();

        var plan = MockupTemplateCoveragePlanner.Plan(
            fixture.TemplateId,
            null,
            fixture.Variants,
            [],
            [],
            fixture.Options,
            fixture.OptionValues);

        Assert.False(plan.HasTargetDesignArea);
        Assert.Empty(plan.Requirements);
        Assert.Contains("target Design Area", plan.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ContextFingerprintDetectsCatalogChangesWithoutChangingPersistedRows()
    {
        var fixture = CreateFixture();
        var plan = MockupTemplateCoveragePlanner.Plan(fixture.TemplateId, fixture.Area.Id, fixture.Variants, [], [], fixture.Options, fixture.OptionValues);
        var changedContext = new MockupTemplateCoverageContext(
            fixture.TemplateId,
            fixture.Area.Id,
            fixture.Variants.Skip(1).Select(value => value.Id).ToArray(),
            fixture.OptionValues.Select(value => value.Id).ToArray());

        Assert.False(plan.IsStaleAgainst(new MockupTemplateCoverageContext(fixture.TemplateId, fixture.Area.Id, fixture.Variants.Select(value => value.Id).ToArray(), fixture.OptionValues.Select(value => value.Id).ToArray())));
        Assert.True(plan.IsStaleAgainst(changedContext));
    }

    private static Fixture CreateFixture()
    {
        var now = DateTimeOffset.UtcNow;
        var offeringId = Guid.NewGuid();
        var colorOption = new OfferingOption(Guid.NewGuid(), offeringId, OptionKind.Color, "Color", 0);
        var sizeOption = new OfferingOption(Guid.NewGuid(), offeringId, OptionKind.Size, "Size", 1);
        var black = new OfferingOptionValue(Guid.NewGuid(), colorOption.Id, offeringId, "Black", 0);
        var white = new OfferingOptionValue(Guid.NewGuid(), colorOption.Id, offeringId, "White", 1);
        var medium = new OfferingOptionValue(Guid.NewGuid(), sizeOption.Id, offeringId, "M", 0);
        var large = new OfferingOptionValue(Guid.NewGuid(), sizeOption.Id, offeringId, "L", 1);
        var variants = new[]
        {
            new OfferingVariant(Guid.NewGuid(), offeringId, "Black M", [black.Id, medium.Id], false, now, now),
            new OfferingVariant(Guid.NewGuid(), offeringId, "Black L", [black.Id, large.Id], false, now, now),
            new OfferingVariant(Guid.NewGuid(), offeringId, "White M", [white.Id, medium.Id], false, now, now),
            new OfferingVariant(Guid.NewGuid(), offeringId, "White L", [white.Id, large.Id], false, now, now)
        };
        var area = new OfferingPlaceholder(Guid.NewGuid(), offeringId, "Front", null, "front", "DTG", 1200, 1200, variants.Select(value => value.Id).ToArray(), false, now, now);
        return new(Guid.NewGuid(), area, [colorOption, sizeOption], [black, white, medium, large], variants, black, now);
    }

    private sealed record Fixture(
        Guid TemplateId,
        OfferingPlaceholder Area,
        IReadOnlyList<OfferingOption> Options,
        IReadOnlyList<OfferingOptionValue> OptionValues,
        IReadOnlyList<OfferingVariant> Variants,
        OfferingOptionValue Black,
        DateTimeOffset Now);
}
