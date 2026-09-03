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
}
