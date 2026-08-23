namespace FusionCanvas.Domain.Mockups;

public static class MockupTemplatePolicy
{
    public static bool IsOutputAffectingChange(Guid oldPlaceholderId, Guid newPlaceholderId, IReadOnlySet<Guid> oldColors, IReadOnlySet<Guid> newColors) =>
        oldPlaceholderId != newPlaceholderId || !oldColors.SetEquals(newColors);

    public static bool IsOutputAffectingChange(Guid oldPlaceholderId, Guid newPlaceholderId, IReadOnlySet<Guid> oldColors, IReadOnlySet<Guid> newColors, string? oldProviderMockupReference, string? newProviderMockupReference, MockupImageSpaceMapping? oldImageMapping, MockupImageSpaceMapping? newImageMapping) =>
        IsOutputAffectingChange(oldPlaceholderId, newPlaceholderId, oldColors, newColors)
        || !string.Equals(oldProviderMockupReference, newProviderMockupReference, StringComparison.Ordinal)
        || oldImageMapping != newImageMapping;

    public static void EnsureUniqueActiveColor(IEnumerable<MockupTemplateColorVariant> bindings)
    {
        var duplicate = bindings.Where(binding => !binding.IsArchived)
            .GroupBy(binding => (binding.MockupTemplateId, binding.ColorOptionValueId))
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException("Only one active template-color record is allowed per template and Color Option Value.");
        }
    }
}
