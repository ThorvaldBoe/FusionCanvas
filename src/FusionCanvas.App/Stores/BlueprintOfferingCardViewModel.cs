using FusionCanvas.Application.Catalog;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;

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
