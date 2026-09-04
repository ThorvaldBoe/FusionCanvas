using FusionCanvas.Application.Catalog;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.App.Stores;

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
            summary.TargetDesignAreaName ?? "No Design Area",
            colors.Length == 0 ? "No Colors" : string.Join(", ", colors),
            $"{summary.CompatibleVariantIds.Count} compatible Variants",
            summary.CurrentRevision,
            summary.IsArchived ? "Archived" : summary.Lifecycle == MockupTemplateLifecycle.ReadyForUse ? "Ready for use" : "Draft");
    }
}
