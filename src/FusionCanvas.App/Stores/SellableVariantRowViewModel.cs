using FusionCanvas.Application.Catalog;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.App.Stores;

public sealed record SellableVariantRowViewModel(
    Guid Id,
    string Name,
    string? Color,
    string? Size,
    string? Other,
    bool IsArchived)
{
    public string SemanticSummary => string.Join(" · ", new[]
    {
        Color is null ? null : $"Color: {Color}",
        Size is null ? null : $"Size: {Size}",
        Other is null ? null : $"Other: {Other}"
    }.Where(value => value is not null));

    public static SellableVariantRowViewModel From(
        OfferingVariant variant,
        IReadOnlyCollection<OfferingOption> options,
        IReadOnlyCollection<OfferingOptionValue> values)
    {
        ArgumentNullException.ThrowIfNull(variant);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(values);

        var optionById = options.ToDictionary(option => option.Id);
        var valuesByKind = new Dictionary<OptionKind, List<string>>();
        var unresolved = false;
        foreach (var valueId in variant.OptionValueIds)
        {
            var value = values.FirstOrDefault(candidate => candidate.Id == valueId);
            if (value is null || !optionById.TryGetValue(value.OptionId, out var option))
            {
                unresolved = true;
                continue;
            }

            if (!valuesByKind.TryGetValue(option.OptionKind, out var labels))
            {
                labels = [];
                valuesByKind.Add(option.OptionKind, labels);
            }
            labels.Add(value.Value);
        }

        string? Resolve(OptionKind kind)
        {
            if (!valuesByKind.TryGetValue(kind, out var labels)) return null;
            return string.Join(", ", labels);
        }

        var other = Resolve(OptionKind.Other);
        if (unresolved)
            other = string.IsNullOrEmpty(other) ? "Unavailable value" : $"{other}, Unavailable value";

        return new(variant.Id, variant.Name, Resolve(OptionKind.Color), Resolve(OptionKind.Size), other, variant.IsArchived);
    }
}
