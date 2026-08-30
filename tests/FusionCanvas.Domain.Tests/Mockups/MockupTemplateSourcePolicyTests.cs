using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Domain.Tests.Mockups;

public sealed class MockupTemplateSourcePolicyTests
{
    [Fact]
    public void Resolve_UsesAllOptionValuesAndReportsMissingAndAmbiguousVariants()
    {
        var color = Guid.NewGuid();
        var size = Guid.NewGuid();
        var secondColor = Guid.NewGuid();
        var firstVariant = new OfferingVariant(Guid.NewGuid(), Guid.NewGuid(), "Navy XL", [color, size], false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var secondVariant = new OfferingVariant(Guid.NewGuid(), firstVariant.OfferingId, "Black XL", [secondColor, size], false, firstVariant.CreatedAt, firstVariant.UpdatedAt);
        var image = new MockupTemplateSourceImage(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new MockupImageSpaceMapping(100, 100, 0, 0, 100, 100), false, firstVariant.CreatedAt, firstVariant.UpdatedAt);
        var image2 = new MockupTemplateSourceImage(Guid.NewGuid(), image.MockupTemplateId, Guid.NewGuid(), new MockupImageSpaceMapping(100, 100, 0, 0, 100, 100), false, firstVariant.CreatedAt, firstVariant.UpdatedAt);
        var image3 = new MockupTemplateSourceImage(Guid.NewGuid(), image.MockupTemplateId, Guid.NewGuid(), new MockupImageSpaceMapping(100, 100, 0, 0, 100, 100), false, firstVariant.CreatedAt, firstVariant.UpdatedAt);
        var conditions = new[]
        {
            new MockupTemplateSourceImageOptionValue(image.Id, color),
            new MockupTemplateSourceImageOptionValue(image.Id, size),
            new MockupTemplateSourceImageOptionValue(image2.Id, secondColor),
            new MockupTemplateSourceImageOptionValue(image2.Id, size),
            new MockupTemplateSourceImageOptionValue(image3.Id, secondColor),
            new MockupTemplateSourceImageOptionValue(image3.Id, size)
        };

        var results = MockupTemplateSourcePolicy.Resolve([firstVariant, secondVariant], [image, image2, image3], conditions);

        var firstResult = Assert.Single(results, value => value.VariantId == firstVariant.Id);
        var secondResult = Assert.Single(results, value => value.VariantId == secondVariant.Id);
        Assert.Equal(MockupTemplateSourceResolutionKind.Resolved, firstResult.Kind);
        Assert.Equal(image.Id, Assert.Single(firstResult.SourceImageIds));
        Assert.Equal(MockupTemplateSourceResolutionKind.Ambiguous, secondResult.Kind);
        Assert.False(MockupTemplateSourcePolicy.IsReady(results));
    }

    [Fact]
    public void Resolve_ReportsMissingWhenNoConditionMatches()
    {
        var offering = Guid.NewGuid();
        var variant = new OfferingVariant(Guid.NewGuid(), offering, "Navy", [Guid.NewGuid()], false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var image = new MockupTemplateSourceImage(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new MockupImageSpaceMapping(100, 100, 0, 0, 100, 100), false, variant.CreatedAt, variant.UpdatedAt);

        var result = Assert.Single(MockupTemplateSourcePolicy.Resolve([variant], [image], [new MockupTemplateSourceImageOptionValue(image.Id, Guid.NewGuid())]));

        Assert.Equal(MockupTemplateSourceResolutionKind.Missing, result.Kind);
        Assert.Empty(result.SourceImageIds);
    }

    [Fact]
    public void SourceEntities_RejectEmptyIdentityAndInvalidMapping()
    {
        Assert.Throws<ArgumentException>(() => new MockupTemplateSourceImage(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), new MockupImageSpaceMapping(100, 100, 0, 0, 100, 100), false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MockupImageSpaceMapping(100, 100, 90, 0, 20, 20));
    }

    [Fact]
    public void Resolve_UsesOrWithinOptionAndAndAcrossOptions_AndIgnoresIncompleteImages()
    {
        var offering = Guid.NewGuid();
        var colorOption = Guid.NewGuid();
        var sizeOption = Guid.NewGuid();
        var black = new OfferingOptionValue(Guid.NewGuid(), colorOption, offering, "Black", 0);
        var navy = new OfferingOptionValue(Guid.NewGuid(), colorOption, offering, "Navy", 1);
        var medium = new OfferingOptionValue(Guid.NewGuid(), sizeOption, offering, "M", 0);
        var large = new OfferingOptionValue(Guid.NewGuid(), sizeOption, offering, "L", 1);
        var variant = new OfferingVariant(Guid.NewGuid(), offering, "Navy M", [navy.Id, medium.Id], false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var image = new MockupTemplateSourceImage(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new MockupImageSpaceMapping(100, 100, 0, 0, 100, 100), false, variant.CreatedAt, variant.UpdatedAt);
        var incomplete = new MockupTemplateSourceImage(Guid.NewGuid(), image.MockupTemplateId, Guid.NewGuid(), null, false, variant.CreatedAt, variant.UpdatedAt, 100, 100);
        var conditions = new[]
        {
            new MockupTemplateSourceImageOptionValue(image.Id, black.Id),
            new MockupTemplateSourceImageOptionValue(image.Id, navy.Id),
            new MockupTemplateSourceImageOptionValue(image.Id, medium.Id),
            new MockupTemplateSourceImageOptionValue(image.Id, large.Id),
            new MockupTemplateSourceImageOptionValue(incomplete.Id, navy.Id)
        };

        var result = Assert.Single(MockupTemplateSourcePolicy.Resolve([variant], [image, incomplete], conditions, [black, navy, medium, large]));

        Assert.Equal(MockupTemplateSourceResolutionKind.Resolved, result.Kind);
        Assert.Equal(image.Id, Assert.Single(result.SourceImageIds));
    }
}
