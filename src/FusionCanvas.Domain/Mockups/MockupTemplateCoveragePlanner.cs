using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.Domain.Mockups;

public static class MockupTemplateCoveragePlanner
{
    public static MockupTemplateCoveragePlan Plan(
        Guid templateId,
        Guid? targetDesignAreaId,
        IEnumerable<OfferingVariant> compatibleVariants,
        IEnumerable<MockupTemplateSourceImage> sourceImages,
        IEnumerable<MockupTemplateSourceImageOptionValue> conditions,
        IEnumerable<OfferingOption> options,
        IEnumerable<OfferingOptionValue> optionValues,
        MockupTemplateCoverageGroupingStrategy groupingStrategy = MockupTemplateCoverageGroupingStrategy.ColorFirst)
    {
        ArgumentNullException.ThrowIfNull(compatibleVariants);
        ArgumentNullException.ThrowIfNull(sourceImages);
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(optionValues);

        var variants = compatibleVariants.Where(value => !value.IsArchived).OrderBy(value => value.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        var activeImages = sourceImages.Where(value => !value.IsArchived).ToArray();
        var activeConditions = conditions.Where(value => activeImages.Any(image => image.Id == value.SourceImageId)).ToArray();
        var offeringIds = variants.Select(value => value.OfferingId).ToHashSet();
        var activeValues = optionValues.Where(value => !value.IsArchived && offeringIds.Contains(value.OfferingId)).ToDictionary(value => value.Id);
        var optionKinds = options.Where(value => !value.IsArchived && offeringIds.Contains(value.OfferingId)).ToDictionary(value => value.Id, value => value.OptionKind);
        var context = new MockupTemplateCoverageContext(
            templateId,
            targetDesignAreaId,
            variants.Select(value => value.Id).ToArray(),
            activeValues.Keys.ToArray());

        if (targetDesignAreaId is null)
        {
            return new(templateId, null, groupingStrategy, context.Fingerprint, [], [], false,
                "A target Design Area is required before compatible Variant coverage can be derived.");
        }

        if (variants.Length == 0)
            return new(templateId, targetDesignAreaId, groupingStrategy, context.Fingerprint, [], IncompleteSourceIds(activeImages, activeConditions, activeValues, optionKinds), true,
                "The target Design Area has no active compatible Variants.");

        var resolutions = MockupTemplateSourcePolicy.Resolve(variants, activeImages, activeConditions, activeValues.Values);
        var resolutionByVariant = resolutions.ToDictionary(value => value.VariantId);
        var incompleteIds = IncompleteSourceIds(activeImages, activeConditions, activeValues, optionKinds);
        var rows = variants.Select(variant =>
        {
            var resolution = resolutionByVariant[variant.Id];
            var status = resolution.Kind switch
            {
                MockupTemplateSourceResolutionKind.Resolved => MockupTemplateCoverageStatus.Resolved,
                MockupTemplateSourceResolutionKind.Ambiguous => MockupTemplateCoverageStatus.Ambiguous,
                _ when incompleteIds.Any(id => activeImages.Any(image => image.Id == id && IsApplicable(image.Id, variant, activeConditions, activeValues, optionKinds)))
                    => MockupTemplateCoverageStatus.Incomplete,
                _ => MockupTemplateCoverageStatus.Missing
            };
            return new CoverageRow(variant, status, resolution.SourceImageIds);
        }).ToArray();

        var groups = BuildGroups(rows, groupingStrategy, activeValues, optionKinds);
        var requirements = groups.Select(group =>
        {
            var status = group.Rows.Select(value => value.Status).OrderByDescending(value => value).First();
            var matches = group.Rows.SelectMany(value => value.MatchedSourceImageIds).Distinct().OrderBy(value => value).ToArray();
            var explanation = status switch
            {
                MockupTemplateCoverageStatus.Resolved => "Covered by exactly one complete source image.",
                MockupTemplateCoverageStatus.Ambiguous => "More than one complete source image matches; narrow or merge applicability.",
                MockupTemplateCoverageStatus.Incomplete => "A source image matches this coverage but still needs applicability or mapping setup.",
                _ => group.Explanation
            };
            return new MockupTemplateCoverageRequirement(
                group.Key,
                status,
                group.Rows.Select(value => value.Variant.Id).ToArray(),
                group.Rows.Select(value => value.Variant.Name).ToArray(),
                group.Applicability,
                matches,
                explanation);
        }).ToArray();

        return new(templateId, targetDesignAreaId, groupingStrategy, context.Fingerprint, requirements, incompleteIds, true);
    }

    private static IReadOnlyList<Guid> IncompleteSourceIds(
        IReadOnlyList<MockupTemplateSourceImage> images,
        IReadOnlyList<MockupTemplateSourceImageOptionValue> conditions,
        IReadOnlyDictionary<Guid, OfferingOptionValue> values,
        IReadOnlyDictionary<Guid, OptionKind> optionKinds) =>
        images.Where(image => image.ImageMapping is null
            || !conditions.Any(value => value.SourceImageId == image.Id)
            || conditions.Where(value => value.SourceImageId == image.Id).Any(value => !values.ContainsKey(value.OptionValueId) || !optionKinds.ContainsKey(values[value.OptionValueId].OptionId)))
            .Select(value => value.Id)
            .OrderBy(value => value)
            .ToArray();

    private static bool IsApplicable(
        Guid imageId,
        OfferingVariant variant,
        IReadOnlyList<MockupTemplateSourceImageOptionValue> conditions,
        IReadOnlyDictionary<Guid, OfferingOptionValue> values,
        IReadOnlyDictionary<Guid, OptionKind> optionKinds)
    {
        var required = conditions.Where(value => value.SourceImageId == imageId).Select(value => value.OptionValueId).ToArray();
        if (required.Length == 0) return false;
        var variantValues = variant.OptionValueIds.ToHashSet();
        return required.GroupBy(id => values.TryGetValue(id, out var value) && optionKinds.TryGetValue(value.OptionId, out var kind) ? (Guid?)value.OptionId : null)
            .Where(group => group.Key is not null)
            .All(group => group.Any(value => variantValues.Contains(value)));
    }

    private static IReadOnlyList<CoverageGroup> BuildGroups(
        IReadOnlyList<CoverageRow> rows,
        MockupTemplateCoverageGroupingStrategy strategy,
        IReadOnlyDictionary<Guid, OfferingOptionValue> values,
        IReadOnlyDictionary<Guid, OptionKind> optionKinds)
    {
        var colorByVariant = rows.ToDictionary(row => row.Variant.Id, row => OptionIds(row.Variant, OptionKind.Color, values, optionKinds).ToArray());
        var isColorSafe = rows.All(row => colorByVariant[row.Variant.Id].Length == 1);
        if (strategy == MockupTemplateCoverageGroupingStrategy.ColorFirst && isColorSafe)
            return rows.GroupBy(row => colorByVariant[row.Variant.Id][0]).Select(group => CreateGroup(group.ToArray(), values, optionKinds, "This Color-only requirement covers every compatible Size and other option.")).ToArray();

        if (strategy == MockupTemplateCoverageGroupingStrategy.ColorAndSize && isColorSafe)
        {
            var groups = rows.GroupBy(row =>
            {
                var color = colorByVariant[row.Variant.Id][0];
                var size = OptionIds(row.Variant, OptionKind.Size, values, optionKinds).FirstOrDefault();
                return (color, size);
            });
            return groups.Select(group => CreateGroup(group.ToArray(), values, optionKinds, "This requirement is narrowed to Color and Size for predictable coverage.")).ToArray();
        }

        return rows.Select(row => CreateGroup([row], values, optionKinds,
            strategy == MockupTemplateCoverageGroupingStrategy.ColorFirst
                ? "Color-only grouping was unsafe for this Variant matrix, so the plan uses individual Variants."
                : "This individual Variant requirement avoids combining incompatible option values.")).ToArray();
    }

    private static CoverageGroup CreateGroup(
        IReadOnlyList<CoverageRow> rows,
        IReadOnlyDictionary<Guid, OfferingOptionValue> values,
        IReadOnlyDictionary<Guid, OptionKind> optionKinds,
        string explanation)
    {
        var optionIds = rows.SelectMany(row => row.Variant.OptionValueIds)
            .Where(value => values.ContainsKey(value) && optionKinds.ContainsKey(values[value].OptionId))
            .GroupBy(value => values[value].OptionId)
            .Where(group => group.Select(value => values[value].Id).Distinct().Count() == 1)
            .Select(group => group.First())
            .Distinct()
            .OrderBy(value => optionKinds[values[value].OptionId])
            .ThenBy(value => values[value].Value, StringComparer.OrdinalIgnoreCase)
            .Select(value => new MockupTemplateCoverageOptionValue(value, optionKinds[values[value].OptionId], values[value].Value))
            .ToArray();
        var key = string.Join(",", rows.SelectMany(row => row.Variant.OptionValueIds).Distinct().OrderBy(value => value).Select(value => value.ToString("N")));
        return new(key, rows, optionIds, explanation);
    }

    private static IEnumerable<Guid> OptionIds(
        OfferingVariant variant,
        OptionKind kind,
        IReadOnlyDictionary<Guid, OfferingOptionValue> values,
        IReadOnlyDictionary<Guid, OptionKind> optionKinds) =>
        variant.OptionValueIds.Where(value => values.TryGetValue(value, out var optionValue) && optionKinds.TryGetValue(optionValue.OptionId, out var actualKind) && actualKind == kind);

    private sealed record CoverageRow(OfferingVariant Variant, MockupTemplateCoverageStatus Status, IReadOnlyList<Guid> MatchedSourceImageIds);
    private sealed record CoverageGroup(string Key, IReadOnlyList<CoverageRow> Rows, IReadOnlyList<MockupTemplateCoverageOptionValue> Applicability, string Explanation);
}
