using FusionCanvas.Application.Catalog;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.App.Stores;

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
