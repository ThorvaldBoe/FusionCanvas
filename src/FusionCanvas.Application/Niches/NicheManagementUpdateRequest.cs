namespace FusionCanvas.Application.Niches;

public sealed record NicheManagementUpdateRequest(Guid NicheId, string Name, NicheContext? Context = null);
