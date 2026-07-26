namespace FusionCanvas.Application.Niches;

public sealed record NicheManagementState(
    Guid? ActiveStoreId,
    IReadOnlyList<NicheSummary> ActiveNiches,
    IReadOnlyList<NicheSummary> ArchivedNiches,
    Guid? ActiveNicheId,
    NicheSummary? ActiveNiche,
    bool NeedsFirstNiche);
