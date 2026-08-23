using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Catalog;

/// <summary>
/// Repairs catalog rows written by early schema-11 builds and keeps the legacy
/// product projection aligned for consumers that have not moved to Catalog yet.
/// The normalized catalog is authoritative whenever an equivalent row exists.
/// </summary>
public static class CatalogCompatibilitySynchronizer
{
    public static (WorkspaceSnapshot Snapshot, bool Changed) SynchronizeStore(
        WorkspaceSnapshot source,
        Guid storeId,
        Func<DateTimeOffset> clock,
        Func<Guid> newId)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(newId);

        var repaired = RepairMissingNormalizedRecords(source, storeId, clock, newId);
        return MirrorNormalizedRecords(repaired.Snapshot, storeId, repaired.Changed);
    }

    private static (WorkspaceSnapshot Snapshot, bool Changed) RepairMissingNormalizedRecords(
        WorkspaceSnapshot source,
        Guid storeId,
        Func<DateTimeOffset> clock,
        Func<Guid> newId)
    {
        var blueprints = source.Blueprints.ToList();
        var providers = source.PrintProviders.ToList();
        var offerings = source.BlueprintOfferings.ToList();
        var options = source.OfferingOptions.ToList();
        var values = source.OfferingOptionValues.ToList();
        var variants = source.OfferingVariants.ToList();
        var placeholders = source.OfferingPlaceholders.ToList();
        var changed = false;

        foreach (var product in source.StoreProducts.Where(value => value.StoreId == storeId))
        {
            if (blueprints.All(value => value.Id != product.Id))
            {
                blueprints.Add(new Blueprint(product.Id, storeId, product.Name, product.Description, false, product.CreatedAt, product.UpdatedAt, product.MetadataJson));
                changed = true;
            }
        }

        var legacyOfferings = source.FulfillmentOfferings
            .Where(value => source.StoreProducts.Any(product => product.Id == value.StoreProductId && product.StoreId == storeId))
            .ToArray();

        foreach (var legacy in legacyOfferings)
        {
            if (offerings.All(value => value.Id != legacy.Id))
            {
                Guid? providerId = null;
                string? networkCode = null;
                var kind = legacy.Kind == FulfillmentKind.FixedProvider
                    ? BlueprintOfferingKind.FixedPrintProvider
                    : BlueprintOfferingKind.ProviderNetwork;

                if (kind == BlueprintOfferingKind.FixedPrintProvider)
                {
                    var providerName = legacy.ProviderName ?? "Unknown provider";
                    var provider = providers.FirstOrDefault(value => value.StoreId == storeId &&
                        string.Equals(value.Name, providerName, StringComparison.OrdinalIgnoreCase));
                    if (provider is null)
                    {
                        var now = clock();
                        provider = new PrintProvider(NextId(source, blueprints, providers, offerings, options, values, variants, placeholders, newId), storeId, providerName, null, false, now, now);
                        providers.Add(provider);
                        changed = true;
                    }

                    providerId = provider.Id;
                }
                else
                {
                    networkCode = "printify-choice";
                }

                offerings.Add(new BlueprintOffering(
                    legacy.Id,
                    legacy.StoreProductId,
                    storeId,
                    legacy.Name,
                    legacy.Description,
                    kind,
                    providerId,
                    networkCode,
                    null,
                    legacy.ExternalOfferingId,
                    false,
                    legacy.CreatedAt,
                    legacy.UpdatedAt,
                    legacy.MetadataJson));
                changed = true;
            }

            var normalizedOffering = offerings.Single(value => value.Id == legacy.Id);
            foreach (var legacyVariant in source.ProductVariants.Where(value => value.FulfillmentOfferingId == legacy.Id))
            {
                var optionValueIds = new List<Guid>();
                foreach (var legacyOption in legacyVariant.Options)
                {
                    var kind = ToOptionKind(legacyOption.Name);
                    var option = options.FirstOrDefault(value => value.OfferingId == legacy.Id && !value.IsArchived &&
                        string.Equals(value.Name, legacyOption.Name, StringComparison.OrdinalIgnoreCase));
                    option ??= options.FirstOrDefault(value => value.OfferingId == legacy.Id && !value.IsArchived &&
                        value.OptionKind == kind && kind is OptionKind.Color or OptionKind.Size);
                    if (option is null)
                    {
                        option = new OfferingOption(
                            NextId(source, blueprints, providers, offerings, options, values, variants, placeholders, newId),
                            legacy.Id,
                            kind,
                            legacyOption.Name,
                            options.Count(value => value.OfferingId == legacy.Id));
                        options.Add(option);
                        changed = true;
                    }

                    var optionValue = values.FirstOrDefault(value => value.OptionId == option.Id && !value.IsArchived &&
                        string.Equals(value.Value, legacyOption.Value, StringComparison.OrdinalIgnoreCase));
                    if (optionValue is null)
                    {
                        optionValue = new OfferingOptionValue(
                            NextId(source, blueprints, providers, offerings, options, values, variants, placeholders, newId),
                            option.Id,
                            legacy.Id,
                            legacyOption.Value,
                            values.Count(value => value.OptionId == option.Id));
                        values.Add(optionValue);
                        changed = true;
                    }

                    optionValueIds.Add(optionValue.Id);
                }

                if (variants.All(value => value.Id != legacyVariant.Id) && optionValueIds.Count > 0)
                {
                    variants.Add(new OfferingVariant(
                        legacyVariant.Id,
                        normalizedOffering.Id,
                        VariantDisplayName(legacyVariant.Options),
                        optionValueIds,
                        false,
                        legacyVariant.CreatedAt,
                        legacyVariant.UpdatedAt));
                    changed = true;
                }
            }

            foreach (var area in source.DesignAreas.Where(value => value.FulfillmentOfferingId == legacy.Id))
            {
                if (placeholders.Any(value => value.Id == area.Id))
                {
                    continue;
                }

                var compatibleVariants = area.VariantIds.Count == 0
                    ? variants.Where(value => value.OfferingId == legacy.Id && !value.IsArchived).Select(value => value.Id).ToArray()
                    : area.VariantIds.Where(id => variants.Any(value => value.Id == id && value.OfferingId == legacy.Id)).ToArray();
                placeholders.Add(new OfferingPlaceholder(
                    area.Id,
                    legacy.Id,
                    area.Name,
                    area.Description,
                    area.Position,
                    area.DecorationMethod,
                    area.Width,
                    area.Height,
                    compatibleVariants,
                    false,
                    area.CreatedAt,
                    area.UpdatedAt,
                    area.MetadataJson));
                changed = true;
            }
        }

        return (source with
        {
            Blueprints = blueprints,
            PrintProviders = providers,
            BlueprintOfferings = offerings,
            OfferingOptions = options,
            OfferingOptionValues = values,
            OfferingVariants = variants,
            OfferingPlaceholders = placeholders
        }, changed);
    }

    private static (WorkspaceSnapshot Snapshot, bool Changed) MirrorNormalizedRecords(
        WorkspaceSnapshot source,
        Guid storeId,
        bool alreadyChanged)
    {
        var products = source.StoreProducts.ToList();
        foreach (var blueprint in source.Blueprints.Where(value => value.StoreId == storeId))
        {
            var existing = products.FirstOrDefault(value => value.Id == blueprint.Id);
            if (existing is null)
            {
                products.Add(new StoreProduct(blueprint.Id, storeId, blueprint.Name, blueprint.Description, null, blueprint.CreatedAt, blueprint.UpdatedAt, blueprint.MetadataJson));
            }
            else
            {
                products[products.IndexOf(existing)] = existing with
                {
                    Name = blueprint.Name,
                    Description = blueprint.Description,
                    UpdatedAt = blueprint.UpdatedAt
                };
            }
        }

        var normalizedOfferingIds = source.BlueprintOfferings.Where(value => value.StoreId == storeId).Select(value => value.Id).ToHashSet();
        var legacyOfferings = source.FulfillmentOfferings.Where(value => !normalizedOfferingIds.Contains(value.Id)).ToList();
        foreach (var offering in source.BlueprintOfferings.Where(value => value.StoreId == storeId && !value.IsArchived))
        {
            var providerName = offering.PrintProviderId is Guid providerId
                ? source.PrintProviders.SingleOrDefault(value => value.Id == providerId)?.Name
                : null;
            legacyOfferings.Add(new FulfillmentOffering(
                offering.Id,
                offering.BlueprintId,
                offering.Name,
                offering.Description,
                offering.Kind == BlueprintOfferingKind.FixedPrintProvider ? FulfillmentKind.FixedProvider : FulfillmentKind.PrintifyChoiceNetwork,
                providerName,
                offering.ExternalOfferingId,
                offering.CreatedAt,
                offering.UpdatedAt,
                offering.MetadataJson));
        }

        var legacyVariants = source.ProductVariants.Where(value => !normalizedOfferingIds.Contains(value.FulfillmentOfferingId)).ToList();
        foreach (var variant in source.OfferingVariants.Where(value => normalizedOfferingIds.Contains(value.OfferingId) && !value.IsArchived))
        {
            var variantOptions = variant.OptionValueIds.Select(valueId =>
            {
                var optionValue = source.OfferingOptionValues.Single(value => value.Id == valueId);
                var option = source.OfferingOptions.Single(value => value.Id == optionValue.OptionId);
                return new VariantOption(option.Name, optionValue.Value);
            }).ToArray();
            legacyVariants.Add(new ProductVariant(variant.Id, variant.OfferingId, variantOptions, variant.CreatedAt, variant.UpdatedAt));
        }

        var legacyAreas = source.DesignAreas.Where(value => !normalizedOfferingIds.Contains(value.FulfillmentOfferingId)).ToList();
        legacyAreas.AddRange(source.OfferingPlaceholders
            .Where(value => normalizedOfferingIds.Contains(value.OfferingId) && !value.IsArchived)
            .Select(value => new DesignArea(
                value.Id,
                value.OfferingId,
                value.Name,
                value.Description,
                value.Position,
                value.DecorationMethod,
                value.Width,
                value.Height,
                value.VariantIds,
                value.CreatedAt,
                value.UpdatedAt,
                value.MetadataJson)));

        var changed = alreadyChanged
            || !products.SequenceEqual(source.StoreProducts)
            || !legacyOfferings.SequenceEqual(source.FulfillmentOfferings)
            || !legacyVariants.SequenceEqual(source.ProductVariants)
            || !legacyAreas.SequenceEqual(source.DesignAreas);
        return (source with
        {
            StoreProducts = products,
            FulfillmentOfferings = legacyOfferings,
            ProductVariants = legacyVariants,
            DesignAreas = legacyAreas
        }, changed);
    }

    private static OptionKind ToOptionKind(string name) =>
        name.Equals("Color", StringComparison.OrdinalIgnoreCase) ? OptionKind.Color
            : name.Equals("Size", StringComparison.OrdinalIgnoreCase) ? OptionKind.Size
            : OptionKind.Other;

    private static string VariantDisplayName(IEnumerable<VariantOption> options) =>
        string.Join(", ", options.Select(value => $"{value.Name}: {value.Value}"));

    private static Guid NextId(
        WorkspaceSnapshot source,
        IReadOnlyCollection<Blueprint> blueprints,
        IReadOnlyCollection<PrintProvider> providers,
        IReadOnlyCollection<BlueprintOffering> offerings,
        IReadOnlyCollection<OfferingOption> options,
        IReadOnlyCollection<OfferingOptionValue> values,
        IReadOnlyCollection<OfferingVariant> variants,
        IReadOnlyCollection<OfferingPlaceholder> placeholders,
        Func<Guid> newId)
    {
        while (true)
        {
            var id = newId();
            if (id != Guid.Empty
                && blueprints.All(value => value.Id != id)
                && providers.All(value => value.Id != id)
                && offerings.All(value => value.Id != id)
                && options.All(value => value.Id != id)
                && values.All(value => value.Id != id)
                && variants.All(value => value.Id != id)
                && placeholders.All(value => value.Id != id)
                && source.MockupTemplates.All(value => value.Id != id))
            {
                return id;
            }
        }
    }
}
