namespace FusionCanvas.Application.Niches;

public sealed record NicheManagementDeleteRequest(Guid NicheId, bool ConfirmPermanentDeletion);
