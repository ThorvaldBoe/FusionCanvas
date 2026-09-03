using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.Domain.Mockups;

public sealed record MockupTemplateReadinessContext(
    MockupTemplate Template,
    MockupTemplateRevision Revision,
    IReadOnlyList<Guid> ActiveColorOptionValueIds,
    IReadOnlyList<OfferingOption> Options,
    IReadOnlyList<OfferingOptionValue> OptionValues,
    IReadOnlyList<OfferingVariant> Variants,
    IReadOnlyList<OfferingPlaceholder> DesignAreas,
    IReadOnlySet<Guid>? KnownSupportedColorOptionValueIds = null);
