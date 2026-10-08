using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Products;

namespace FusionCanvas.Domain.Tests.Products;

public class VariantMigrationMatcherTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MatchExactVariants_NormalizesCompleteSemanticOptionsAndIgnoresOrder()
    {
        var (sourceOffering, targetOffering, sourceVariant, targetVariant, options, values) = CreatePair("Size", "M", " size ", "m");

        var result = VariantMigrationMatcher.MatchExactVariants([sourceVariant], [targetVariant], options, values);

        Assert.Equal(targetVariant.Id, Assert.Single(result).Key);
        Assert.Equal(sourceVariant.Id, result[targetVariant.Id]);
    }

    [Fact]
    public void MatchExactVariants_DoesNotMatchWhenOptionKindOrFullSetDiffers()
    {
        var sourceOffering = Guid.NewGuid();
        var targetOffering = Guid.NewGuid();
        var sourceOption = new OfferingOption(Guid.NewGuid(), sourceOffering, OptionKind.Size, "Size", 0);
        var targetOption = new OfferingOption(Guid.NewGuid(), targetOffering, OptionKind.Color, "Size", 0);
        var sourceValue = new OfferingOptionValue(Guid.NewGuid(), sourceOption.Id, sourceOffering, "M", 0);
        var targetValue = new OfferingOptionValue(Guid.NewGuid(), targetOption.Id, targetOffering, "M", 0);
        var sourceVariant = Variant(sourceOffering, "M", sourceValue.Id);
        var targetVariant = Variant(targetOffering, "M", targetValue.Id);

        Assert.Empty(VariantMigrationMatcher.MatchExactVariants([sourceVariant], [targetVariant], [sourceOption, targetOption], [sourceValue, targetValue]));
    }

    [Fact]
    public void MatchExactVariants_RejectsAmbiguousAndIncompleteSignatures()
    {
        var (sourceOffering, targetOffering, sourceVariant, targetVariant, options, values) = CreatePair("Size", "M", "Size", "M");
        var duplicateSource = Variant(sourceOffering, "Another label", sourceVariant.OptionValueIds[0]);
        var duplicateTarget = Variant(targetOffering, "Another label", targetVariant.OptionValueIds[0]);
        var orphanValueId = Guid.NewGuid();
        var incomplete = Variant(targetOffering, "Incomplete", orphanValueId);

        var result = VariantMigrationMatcher.MatchExactVariants(
            [sourceVariant, duplicateSource], [targetVariant, duplicateTarget, incomplete], options, values);

        Assert.Empty(result);
    }

    [Fact]
    public void MatchExactVariants_RequiresOneToOneAndIgnoresArchivedVariants()
    {
        var (sourceOffering, targetOffering, sourceVariant, targetVariant, options, values) = CreatePair("Size", "M", "Size", "M");
        var archivedSource = Variant(sourceOffering, "Archived", sourceVariant.OptionValueIds[0]) with { IsArchived = true };

        var result = VariantMigrationMatcher.MatchExactVariants([sourceVariant, archivedSource], [targetVariant], options, values);

        Assert.Equal(sourceVariant.Id, result[targetVariant.Id]);
    }

    private static (Guid, Guid, OfferingVariant, OfferingVariant, OfferingOption[], OfferingOptionValue[]) CreatePair(
        string sourceOptionName, string sourceValueText, string destinationOptionName, string destinationValueText)
    {
        var sourceOffering = Guid.NewGuid();
        var destinationOffering = Guid.NewGuid();
        var sourceOption = new OfferingOption(Guid.NewGuid(), sourceOffering, OptionKind.Size, sourceOptionName, 0);
        var destinationOption = new OfferingOption(Guid.NewGuid(), destinationOffering, OptionKind.Size, destinationOptionName, 0);
        var sourceValue = new OfferingOptionValue(Guid.NewGuid(), sourceOption.Id, sourceOffering, sourceValueText, 0);
        var destinationValue = new OfferingOptionValue(Guid.NewGuid(), destinationOption.Id, destinationOffering, destinationValueText, 0);
        return (sourceOffering, destinationOffering,
            Variant(sourceOffering, "Source display", sourceValue.Id), Variant(destinationOffering, "Different display", destinationValue.Id),
            [sourceOption, destinationOption], [sourceValue, destinationValue]);
    }

    private static OfferingVariant Variant(Guid offeringId, string name, Guid optionValueId) =>
        new(Guid.NewGuid(), offeringId, name, [optionValueId], false, Now, Now);
}
