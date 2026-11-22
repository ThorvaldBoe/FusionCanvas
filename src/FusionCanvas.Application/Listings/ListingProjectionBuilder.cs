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

        var externalOfferingParts = request.Offering.ExternalOfferingId?.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var externalBlueprintId = ReadExternalId(request.Blueprint.MetadataJson)
            ?? (externalOfferingParts is { Length: >= 1 } && int.TryParse(externalOfferingParts[0], out var offeringBlueprintId) ? offeringBlueprintId : null);
        int? externalProviderId = request.Provider.ExternalProviderId is not null && int.TryParse(request.Provider.ExternalProviderId, out var providerId)
            ? providerId
            : externalOfferingParts is { Length: >= 2 } && int.TryParse(externalOfferingParts[1], out var offeringProviderId) ? offeringProviderId : null;

        if (externalBlueprintId is null or <= 0)
        {
            issues.Add(new("printify-blueprint-identity-missing", "The selected Blueprint has no Printify Blueprint ID. Reimport its Printify catalog data before creating a listing."));
        }

        if (externalProviderId is null or <= 0)
        {
            issues.Add(new("printify-provider-identity-missing", "The selected Print Provider has no Printify Provider ID. Reimport its Printify catalog data before creating a listing."));
        }

        var listingSelections = request.ListingColorSelections.Where(value => value.ItemId == request.Item.Id).ToArray();
        var selectedColorIds = listingSelections.Where(value => value.ColorOptionValueId is not null)
            .Select(value => value.ColorOptionValueId!.Value).ToHashSet();
        var selectedColorValues = listingSelections.Where(value => value.ColorOptionValueId is null)
            .Select(value => value.LegacyColorLabel!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (listingSelections.Length == 0)
        {
            selectedColorValues = request.SelectedColors.Where(value => value.ItemId == request.Item.Id)
                .Select(value => value.ColorValue).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        if (selectedColorValues.Count == 0 && selectedColorIds.Count == 0)
        {
            issues.Add(new("no-selected-colors", "Select at least one Color in Listing before creating a product."));
        }

        var optionValuesById = request.OptionValues.ToDictionary(value => value.Id);
        var optionsById = request.Options.ToDictionary(value => value.Id);
        var prices = new Dictionary<Guid, decimal?>();
        var variantProjections = new List<(OfferingVariant Variant, string? Color, Guid? ColorOptionValueId, IReadOnlyList<KeyValuePair<string, string>> Options, decimal? Cost)>();

        foreach (var variant in request.Variants.Where(value => !value.IsArchived))
        {
            var optionPairs = new List<KeyValuePair<string, string>>();
            string? color = null;
            Guid? colorOptionValueId = null;
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
                    colorOptionValueId = optionValue.Id;
                }
            }

            var colorSelected = colorOptionValueId is Guid stableColorId
                ? selectedColorIds.Contains(stableColorId) || (selectedColorIds.Count == 0 && selectedColorValues.Contains(color ?? string.Empty))
                : color is null || selectedColorValues.Contains(color);
            if (invalid || !colorSelected)
            {
                continue;
            }

            var cost = ReadProductionCost(variant.MetadataJson);
            if (request.Pricing.Policy == ListingPricingPolicy.FixedProfitAmount && cost is null)
            {
                issues.Add(new("production-cost-missing", $"Production cost is required for fixed-profit pricing of variant '{variant.Name}'."));
            }
            prices[variant.Id] = cost;
            variantProjections.Add((variant, color, colorOptionValueId, optionPairs, cost));
        }

        if (variantProjections.Count == 0)
        {
            issues.Add(new("no-compatible-variants", "The selected Colors have no active configured provider Variants."));
        }

        var pricesByVariant = issues.Count == 0
            ? ListingPricingCalculator.Calculate(request.Pricing, prices).ToDictionary(value => value.SourceVariantId)
            : new Dictionary<Guid, ListingPrice>();

        var selectionsForArtwork = listingSelections.Length > 0
            ? listingSelections.Select(value => (
                value.ColorOptionValueId,
                value.ColorOptionValueId is Guid id ? optionValuesById.GetValueOrDefault(id)?.Value ?? value.LegacyColorLabel ?? string.Empty : value.LegacyColorLabel ?? string.Empty,
                value.ArtworkVariantId)).ToArray()
            : selectedColorValues.Select(color =>
            {
                var rowId = request.VariantRowColors.FirstOrDefault(value => string.Equals(value.ColorValue, color, StringComparison.OrdinalIgnoreCase))?.RowId
                    ?? request.VariantRows.FirstOrDefault(value => value.ItemId == request.Item.Id && value.IsDefault)?.Id
                    ?? Guid.Empty;
                return ((Guid?)null, color, rowId);
            }).ToArray();
        var artwork = BuildArtwork(request, issues, variantProjections, selectionsForArtwork);
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
        ICollection<ListingReadinessIssue> issues,
        IReadOnlyList<(OfferingVariant Variant, string? Color, Guid? ColorOptionValueId, IReadOnlyList<KeyValuePair<string, string>> Options, decimal? Cost)> projectedVariants,
        IReadOnlyList<(Guid? ColorOptionValueId, string Color, Guid ArtworkVariantId)> selectedColors)
    {
        var assetsById = request.Assets.ToDictionary(value => value.Id);
        var itemRowIds = request.VariantRows
            .Where(value => value.ItemId == request.Item.Id)
            .Select(value => value.Id)
            .ToHashSet();
        var result = new List<ListingArtworkProjection>();
        var hasAssignment = false;
        foreach (var selectedColor in selectedColors)
        {
            if (selectedColor.ArtworkVariantId == Guid.Empty || !itemRowIds.Contains(selectedColor.ArtworkVariantId))
            {
                issues.Add(new("artwork-variant-assignment-missing", $"No valid artwork variant is assigned to Color '{selectedColor.Color}'."));
                continue;
            }

            var sourceVariantIds = projectedVariants.Where(value =>
                    selectedColor.ColorOptionValueId is Guid colorId
                        ? value.ColorOptionValueId == colorId
                        : string.Equals(value.Color, selectedColor.Color, StringComparison.OrdinalIgnoreCase))
                .Select(value => value.Variant.Id)
                .ToArray();
            if (sourceVariantIds.Length == 0)
            {
                issues.Add(new("no-compatible-color-variants", $"Color '{selectedColor.Color}' has no configured active provider Variants."));
                continue;
            }

            var colorHasAssignment = false;
            foreach (var area in request.DesignAreas)
            {
                var assignment = request.SlotAssignments.SingleOrDefault(value =>
                    value.RowId == selectedColor.ArtworkVariantId && value.DesignAreaId == area.Id);
                if (assignment?.AssetId is not Guid assetId)
                    continue;
                hasAssignment = true;
                colorHasAssignment = true;

                if (!assetsById.TryGetValue(assetId, out var asset) || asset.IsMissing)
                {
                    issues.Add(new("artwork-asset-missing", $"The artwork assigned to design area '{area.Name}' for Color '{selectedColor.Color}' is unavailable."));
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
                    DecorationMethod = area.DecorationMethod,
                    ColorOptionValueId = selectedColor.ColorOptionValueId,
                    ArtworkVariantId = selectedColor.ArtworkVariantId,
                    SourceVariantIds = sourceVariantIds
                });
            }

            if (!colorHasAssignment)
                issues.Add(new("artwork-assignment-missing", $"Assign artwork for Color '{selectedColor.Color}' before creating a Printify product."));
        }

        if (result.Count == 0 && !hasAssignment)
        {
            issues.Add(new("artwork-assignment-missing", "Assign artwork to at least one design area before creating a Printify product."));
        }

        return result;
    }

    private static decimal? ReadProductionCost(string metadataJson)
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

        return null;
    }

    private static int? ReadExternalId(string metadataJson)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(string.IsNullOrWhiteSpace(metadataJson) ? "{}" : metadataJson);
            var root = document.RootElement;
            if (root.TryGetProperty("id", out var id) && id.TryGetInt32(out var value) && value > 0)
                return value;
            if (root.TryGetProperty("blueprintId", out var blueprintId) && blueprintId.TryGetInt32(out value) && value > 0)
                return value;
            if (root.TryGetProperty("ids", out var ids)
                && ids.ValueKind == System.Text.Json.JsonValueKind.Array
                && ids.EnumerateArray().FirstOrDefault(candidate => candidate.TryGetInt32(out var parsed) && parsed > 0) is { ValueKind: System.Text.Json.JsonValueKind.Number } importedId
                && importedId.TryGetInt32(out value))
            {
                return value;
            }

            return null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }
}
