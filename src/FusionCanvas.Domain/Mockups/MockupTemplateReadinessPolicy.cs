using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.Domain.Mockups;

public static class MockupTemplateReadinessPolicy
{
    public static MockupTemplateReadinessResult Evaluate(MockupTemplateReadinessContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var blockers = new List<MockupTemplateReadinessBlocker>();
        var template = context.Template;

        if (template.IsArchived)
            blockers.Add(MockupTemplateReadinessBlocker.Archived);

        OfferingPlaceholder? target = null;
        if (template.TargetPlaceholderId is null)
        {
            blockers.Add(MockupTemplateReadinessBlocker.MissingTargetDesignArea);
        }
        else
        {
            target = context.DesignAreas.SingleOrDefault(value =>
                value.Id == template.TargetPlaceholderId &&
                value.OfferingId == template.BlueprintOfferingId &&
                !value.IsArchived);
            if (target is null)
                blockers.Add(MockupTemplateReadinessBlocker.InvalidTargetDesignArea);
        }

        // Local templates keep applicability and placement on their source images.
        // Retained archived entries also identify a local template with no active images.
        var sourceImages = context.SourceImages?.Where(value => value.MockupTemplateId == template.Id).ToArray() ?? [];
        if (sourceImages.Length > 0)
        {
            EvaluateLocalSources(context, target, sourceImages, blockers);
            return new(blockers);
        }

        var requestedColors = context.ActiveColorOptionValueIds.Distinct().ToArray();
        var validColorIds = context.OptionValues
            .Where(value => requestedColors.Contains(value.Id)
                && value.OfferingId == template.BlueprintOfferingId
                && !value.IsArchived
                && context.Options.Any(option => option.Id == value.OptionId
                    && option.OfferingId == template.BlueprintOfferingId
                    && option.OptionKind == OptionKind.Color
                    && !option.IsArchived))
            .Select(value => value.Id)
            .ToHashSet();

        if (requestedColors.Length == 0)
            blockers.Add(MockupTemplateReadinessBlocker.MissingColors);
        else if (validColorIds.Count != requestedColors.Length)
            blockers.Add(MockupTemplateReadinessBlocker.InvalidColors);

        var impliedVariants = context.Variants
            .Where(value => value.OfferingId == template.BlueprintOfferingId
                && !value.IsArchived
                && value.OptionValueIds.Any(validColorIds.Contains))
            .ToArray();
        if (requestedColors.Length > 0 && validColorIds.Count == requestedColors.Length && impliedVariants.Length == 0)
            blockers.Add(MockupTemplateReadinessBlocker.MissingCompatibleVariants);
        if (target is not null && impliedVariants.Any(value => !target.VariantIds.Contains(value.Id)))
            blockers.Add(MockupTemplateReadinessBlocker.IncompatibleVariants);

        if (string.IsNullOrWhiteSpace(context.Revision.ProviderMockupReference))
            blockers.Add(MockupTemplateReadinessBlocker.MissingImage);
        if (context.Revision.ImageMapping is null)
            blockers.Add(MockupTemplateReadinessBlocker.MissingMapping);

        if (context.KnownSupportedColorOptionValueIds is not null
            && validColorIds.Any(value => !context.KnownSupportedColorOptionValueIds.Contains(value)))
            blockers.Add(MockupTemplateReadinessBlocker.KnownImageColorIncompatibility);

        return new MockupTemplateReadinessResult(blockers);
    }

    private static void EvaluateLocalSources(
        MockupTemplateReadinessContext context,
        OfferingPlaceholder? target,
        IReadOnlyList<MockupTemplateSourceImage> sourceImages,
        List<MockupTemplateReadinessBlocker> blockers)
    {
        var images = sourceImages.Where(value => !value.IsArchived).ToArray();
        var conditions = (context.SourceImageOptionValues ?? [])
            .Where(value => images.Any(image => image.Id == value.SourceImageId)).ToArray();
        var validValues = context.OptionValues.Where(value => value.OfferingId == context.Template.BlueprintOfferingId
            && !value.IsArchived && context.Options.Any(option => option.Id == value.OptionId
                && option.OfferingId == context.Template.BlueprintOfferingId && !option.IsArchived)).ToArray();
        var validIds = validValues.Select(value => value.Id).ToHashSet();

        if (images.Length == 0) blockers.Add(MockupTemplateReadinessBlocker.MissingImage);
        if (images.Any(image => image.ImageMapping is null)) blockers.Add(MockupTemplateReadinessBlocker.MissingMapping);
        if (images.Any(image => !conditions.Any(value => value.SourceImageId == image.Id)))
            blockers.Add(MockupTemplateReadinessBlocker.MissingSourceApplicability);
        if (conditions.Any(value => !validIds.Contains(value.OptionValueId)))
            blockers.Add(MockupTemplateReadinessBlocker.InvalidSourceApplicability);

        if (target is null) return;
        var variants = context.Variants.Where(value => value.OfferingId == context.Template.BlueprintOfferingId
            && !value.IsArchived && target.VariantIds.Contains(value.Id)).ToArray();
        if (variants.Length == 0) blockers.Add(MockupTemplateReadinessBlocker.MissingCompatibleVariants);
        var validImages = images.Where(image => conditions.Where(value => value.SourceImageId == image.Id)
            .All(value => validIds.Contains(value.OptionValueId))).ToArray();
        var resolutions = MockupTemplateSourcePolicy.Resolve(variants, validImages, conditions, validValues);
        if (resolutions.Any(value => value.Kind == MockupTemplateSourceResolutionKind.Missing))
            blockers.Add(MockupTemplateReadinessBlocker.MissingVariantSourceImage);
        if (resolutions.Any(value => value.Kind == MockupTemplateSourceResolutionKind.Ambiguous))
            blockers.Add(MockupTemplateReadinessBlocker.AmbiguousVariantSourceImages);
    }
}
