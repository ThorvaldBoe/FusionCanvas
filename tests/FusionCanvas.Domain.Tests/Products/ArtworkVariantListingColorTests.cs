using FusionCanvas.Domain.Products;

namespace FusionCanvas.Domain.Tests.Products;

public sealed class ArtworkVariantListingColorTests
{
    [Fact]
    public void ListingColorSelection_RequiresStableColorOrLegacyLabel()
    {
        Assert.Throws<ArgumentException>(() => new ListingColorSelection(
            Guid.NewGuid(), null, Guid.NewGuid(), 0));
    }

    [Fact]
    public void ListingColorSelection_DoesNotAllowResolvedAndLegacyIdentityTogether()
    {
        Assert.Throws<ArgumentException>(() => new ListingColorSelection(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0, "Black"));
    }

    [Fact]
    public void ValidateArtworkVariants_RequiresOneDefaultWhenVariantsExist()
    {
        var itemId = Guid.NewGuid();
        var variants = new[]
        {
            new DesignVariantRow(Guid.NewGuid(), itemId, false, 0),
            new DesignVariantRow(Guid.NewGuid(), itemId, false, 1)
        };

        Assert.Throws<InvalidOperationException>(() => DesignStagePolicy.ValidateArtworkVariants(itemId, variants, []));
    }

    [Fact]
    public void ValidateArtworkVariants_AllowsIndependentVariantsWithoutColorPartitions()
    {
        var itemId = Guid.NewGuid();
        var defaultVariant = new DesignVariantRow(Guid.NewGuid(), itemId, true, 0, ArtworkContrastIntent.LightForDarkProduct);
        var alternateVariant = new DesignVariantRow(Guid.NewGuid(), itemId, false, 1, ArtworkContrastIntent.DarkForLightProduct);

        DesignStagePolicy.ValidateArtworkVariants(itemId, [defaultVariant, alternateVariant], []);

        Assert.Equal(ArtworkContrastIntent.LightForDarkProduct, defaultVariant.ContrastIntent);
        Assert.Equal(ArtworkContrastIntent.DarkForLightProduct, alternateVariant.ContrastIntent);
    }

    [Fact]
    public void ValidateListingColorSelections_RejectsAssignmentToAnotherItemsVariant()
    {
        var itemId = Guid.NewGuid();
        var otherItemVariant = new DesignVariantRow(Guid.NewGuid(), Guid.NewGuid(), true, 0);
        var selection = new ListingColorSelection(itemId, Guid.NewGuid(), otherItemVariant.Id, 0);

        Assert.Throws<InvalidOperationException>(() =>
            DesignStagePolicy.ValidateListingColorSelections(itemId, [selection], [otherItemVariant]));
    }

    [Fact]
    public void ValidateListingColorSelections_RejectsDuplicateStableColorIdentity()
    {
        var itemId = Guid.NewGuid();
        var variant = new DesignVariantRow(Guid.NewGuid(), itemId, true, 0);
        var colorId = Guid.NewGuid();
        var selections = new[]
        {
            new ListingColorSelection(itemId, colorId, variant.Id, 0),
            new ListingColorSelection(itemId, colorId, variant.Id, 1)
        };

        Assert.Throws<InvalidOperationException>(() =>
            DesignStagePolicy.ValidateListingColorSelections(itemId, selections, [variant]));
    }

    [Fact]
    public void ValidateListingColorSelections_AllowsUnresolvedLegacyColorWithArtworkAssignment()
    {
        var itemId = Guid.NewGuid();
        var variant = new DesignVariantRow(Guid.NewGuid(), itemId, true, 0);
        var selection = new ListingColorSelection(itemId, null, variant.Id, 0, "Heather Gray");

        DesignStagePolicy.ValidateListingColorSelections(itemId, [selection], [variant]);

        Assert.False(selection.IsResolved);
        Assert.Equal("Heather Gray", selection.LegacyColorLabel);
    }
}
