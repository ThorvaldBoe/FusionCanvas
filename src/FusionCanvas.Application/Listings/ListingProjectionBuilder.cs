using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Products;

namespace FusionCanvas.Application.Listings;

public static class ListingProjectionBuilder
{
    public static ListingProjectionResult Build(ListingProjectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var issues = new List<ListingReadinessIssue>();

        if (request.Offering.Kind != BlueprintOfferingKind.FixedPrintProvider || request.Offering.PrintProviderId != request.Provider.Id)
        {
            issues.Add(new("fixed-provider-required", "The selected offering must resolve to the selected fixed Print Provider."));
        }

        if (request.Item.StoreId != request.Offering.StoreId || request.Item.StoreId != request.Provider.StoreId)
        {
            issues.Add(new("store-mismatch", "The Item, offering, and provider must belong to the same Store."));
        }

        var selectedColorValues = request.SelectedColors
            .Where(value => value.ItemId == request.Item.Id)
            .Select(value => value.ColorValue)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (selectedColorValues.Count == 0)
        {
            issues.Add(new("no-selected-colors", "Design must select at least one color before a product can be created."));
        }

        var optionValuesById = request.OptionValues.ToDictionary(value => value.Id);
        var optionsById = request.Options.ToDictionary(value => value.Id);
        var prices = new Dictionary<Guid, decimal>();
        var variantProjections = new List<(OfferingVariant Variant, string? Color, IReadOnlyList<KeyValuePair<string, string>> Options, decimal Cost)>();

        foreach (var variant in request.Variants.Where(value => !value.IsArchived))
        {
            var optionPairs = new List<KeyValuePair<string, string>>();
            string? color = null;
            var invalid = false;
            foreach (var optionValueId in variant.OptionValueIds)
            {
                if (!optionValuesById.TryGetValue(optionValueId, out var optionValue)
                    || !optionsById.TryGetValue(optionValue.OptionId, out var option))
                {
                    issues.Add(new("variant-option-missing", $"Variant '{variant.Name}' references missing catalog option data."));
                    invalid = true;
                    continue;
                }

                optionPairs.Add(new(option.Name, optionValue.Value));
                if (option.OptionKind == OptionKind.Color)
                {
                    color = optionValue.Value;
                }
            }

            if (invalid || (color is not null && !selectedColorValues.Contains(color)))
            {
                continue;
            }

            var cost = ReadProductionCost(variant.MetadataJson, issues, variant.Name);
            prices[variant.Id] = cost;
            variantProjections.Add((variant, color, optionPairs, cost));
        }

        if (variantProjections.Count == 0)
        {
            issues.Add(new("no-compatible-variants", "The fixed provider has no active variants matching Design's selected colors."));
        }

        var pricesByVariant = issues.Count == 0
            ? ListingPricingCalculator.Calculate(request.Pricing, prices).ToDictionary(value => value.SourceVariantId)
            : new Dictionary<Guid, ListingPrice>();

        var artwork = BuildArtwork(request, issues);
        if (issues.Count > 0)
        {
            return new ListingProjectionResult(null, issues);
        }

        var variants = variantProjections
            .Select(value => new ListingVariantProjection(
                value.Variant.Id,
                value.Variant.Name,
                value.Color,
                value.Options,
                value.Cost,
                pricesByVariant[value.Variant.Id])
            {
                ExternalVariantId = ReadExternalId(value.Variant.MetadataJson)
            })
            .ToArray();

        var externalOfferingParts = request.Offering.ExternalOfferingId?.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var externalBlueprintId = ReadExternalId(request.Blueprint.MetadataJson)
            ?? (externalOfferingParts is { Length: >= 1 } && int.TryParse(externalOfferingParts[0], out var offeringBlueprintId) ? offeringBlueprintId : null);
        int? externalProviderId = request.Provider.ExternalProviderId is not null && int.TryParse(request.Provider.ExternalProviderId, out var providerId)
            ? providerId
            : externalOfferingParts is { Length: >= 2 } && int.TryParse(externalOfferingParts[1], out var offeringProviderId) ? offeringProviderId : null;

        return new ListingProjectionResult(
            new ListingProductProjection(
                request.Item.Id,
                request.Item.StoreId,
                request.Blueprint.Id,
                request.Offering.Id,
                request.Provider.Id,
                $"{request.Item.Name} – {request.Blueprint.Name}",
                request.Item.Description,
                request.ShippingProfile,
                request.OutOfStockPolicy,
                request.Pricing.Normalize(),
                variants,
                artwork)
            {
                ExternalBlueprintId = externalBlueprintId,
                ExternalProviderId = externalProviderId
            },
            []);
    }

    private static IReadOnlyList<ListingArtworkProjection> BuildArtwork(
        ListingProjectionRequest request,
        ICollection<ListingReadinessIssue> issues)
    {
        var assetsById = request.Assets.ToDictionary(value => value.Id);
        var assignmentsByArea = request.SlotAssignments
            .Where(value => value.AssetId.HasValue)
            .GroupBy(value => value.DesignAreaId)
            .ToDictionary(value => value.Key, value => value.First());

        var result = new List<ListingArtworkProjection>();
        foreach (var area in request.DesignAreas)
        {
            if (!assignmentsByArea.TryGetValue(area.Id, out var assignment) || !assignment.AssetId.HasValue)
            {
                issues.Add(new("artwork-assignment-missing", $"Design area '{area.Name}' has no artwork assignment."));
                continue;
            }

            if (!assetsById.TryGetValue(assignment.AssetId.Value, out var asset) || asset.IsMissing)
            {
                issues.Add(new("artwork-asset-missing", $"The artwork assigned to design area '{area.Name}' is unavailable."));
                continue;
            }

            if (!request.ArtworkDimensions.TryGetValue(asset.Id, out var dimensions))
            {
                issues.Add(new("artwork-dimensions-missing", $"Artwork dimensions are unavailable for '{asset.Name}'."));
                continue;
            }

            dimensions.Normalize();
            var target = new ArtworkDimensions(area.Width, area.Height);
            result.Add(new ListingArtworkProjection(
                area.Id,
                asset.Id,
                asset.WorkspaceRelativePath,
                dimensions,
                target,
                ListingPlacementCalculator.Calculate(dimensions, target))
            {
                Position = area.Position,
                DecorationMethod = area.DecorationMethod
            });
        }

        return result;
    }

    private static decimal ReadProductionCost(string metadataJson, ICollection<ListingReadinessIssue> issues, string variantName)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(string.IsNullOrWhiteSpace(metadataJson) ? "{}" : metadataJson);
            if (document.RootElement.TryGetProperty("productionCost", out var property)
                && property.TryGetDecimal(out var cost)
                && cost >= 0m)
            {
                return cost;
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // The readiness diagnostic below is intentionally the same for malformed and missing metadata.
        }

        issues.Add(new("production-cost-missing", $"Production cost is unavailable for variant '{variantName}'."));
        return 0m;
    }

    private static int? ReadExternalId(string metadataJson)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(string.IsNullOrWhiteSpace(metadataJson) ? "{}" : metadataJson);
            return document.RootElement.TryGetProperty("id", out var id) && id.TryGetInt32(out var value) && value > 0 ? value : null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }
}
