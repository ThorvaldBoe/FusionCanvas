using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Application.Catalog;

public sealed record OfferingManagementState(OfferingContext Context, bool IsReadOnly, Blueprint Blueprint, BlueprintOffering Offering, BlueprintOfferingSetupSummary Summary, IReadOnlyList<OfferingOption> Options, IReadOnlyList<OfferingOptionValue> OptionValues, IReadOnlyList<OfferingVariant> Variants, IReadOnlyList<OfferingPlaceholder> DesignAreas, IReadOnlyList<DesignAreaSetupSummary> DesignAreaSummaries, IReadOnlyList<MockupTemplate> MockupTemplates, IReadOnlyList<MockupTemplateSetupSummary> MockupTemplateSummaries);
