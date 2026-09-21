using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Application.Catalog;

public static class CatalogSetupQueries
{
    public static IReadOnlyList<OfferingOption> ActiveOptions(
        IEnumerable<OfferingOption> options,
        Guid? offeringId) =>
        options
            .Where(option => option.OfferingId == offeringId && !option.IsArchived)
            .OrderBy(option => option.SortOrder)
            .ToArray();

    public static IReadOnlyList<OfferingOptionValue> ActiveValues(
        IEnumerable<OfferingOptionValue> values,
        Guid? offeringId,
        Guid? optionId) =>
        values
            .Where(value => value.OfferingId == offeringId
                            && value.OptionId == optionId
                            && !value.IsArchived)
            .OrderBy(value => value.SortOrder)
            .ToArray();

    public static IReadOnlyList<OfferingVariant> ActiveVariants(
        IEnumerable<OfferingVariant> variants,
        Guid? offeringId) =>
        variants
            .Where(variant => variant.OfferingId == offeringId && !variant.IsArchived)
            .ToArray();

    public static IReadOnlyList<OfferingPlaceholder> ActiveDesignAreas(
        IEnumerable<OfferingPlaceholder> designAreas,
        Guid? offeringId) =>
        designAreas
            .Where(area => area.OfferingId == offeringId && !area.IsArchived)
            .ToArray();

    public static IReadOnlyList<MockupTemplate> ActiveTemplates(
        IEnumerable<MockupTemplate> templates,
        Guid? offeringId) =>
        templates
            .Where(template => template.BlueprintOfferingId == offeringId && !template.IsArchived)
            .ToArray();

    public static IReadOnlyList<OfferingOptionValue> ActiveColors(
        IEnumerable<OfferingOptionValue> values,
        IEnumerable<OfferingOption> options,
        Guid? offeringId)
    {
        var colorOptionIds = options
            .Where(option => option.OfferingId == offeringId
                             && !option.IsArchived
                             && option.OptionKind == OptionKind.Color)
            .Select(option => option.Id)
            .ToHashSet();

        return values
            .Where(value => value.OfferingId == offeringId
                            && !value.IsArchived
                            && colorOptionIds.Contains(value.OptionId))
            .OrderBy(value => value.SortOrder)
            .ThenBy(value => value.Id)
            .ToArray();
    }

    public static OfferingSetupCounts CountSetup(
        IEnumerable<OfferingVariant> variants,
        IEnumerable<OfferingPlaceholder> designAreas,
        IEnumerable<MockupTemplate> templates,
        Guid? offeringId) =>
        new(
            ActiveVariants(variants, offeringId).Count,
            ActiveDesignAreas(designAreas, offeringId).Count,
            ActiveTemplates(templates, offeringId).Count);
}
