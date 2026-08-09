using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Domain.Catalog;

public static class CatalogLifecyclePolicy
{
    public static string? PlaceholderDeletionBlocker(Guid placeholderId, IEnumerable<MockupTemplate> templates) =>
        templates.Any(template => template.TargetPlaceholderId == placeholderId && !template.IsArchived)
            ? "The Placeholder is referenced by an active Mockup Template; reassign or archive the template first."
            : null;

    public static string? ColorValueRetirementBlocker(Guid colorValueId, IEnumerable<MockupTemplateColorVariant> bindings) =>
        bindings.Any(binding => binding.ColorOptionValueId == colorValueId && !binding.IsArchived)
            ? "The Color Option Value is referenced by an active template-color binding; archive the binding first."
            : null;

    public static bool CoversSelectedVariants(OfferingPlaceholder placeholder, IEnumerable<Guid> selectedVariantIds) =>
        selectedVariantIds.All(placeholder.VariantIds.Contains);

    public static void EnsureStableTemplateTarget(MockupTemplate template, OfferingPlaceholder placeholder)
    {
        if (template.TargetPlaceholderId != placeholder.Id || template.BlueprintOfferingId != placeholder.OfferingId)
            throw new InvalidOperationException("Mockup Template TargetPlaceholderId must reference a Placeholder from the same offering.");
    }
}
