namespace FusionCanvas.Application.Groups;

public sealed record GroupCreationDestinationResult(GroupParentReference? Parent, string? Error)
{
    public bool Succeeded => Parent is not null;
}
