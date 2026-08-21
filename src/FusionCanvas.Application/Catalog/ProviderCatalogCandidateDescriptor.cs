namespace FusionCanvas.Application.Catalog;

public sealed record ProviderCatalogCandidateDescriptor(OfferingContext Context, bool IsAvailable, string? UnavailableReason, IReadOnlySet<ProviderCatalogCombination> ValidColorSizeCombinations, IReadOnlyList<ProviderMockupCandidateDescriptor>? MockupImages = null)
{
    public IReadOnlyList<ProviderMockupCandidateDescriptor> AvailableMockupImages => MockupImages ?? [];

    public static ProviderCatalogCandidateDescriptor Unavailable(OfferingContext context, string reason) => new(context, false, reason, new HashSet<ProviderCatalogCombination>(), []);
}
