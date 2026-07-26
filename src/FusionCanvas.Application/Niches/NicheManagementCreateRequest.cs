namespace FusionCanvas.Application.Niches;

public sealed record NicheManagementCreateRequest(Guid StoreId, string Name, NicheContext? Context = null);
