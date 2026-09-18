namespace FusionCanvas.Domain.Catalog;

public static class CatalogRelationshipPolicy
{
    public static IReadOnlyList<BlueprintOffering> ReplacePrimaryArtworkDesignArea(
        IReadOnlyList<BlueprintOffering> offerings,
        IReadOnlyList<OfferingPlaceholder> placeholders,
        Guid offeringId,
        Guid? designAreaId)
    {
        ArgumentNullException.ThrowIfNull(offerings);
        ArgumentNullException.ThrowIfNull(placeholders);

        var offering = offerings.SingleOrDefault(value => value.Id == offeringId)
            ?? throw new InvalidOperationException("Offering was not found.");

        if (designAreaId is Guid selectedAreaId)
        {
            var area = placeholders.SingleOrDefault(value => value.Id == selectedAreaId);
            if (area is null || area.OfferingId != offering.Id)
                throw new InvalidOperationException("Primary artwork Design Area must belong to the same offering.");
            if (area.IsArchived)
                throw new InvalidOperationException("Primary artwork Design Area must be active.");
        }

        return offerings
            .Select(value => value.Id == offering.Id
                ? value with { PrimaryArtworkDesignAreaId = designAreaId }
                : value)
            .ToArray();
    }

    public static void ValidateOffering(
        BlueprintOffering offering,
        Blueprint blueprint,
        PrintProvider? printProvider,
        IReadOnlyList<OfferingOption> options,
        IReadOnlyList<OfferingOptionValue> values,
        IReadOnlyList<OfferingVariant> variants,
        IReadOnlyList<OfferingPlaceholder> placeholders)
    {
        if (offering.BlueprintId != blueprint.Id || offering.StoreId != blueprint.StoreId)
            throw new InvalidOperationException("Offering must belong to its Blueprint and Store.");

        if (offering.Kind == BlueprintOfferingKind.FixedPrintProvider && offering.PrintProviderId is null)
            throw new InvalidOperationException("A fixed-provider offering requires a Print Provider.");
        if (offering.Kind == BlueprintOfferingKind.ProviderNetwork && string.IsNullOrWhiteSpace(offering.ProviderNetworkCode))
            throw new InvalidOperationException("A Provider-Network offering requires a stable provider-network code.");
        if (offering.Kind == BlueprintOfferingKind.FixedPrintProvider && offering.ProviderNetworkCode is not null)
            throw new InvalidOperationException("A fixed-provider offering must not have a Provider-Network code.");
        if (offering.Kind == BlueprintOfferingKind.ProviderNetwork && offering.PrintProviderId is not null)
            throw new InvalidOperationException("A Provider-Network offering must not reference a Print Provider.");
        if (printProvider is not null && printProvider.StoreId != offering.StoreId)
            throw new InvalidOperationException("Print Provider must belong to the offering Store.");

        var offeringOptions = options.Where(option => option.OfferingId == offering.Id).ToArray();
        var optionIds = offeringOptions.Select(option => option.Id).ToHashSet();
        var activeOfferingOptions = offeringOptions.Where(option => !option.IsArchived).ToArray();
        if (activeOfferingOptions.Select(option => option.OptionKind).Distinct().Count() != activeOfferingOptions.Length)
            throw new InvalidOperationException("An offering cannot contain duplicate active option kinds.");

        var offeringValues = values.Where(value => value.OfferingId == offering.Id).ToArray();
        if (offeringValues.Any(value => !optionIds.Contains(value.OptionId)))
            throw new InvalidOperationException("Option Values must belong to an Option in the same offering.");

        var valueIds = offeringValues.Select(value => value.Id).ToHashSet();
        foreach (var variant in variants.Where(variant => variant.OfferingId == offering.Id))
        {
            if (variant.OptionValueIds.Any(valueId => !valueIds.Contains(valueId)))
                throw new InvalidOperationException("Variant Option Values must belong to the same offering.");

            var kinds = variant.OptionValueIds.Select(valueId => offeringValues.Single(value => value.Id == valueId).OptionId).ToArray();
            if (kinds.Distinct().Count() != kinds.Length)
                throw new InvalidOperationException("A concrete Variant cannot contain two values from the same Option.");
        }

        foreach (var placeholder in placeholders.Where(placeholder => placeholder.OfferingId == offering.Id))
        {
            if (placeholder.VariantIds.Any(variantId => variants.All(variant => variant.Id != variantId || variant.OfferingId != offering.Id)))
                throw new InvalidOperationException("Placeholder compatibility must reference concrete Variants from the same offering.");
        }

        if (offering.PrimaryArtworkDesignAreaId is Guid primaryAreaId)
        {
            var primary = placeholders.SingleOrDefault(value => value.Id == primaryAreaId);
            if (primary is null || primary.OfferingId != offering.Id)
                throw new InvalidOperationException("Primary artwork Design Area must belong to the same offering.");
            if (!offering.IsArchived && primary.IsArchived)
                throw new InvalidOperationException("Primary artwork Design Area must be active.");
        }
    }

    public static void ValidateMockupTemplateColor(
        Guid offeringId,
        OfferingOptionValue colorValue,
        OfferingOption colorOption,
        Mockups.MockupTemplate template)
    {
        if (template.BlueprintOfferingId != offeringId || colorValue.OfferingId != offeringId || colorOption.OfferingId != offeringId)
            throw new InvalidOperationException("Mockup template color bindings must belong to the template offering.");
        if (colorOption.OptionKind != OptionKind.Color || colorValue.OptionId != colorOption.Id)
            throw new InvalidOperationException("A mockup template color must reference a Color Option Value.");
    }
}
