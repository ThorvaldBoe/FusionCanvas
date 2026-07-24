namespace FusionCanvas.Application.Groups;

public sealed record GroupDestination(GroupParentReference Parent, Guid StoreId, Guid NicheId, string DisplayPath);
