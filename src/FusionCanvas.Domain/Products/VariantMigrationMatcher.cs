using System.Text;
using System.Text.Json;
using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.Domain.Products;

public static class VariantMigrationMatcher
{
    public static IReadOnlyDictionary<Guid, Guid> MatchExactVariants(
        IReadOnlyList<OfferingVariant> sourceVariants, IReadOnlyList<OfferingVariant> destinationVariants,
        IReadOnlyList<OfferingOption> options, IReadOnlyList<OfferingOptionValue> values)
    {
        var source = sourceVariants.Where(value => !value.IsArchived).Select(value => (Variant: value, Key: Signature(value, options, values))).Where(value => value.Key is not null).ToArray();
        var destination = destinationVariants.Where(value => !value.IsArchived).Select(value => (Variant: value, Key: Signature(value, options, values))).Where(value => value.Key is not null).ToArray();
        var sourceCounts = source.GroupBy(value => value.Key!, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var destinationCounts = destination.GroupBy(value => value.Key!, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var result = new Dictionary<Guid, Guid>();
        foreach (var target in destination)
            if (sourceCounts.GetValueOrDefault(target.Key!) == 1 && destinationCounts.GetValueOrDefault(target.Key!) == 1)
                result[target.Variant.Id] = source.Single(value => value.Key == target.Key).Variant.Id;
        return result;
    }

    private static string? Signature(OfferingVariant variant, IReadOnlyList<OfferingOption> options, IReadOnlyList<OfferingOptionValue> values)
    {
        var optionMap = options.Where(value => !value.IsArchived && value.OfferingId == variant.OfferingId).ToDictionary(value => value.Id);
        var valueMap = values.Where(value => !value.IsArchived && value.OfferingId == variant.OfferingId).ToDictionary(value => value.Id);
        if (variant.OptionValueIds.Count == 0) return null;
        var parts = new List<SignaturePart>();
        foreach (var id in variant.OptionValueIds)
        {
            if (!valueMap.TryGetValue(id, out var value) || !optionMap.TryGetValue(value.OptionId, out var option)) return null;
            var name = Normalize(option.Name);
            var optionValue = Normalize(value.Value);
            if (name.Length == 0 || optionValue.Length == 0) return null;
            parts.Add(new SignaturePart((int)option.OptionKind, name, optionValue));
        }
        if (parts.DistinctBy(value => (value.Kind, value.Name, value.Value)).Count() != parts.Count) return null;
        return JsonSerializer.Serialize(parts.OrderBy(value => value.Kind).ThenBy(value => value.Name, StringComparer.Ordinal).ThenBy(value => value.Value, StringComparer.Ordinal));
    }

    private static string Normalize(string value) => value.Normalize(NormalizationForm.FormKC).Trim().ToUpperInvariant();

    private sealed record SignaturePart(int Kind, string Name, string Value);
}
