using FusionCanvas.Application.Catalog;
using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.App.Stores;

public sealed record BlueprintOfferingCardViewModel(
    Guid Id,
    string Name,
    string FulfillmentContext,
    bool IsProviderNetwork,
    string Status,
    int VariantCount,
    int DesignAreaCount,
    int MockupTemplateCount)
{
    public string SetupSummary => $"{VariantCount} Variants · {DesignAreaCount} Design Areas · {MockupTemplateCount} Mockup Templates";

    public static BlueprintOfferingCardViewModel From(BlueprintOfferingSetupSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        var status = summary.IsArchived
            ? "Archived"
            : summary.Counts.VariantsComplete && summary.Counts.DesignAreasComplete && summary.Counts.MockupTemplatesComplete
                ? "Ready"
                : "Setup incomplete";
        return new(
            summary.Context.OfferingId,
            summary.Name,
            summary.Fulfillment.DisplayName,
            summary.Fulfillment.IsVariableProviderNetwork,
            status,
            summary.Counts.ActiveVariants,
            summary.Counts.ActiveDesignAreas,
            summary.Counts.ActiveMockupTemplates);
    }
}

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

public sealed record DesignAreaCardViewModel(
    Guid Id,
    string Name,
    string Placement,
    int MaximumWidthPixels,
    int MaximumHeightPixels,
    string CompatibilitySummary)
{
    public string MaximumSizeSummary => $"{MaximumWidthPixels:N0} × {MaximumHeightPixels:N0} px";

    public static DesignAreaCardViewModel From(DesignAreaSetupSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return new(
            summary.Id,
            summary.Name,
            summary.Placement,
            summary.MaximumWidthPixels,
            summary.MaximumHeightPixels,
            summary.AppliesToAllActiveVariants ? "All active Variants" : $"{summary.CompatibleVariantCount} compatible Variants");
    }
}

public sealed record MockupTemplateCardViewModel(
    Guid Id,
    string Name,
    string TargetDesignArea,
    string ColorSummary,
    string VariantSummary,
    int CurrentRevision,
    string Status)
{
    public string RevisionSummary => $"Revision {CurrentRevision} · {Status}";

    public static MockupTemplateCardViewModel From(
        MockupTemplateSetupSummary summary,
        IReadOnlyCollection<OfferingOptionValue> optionValues)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(optionValues);
        var colors = summary.ColorOptionValueIds
            .Select(id => optionValues.FirstOrDefault(value => value.Id == id)?.Value ?? "Unavailable Color")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new(
            summary.Id,
            summary.Name,
            summary.TargetDesignAreaName,
            colors.Length == 0 ? "No Colors" : string.Join(", ", colors),
            $"{summary.CompatibleVariantIds.Count} compatible Variants",
            summary.CurrentRevision,
            summary.IsArchived ? "Archived" : "Active");
    }
}
