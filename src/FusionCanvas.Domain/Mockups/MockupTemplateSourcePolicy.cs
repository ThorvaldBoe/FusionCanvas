using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.Domain.Mockups;

public static class MockupTemplateSourcePolicy
{
    public static IReadOnlyList<MockupTemplateSourceResolution> Resolve(
        IEnumerable<OfferingVariant> compatibleVariants,
        IEnumerable<MockupTemplateSourceImage> sourceImages,
        IEnumerable<MockupTemplateSourceImageOptionValue> conditions) =>
        Resolve(compatibleVariants, sourceImages, conditions, []);

    public static IReadOnlyList<MockupTemplateSourceResolution> Resolve(
        IEnumerable<OfferingVariant> compatibleVariants,
        IEnumerable<MockupTemplateSourceImage> sourceImages,
        IEnumerable<MockupTemplateSourceImageOptionValue> conditions,
        IEnumerable<OfferingOptionValue> optionValues)
    {
        ArgumentNullException.ThrowIfNull(compatibleVariants);
        ArgumentNullException.ThrowIfNull(sourceImages);
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentNullException.ThrowIfNull(optionValues);

        var activeImages = sourceImages.Where(image => !image.IsArchived).OrderBy(image => image.Id).ToArray();
        var conditionsByImage = conditions
            .GroupBy(value => value.SourceImageId)
            .ToDictionary(group => group.Key, group => group.Select(value => value.OptionValueId).ToHashSet());
        var optionByValue = optionValues.ToDictionary(value => value.Id, value => value.OptionId);

        return compatibleVariants
            .Where(variant => !variant.IsArchived)
            .OrderBy(variant => variant.Id)
            .Select(variant =>
            {
                var values = variant.OptionValueIds.ToHashSet();
                var matches = activeImages
                    .Where(image => image.ImageMapping is not null
                        && conditionsByImage.TryGetValue(image.Id, out var required)
                        && required.Count > 0
                        && (optionValues.Any()
                            ? required.GroupBy(id => optionByValue.TryGetValue(id, out var optionId) ? optionId : Guid.Empty)
                                .All(group => group.Key != Guid.Empty && group.Any(value => values.Contains(value)))
                            : required.IsSubsetOf(values)))
                    .Select(image => image.Id)
                    .ToArray();
                var kind = matches.Length switch
                {
                    0 => MockupTemplateSourceResolutionKind.Missing,
                    1 => MockupTemplateSourceResolutionKind.Resolved,
                    _ => MockupTemplateSourceResolutionKind.Ambiguous
                };
                return new MockupTemplateSourceResolution(variant.Id, kind, matches);
            })
            .ToArray();
    }

    public static bool IsReady(IEnumerable<MockupTemplateSourceResolution> resolutions) =>
        resolutions is not null && resolutions.Any() && resolutions.All(value => value.Kind == MockupTemplateSourceResolutionKind.Resolved);
}
