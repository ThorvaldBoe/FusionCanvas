using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Domain.Products;

/// <summary>
/// Invariant helpers for Design Stage operations.
/// Enforces the partition, one-default-row, slot-uniqueness, and
/// cross-entity validation rules. Editability/read-only gating is NOT
/// here — it belongs in the Application service via <see cref="ItemWorkflowPolicy"/>.
/// </summary>
public static class DesignStagePolicy
{
    /// <summary>Validates that Design variants are independent of Listing Color selection.</summary>
    public static void ValidateArtworkVariants(
        Guid itemId,
        IReadOnlyList<DesignVariantRow> variants,
        IReadOnlyList<DesignSlotAssignment> assignments)
    {
        var itemVariants = variants.Where(value => value.ItemId == itemId).ToArray();
        var variantIds = itemVariants.Select(value => value.Id).ToHashSet();
        var itemAssignments = assignments.Where(value => variantIds.Contains(value.RowId)).ToArray();

        if (itemVariants.Length == 0)
        {
            if (itemAssignments.Length > 0)
            {
                throw new InvalidOperationException($"Item {itemId} has artwork assignments without an artwork variant.");
            }

            return;
        }

        if (itemVariants.Count(value => value.IsDefault) != 1)
        {
            throw new InvalidOperationException($"Item {itemId} must have exactly one default artwork variant.");
        }

        if (itemVariants.Select(value => value.SortOrder).Distinct().Count() != itemVariants.Length)
        {
            throw new InvalidOperationException($"Item {itemId} has duplicate artwork variant order values.");
        }

        var slotKeys = new HashSet<(Guid VariantId, Guid AreaId)>();
        foreach (var assignment in itemAssignments)
        {
            if (!slotKeys.Add((assignment.RowId, assignment.DesignAreaId)))
            {
                throw new InvalidOperationException($"Artwork variant {assignment.RowId} has duplicate assignments for Design Area {assignment.DesignAreaId}.");
            }
        }
    }

    /// <summary>Validates Item-scoped Color-to-artwork assignments against their owner Item.</summary>
    public static void ValidateListingColorSelections(
        Guid itemId,
        IReadOnlyList<ListingColorSelection> selections,
        IReadOnlyList<DesignVariantRow> artworkVariants)
    {
        var itemSelections = selections.Where(value => value.ItemId == itemId).ToArray();
        var itemVariants = artworkVariants.Where(value => value.ItemId == itemId).Select(value => value.Id).ToHashSet();
        if (itemSelections.Any(value => !itemVariants.Contains(value.ArtworkVariantId)))
        {
            throw new InvalidOperationException($"A Listing Color for Item {itemId} references an artwork variant that does not belong to that Item.");
        }

        var duplicateResolved = itemSelections.Where(value => value.ColorOptionValueId.HasValue)
            .GroupBy(value => value.ColorOptionValueId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateResolved is not null)
        {
            throw new InvalidOperationException($"Color Option Value {duplicateResolved.Key} is selected more than once for Item {itemId}.");
        }

        var duplicateLegacy = itemSelections.Where(value => value.LegacyColorLabel is not null)
            .GroupBy(value => value.LegacyColorLabel!, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateLegacy is not null)
        {
            throw new InvalidOperationException($"Legacy Color '{duplicateLegacy.Key}' is selected more than once for Item {itemId}.");
        }
    }

    /// <summary>
    /// Validates that an offering can be selected as an item's listing configuration.
    /// </summary>
    public static bool IsValidConfiguration(WorkspaceSnapshot snapshot, Guid itemId, Guid offeringId)
    {
        var item = snapshot.Items.SingleOrDefault(i => i.Id == itemId);
        if (item is null) return false;

        var offering = snapshot.FulfillmentOfferings.SingleOrDefault(o => o.Id == offeringId);
        if (offering is null) return false;

        var product = snapshot.StoreProducts.SingleOrDefault(p => p.Id == offering.StoreProductId);
        if (product is null) return false;

        // Offering must belong to the item's Store
        if (product.StoreId != item.StoreId) return false;

        // Active: check that owning store exists and is not archived
        var store = snapshot.Stores.SingleOrDefault(s => s.Id == item.StoreId);
        if (store is null || store.IsArchived) return false;

        return true;
    }

    /// <summary>
    /// Validates the partition invariant: every selected color appears in
    /// exactly one row's color set, and the union of all rows' colors equals
    /// the selected colors. Also enforces exactly one default row per item.
    /// Throws <see cref="InvalidOperationException"/> on violation.
    /// </summary>
    public static void ValidatePartition(
        Guid itemId,
        IReadOnlyList<DesignSelectedColor> selectedColors,
        IReadOnlyList<DesignVariantRow> rows,
        IReadOnlyList<DesignVariantRowColor> rowColors)
    {
        var itemRows = rows.Where(r => r.ItemId == itemId).ToArray();
        var itemSelected = selectedColors.Where(c => c.ItemId == itemId).ToArray();
        var itemRowColors = rowColors.Where(rc => itemRows.Any(r => r.Id == rc.RowId)).ToArray();

        if (itemRows.Length == 0 && itemSelected.Length == 0)
        {
            return; // No rows and no selected colors: valid empty state
        }

        // Exactly one default row
        var defaultRows = itemRows.Where(r => r.IsDefault).ToArray();
        if (defaultRows.Length != 1)
        {
            throw new InvalidOperationException(
                $"Item {itemId} must have exactly one default row, but found {defaultRows.Length}.");
        }

        // Every selected color appears in exactly one row color entry
        var selectedColorValues = itemSelected.Select(c => c.ColorValue).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var colorValuesInRows = itemRowColors.Select(rc => rc.ColorValue).ToArray();

        var missingFromRows = selectedColorValues
            .Where(cv => !colorValuesInRows.Contains(cv, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        if (missingFromRows.Length > 0)
        {
            throw new InvalidOperationException(
                $"Selected colors [{string.Join(", ", missingFromRows)}] are not assigned to any row for item {itemId}.");
        }

        var extraInRows = colorValuesInRows
            .Where(cv => !selectedColorValues.Contains(cv))
            .ToArray();
        if (extraInRows.Length > 0)
        {
            throw new InvalidOperationException(
                $"Row colors [{string.Join(", ", extraInRows)}] are not in the selected color set for item {itemId}.");
        }

        // Each color appears at most once across all rows
        var duplicate = colorValuesInRows
            .GroupBy(cv => cv, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Color '{duplicate.Key}' appears in more than one row for item {itemId}.");
        }
    }

    /// <summary>
    /// Validates slot uniqueness and area-belonging.
    /// Throws <see cref="InvalidOperationException"/> on violation.
    /// </summary>
    public static void ValidateSlots(
        IReadOnlyList<DesignArea> areas,
        IReadOnlyList<DesignSlotAssignment> assignments)
    {
        foreach (var assignment in assignments)
        {
            if (areas.All(a => a.Id != assignment.DesignAreaId))
            {
                throw new InvalidOperationException(
                    $"Slot assignment references design area {assignment.DesignAreaId} which does not exist.");
            }
        }

        // PK (rowId, designAreaId) uniqueness
        var seen = new HashSet<(Guid rowId, Guid areaId)>();
        foreach (var assignment in assignments)
        {
            if (!seen.Add((assignment.RowId, assignment.DesignAreaId)))
            {
                throw new InvalidOperationException(
                    $"Duplicate slot assignment for row {assignment.RowId} and area {assignment.DesignAreaId}.");
            }
        }
    }

    /// <summary>
    /// Gets the available (deduplicated) color values from an offering's variants.
    /// A color value is the VariantOption.Value where VariantOption.Name is "Color" (case-insensitive).
    /// </summary>
    public static IReadOnlyList<string> AvailableColors(
        IReadOnlyList<ProductVariant> variants,
        Guid offeringId)
    {
        return variants
            .Where(v => v.FulfillmentOfferingId == offeringId)
            .SelectMany(v => v.Options)
            .Where(o => string.Equals(o.Name, "Color", StringComparison.OrdinalIgnoreCase))
            .Select(o => o.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Gets available color values from the authoritative catalog offering graph.
    /// A color value is referenced by a non-archived variant through an option
    /// whose stable kind is <see cref="OptionKind.Color"/>.
    /// </summary>
    public static IReadOnlyList<string> AvailableColors(
        IReadOnlyList<OfferingOption> options,
        IReadOnlyList<OfferingOptionValue> optionValues,
        IReadOnlyList<OfferingVariant> variants,
        Guid offeringId)
    {
        var colorOptionIds = options
            .Where(option => option.OfferingId == offeringId
                && !option.IsArchived
                && option.OptionKind == OptionKind.Color)
            .Select(option => option.Id)
            .ToHashSet();

        var colorValues = optionValues
            .Where(value => value.OfferingId == offeringId
                && !value.IsArchived
                && colorOptionIds.Contains(value.OptionId))
            .OrderBy(value => value.SortOrder)
            .ThenBy(value => value.Id)
            .ToArray();
        var colorValuesById = colorValues.ToDictionary(value => value.Id);

        var selectedValueIds = variants
            .Where(variant => variant.OfferingId == offeringId && !variant.IsArchived)
            .SelectMany(variant => variant.OptionValueIds)
            .Where(colorValuesById.ContainsKey)
            .ToHashSet();
        return colorValues
            .Where(value => selectedValueIds.Contains(value.Id))
            .Select(value => value.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Gets the design area IDs for an offering.
    /// </summary>
    public static IReadOnlyList<Guid> AreaIdsForOffering(
        IReadOnlyList<DesignArea> areas,
        Guid offeringId)
    {
        return areas
            .Where(a => a.FulfillmentOfferingId == offeringId)
            .Select(a => a.Id)
            .ToArray();
    }
}
