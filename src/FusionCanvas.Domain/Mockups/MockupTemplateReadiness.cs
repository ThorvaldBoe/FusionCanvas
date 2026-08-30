using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.Domain.Mockups;

public enum MockupTemplateLifecycle
{
    Draft,
    ReadyForUse
}

public enum MockupTemplateReadinessBlocker
{
    Archived,
    MissingTargetDesignArea,
    InvalidTargetDesignArea,
    MissingColors,
    InvalidColors,
    MissingCompatibleVariants,
    IncompatibleVariants,
    MissingImage,
    MissingMapping,
    KnownImageColorIncompatibility
}

public sealed record MockupTemplateReadinessResult(IReadOnlyList<MockupTemplateReadinessBlocker> Blockers)
{
    public MockupTemplateLifecycle Lifecycle => Blockers.Count == 0
        ? MockupTemplateLifecycle.ReadyForUse
        : MockupTemplateLifecycle.Draft;

    public bool IsReadyForUse => Lifecycle == MockupTemplateLifecycle.ReadyForUse;
}

public sealed record MockupTemplateReadinessContext(
    MockupTemplate Template,
    MockupTemplateRevision Revision,
    IReadOnlyList<Guid> ActiveColorOptionValueIds,
    IReadOnlyList<OfferingOption> Options,
    IReadOnlyList<OfferingOptionValue> OptionValues,
    IReadOnlyList<OfferingVariant> Variants,
    IReadOnlyList<OfferingPlaceholder> DesignAreas,
    IReadOnlySet<Guid>? KnownSupportedColorOptionValueIds = null);

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
